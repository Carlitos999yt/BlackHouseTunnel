using System;
using System.IO;
using System.Text.Json;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public static class ConfigManager
    {
        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackHouseTunnel"
        );

        public static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "config.json");

        public static AppConfig CurrentConfig { get; private set; } = new AppConfig();

        static ConfigManager()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        // Auto-migrate old channel ID and empty Guild ID
                        bool modified = false;
                        if (config.ChannelId == "1531027757365203015" || string.IsNullOrWhiteSpace(config.ChannelId))
                        {
                            config.ChannelId = "1529169033482600659";
                            modified = true;
                        }
                        if (string.IsNullOrWhiteSpace(config.GuildId))
                        {
                            config.GuildId = "1529015986135502951";
                            modified = true;
                        }
                        if (modified)
                        {
                            SaveConfig(config);
                        }
                        CurrentConfig = config;
                        return;
                    }
                }

                SaveConfig(CurrentConfig);
            }
            catch
            {
                CurrentConfig = new AppConfig();
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                CurrentConfig = config;
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
            }
        }
    }
}
