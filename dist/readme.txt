
--| BootBoost 1.0
--| By TKGP
--| https://www.nexusmods.com/darksouls3/mods/303
--| https://github.com/JKAnderson/BootBoost

Decreases initial boot time in DS3 by around 10-15 seconds. Every time the game starts it has to decrypt the .bhd files in the game directory, which takes a surprisingly long time. Fortunately if you decrypt them yourself and replace the files it will just load them directly, which is what this tool does.
I recommend using with the NoLogo Mod for maximum speed:
https://github.com/bladecoding/DarkSouls3RemoveIntroScreens/releases/tag/v1.15b
Requires .NET 4.7.2 - Windows 10 users should already have this:
https://www.microsoft.com/net/download/thank-you/net472


--| Installation

1. Drop BootBoost.exe into the game directory, usually "C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game"  
2. Run it.


--| Uninstallation

1. Restore the original files from the "BootBoost Backup" folder.


--| Credits

BouncyCastle
https://www.bouncycastle.org/csharp/

Costura.Fody by Simon Cropp, Cameron MacFarland
https://github.com/Fody/Costura
