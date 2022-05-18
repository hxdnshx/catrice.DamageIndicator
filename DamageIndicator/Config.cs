using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Dissonance;

namespace catrice.DamageIndicator
{


    public static class ConfigManager
    {
        // Token: 0x06000035 RID: 53 RVA: 0x00002B60 File Offset: 0x00000D60
        static ConfigManager()
        {
            string text = Path.Combine(Paths.ConfigPath, "DamageIndicator.cfg");
            ConfigFile configFile = new ConfigFile(text, true);
            ConfigManager._isHideDamage = configFile.Bind<bool>("Nyan", "IsHideDamage",
                true, "Indicates whether DamageIndicator will send damage information to all players through the chat panel. (False means it will be sent) ");
        }

        // Token: 0x17000010 RID: 16
        // (get) Token: 0x06000036 RID: 54 RVA: 0x00002EA4 File Offset: 0x000010A4
        public static bool IsHideDamage
        {
            get { return ConfigManager._isHideDamage.Value; }
        }

        private static ConfigEntry<bool> _isHideDamage;
    }
}