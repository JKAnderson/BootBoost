using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Text;

namespace BootBoost
{
    class Program
    {
        private const string bakDir = "BootBoost Backup";

        static void Main(string[] args)
        {
            int success = 0;

            foreach (string header in Headers.Keys.Keys)
            {
                if (!File.Exists(header))
                {
                    Console.WriteLine($"Header not found, skipping: {header}");
                }
                else
                {
                    byte[] bytes = File.ReadAllBytes(header);
                    if (Encoding.ASCII.GetString(bytes, 0, 4) == "BHD5")
                    {
                        Console.WriteLine($"Header already decrypted, skipping: {header}");
                    }
                    else
                    {
                        Console.WriteLine($"Decrypting header: {header}");
                        byte[] decrypted = null;
                        try
                        {
                            decrypted = Decrypt(bytes, Headers.Keys[header]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to decrypt header. Reason:\n{ex}");
                        }

                        if (decrypted != null)
                        {
                            try
                            {
                                Directory.CreateDirectory(bakDir);
                                if (!File.Exists($"{bakDir}\\{header}"))
                                    File.Copy(header, $"{bakDir}\\{header}");

                                try
                                {
                                    File.WriteAllBytes(header, decrypted);
                                    Console.WriteLine($"Header decryption succeeded.");
                                    success++;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to write header. Reason:\n{ex}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to backup header. Reason:\n{ex}");
                            }
                        }
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine($"Decrypted {success} headers.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        // This is practically copy-paste from BinderTool. Thank you Atvaark!
        private static byte[] Decrypt(byte[] bytes, string key)
        {
            AsymmetricKeyParameter keyParam;
            using (var sr = new StringReader(key))
                keyParam = (AsymmetricKeyParameter)new PemReader(sr).ReadObject();

            var engine = new RsaEngine();
            engine.Init(false, keyParam);

            using (var outStream = new MemoryStream())
            using (var inStream = new MemoryStream(bytes))
            {
                int inBlockSize = engine.GetInputBlockSize();
                int outBlockSize = engine.GetOutputBlockSize();
                byte[] inBlock = new byte[inBlockSize];

                while (inStream.Read(inBlock, 0, inBlock.Length) > 0)
                {
                    byte[] outBlock = engine.ProcessBlock(inBlock, 0, inBlockSize);

                    int padding = outBlockSize - outBlock.Length;
                    if (padding > 0)
                    {
                        byte[] padBlock = new byte[outBlockSize];
                        outBlock.CopyTo(padBlock, padding);
                        outBlock = padBlock;
                    }

                    outStream.Write(outBlock, 0, outBlock.Length);
                }

                return outStream.ToArray();
            }
        }
    }
}
