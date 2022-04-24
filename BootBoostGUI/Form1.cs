using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Text;

namespace BootBoostGUI
{
    public partial class Form1 : Form
    {
        string installDir = "";
        const string bakDir = "BootBoost Backup";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            errorLabel.Text = "";
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.UseDescriptionForTitle = true;
            fbd.Description = "Select the Folder Containing DarkSoulsIII.exe";
            fbd.InitialDirectory = "C:\\";
            
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                // Stores the path to the folder containing
                // DarkSoulsIII.exe in the installDir field
                this.installDir = fbd.SelectedPath;
                urlTextBox.Text = this.installDir;

                string[] files = Directory.GetFiles(this.installDir);
                bool hasDS3exe = false;
                
                foreach (string file in files)
                {
                    // 1 is added to the length to account for installDir not containing a \ at the end of it
                    if (file.Substring(this.installDir.Length + 1) == "DarkSoulsIII.exe")
                        hasDS3exe = true;
                }
                if (!hasDS3exe)
                {
                    errorLabel.Text = "DarkSoulsIII.exe was not found in the selected folder.";
                    startButton.Enabled = false;
                }
                else
                {
                    errorLabel.Text = "";
                    startButton.Enabled = true;
                }

                if (Directory.Exists($"{this.installDir}\\{bakDir}"))
                    restoreButton.Enabled = true;
                else
                    restoreButton.Enabled = false;
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            browseButton.Enabled = false;

            int success = 0;

            foreach (string header in Headers.Keys.Keys)
            {
                if (!File.Exists($"{this.installDir}\\{header}"))
                {
                    outputTextBox.AppendText($"Header not found, skipping: {header}" + "\r\n");
                }
                else
                {
                    byte[] bytes = File.ReadAllBytes($"{this.installDir}\\{header}");
                    if (Encoding.ASCII.GetString(bytes, 0, 4) == "BHD5")
                    {
                        outputTextBox.AppendText($"Header already decrypted, skipping: {header}" + "\r\n");
                    }
                    else
                    {
                        outputTextBox.AppendText($"Decrypting header: {header}" + "\r\n");
                        byte[] decrypted = Array.Empty<byte>();
                        try
                        {
                            decrypted = Decrypt(bytes, Headers.Keys[header]);
                        }
                        catch (Exception ex)
                        {
                            outputTextBox.AppendText($"Failed to decrypt header. Reason:\r\n{ex}" + "\r\n");
                        }

                        if (decrypted != null)
                        {
                            try
                            {
                                Directory.CreateDirectory($"{this.installDir}\\{bakDir}");
                                if (!File.Exists($"{this.installDir}\\{bakDir}\\{header}"))
                                    File.Copy($"{this.installDir}\\{header}", $"{this.installDir}\\{bakDir}\\{header}");

                                try
                                {
                                    File.WriteAllBytes($"{this.installDir}\\{header}", decrypted);
                                    outputTextBox.AppendText($"Header decryption succeeded." + "\r\n");
                                    success++;
                                }
                                catch (Exception ex)
                                {
                                    outputTextBox.AppendText($"Failed to write header. Reason:\r\n{ex}" + "\r\n");
                                }
                            }
                            catch (Exception ex)
                            {
                                outputTextBox.AppendText($"Failed to backup header. Reason:\r\n{ex}" + "\r\n");
                            }
                        }
                    }
                }
            }

            startButton.Enabled = false;
            restoreButton.Enabled = false;
            outputTextBox.AppendText($"Decrypted {success} headers." + "\r\n");
            outputTextBox.AppendText("\r\n");
            outputTextBox.AppendText("Click the Exit button to exit");
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

        private void restoreButton_Click(object sender, EventArgs e)
        {
            browseButton.Enabled = false;

            int success = 0;

            // If the backup directory does not exist, break from
            // the method.
            if (!Directory.Exists($"{this.installDir}\\{bakDir}"))
            {
                outputTextBox.AppendText($"Directory 'BootBoost Backup' was not found in '{this.installDir}'");
                browseButton.Enabled = true;
                return;
            }

            // Iterate through each header file to see if it
            // exists within the backup directory; if not,
            // set breakFlag to true and break from the method.
            string[] headers = Headers.Keys.Keys.ToArray();
            bool breakFlag = false;
            foreach (string header in headers)
            {
                if (!File.Exists($"{this.installDir}\\{bakDir}\\{header}"))
                {
                    outputTextBox.AppendText($"{(breakFlag ? "" : "Backup incomplete or not present.\r\n")}File '{header}' was not found in '{this.installDir}\\{bakDir}'" + "\r\n");
                    breakFlag = true;
                }
            }
            if (breakFlag)
            {
                browseButton.Enabled = true;
                return;
            }

            // Move each header from the backup directory to
            // the install directory then delete the backup
            // directory.
            breakFlag = false;
            Directory.CreateDirectory($"{this.installDir}\\tmp");
            foreach (string header in headers)
            {
                File.Copy($"{this.installDir}\\{header}", $"{this.installDir}\\tmp\\{header}", true);
                try
                {
                    outputTextBox.AppendText($"Restoring header: {header}" + "\r\n");
                    File.Copy($"{this.installDir}\\{bakDir}\\{header}", $"{this.installDir}\\{header}", true);
                    outputTextBox.AppendText($"Succussfully restored {header}" + "\r\n");
                    success++;
                }
                catch (Exception ex)
                {
                    outputTextBox.AppendText($"Failed to restore {header}. Reason:\r\n{ex}" + "\r\n");
                    breakFlag = true;
                }
            }
            if (breakFlag)
            {
                foreach (string header in headers)
                {
                    File.Copy($"{this.installDir}\\tmp\\{header}", $"{this.installDir}\\{header}", true);
                }
                Directory.Delete($"{this.installDir}\\tmp");
                browseButton.Enabled = true;
                outputTextBox.AppendText("\r\nSome headers failed to restore. Operation has been aborted and all files have been returned to their initial states." + "\r\n");
                return;
            }
            Directory.Delete($"{this.installDir}\\tmp", true);
            Directory.Delete($"{this.installDir}\\{bakDir}", true);

            startButton.Enabled = false;
            restoreButton.Enabled = false;
            outputTextBox.AppendText($"Restored {success} headers." + "\r\n");
            outputTextBox.AppendText("\r\n");
            outputTextBox.AppendText("Click the Exit button to exit");
        }
    }
}