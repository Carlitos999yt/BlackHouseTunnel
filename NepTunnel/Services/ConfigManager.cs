using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NepTunnel.Services
{
    public static class ConfigManager
    {
        public static string ScriptDir { get; }
        public static string AppDataDir { get; }
        public static string SystemConfigPath { get; }
        public static string LogFile { get; }
        public static string AssetsDir { get; }
        public static List<string> BundledMaps { get; }

        static ConfigManager()
        {
            ScriptDir = AppDomain.CurrentDomain.BaseDirectory;
            AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NepTunnel");
            SystemConfigPath = Path.Combine(AppDataDir, "nep_config.json");
            LogFile = Path.Combine(AppDataDir, "SESSION_INFO.txt");
            AssetsDir = Path.Combine(AppDataDir, "bundled_assets");
            BundledMaps = new List<string>();

            try
            {
                Directory.CreateDirectory(AppDataDir);
            }
            catch { }

            InitBundledAssets();
            MigrateAndCleanupLocalConfig();
        }

        private static void InitBundledAssets()
        {
            string[] files = new[] { "MapsforNepfile.rbxm", "CleanedAnimsNepFile.rbxm" };
            try
            {
                Directory.CreateDirectory(AssetsDir);
                foreach (string name in files)
                {
                    string src = Path.Combine(ScriptDir, name);
                    string dst = Path.Combine(AssetsDir, name);
                    if (File.Exists(src))
                    {
                        try
                        {
                            var sFi = new FileInfo(src);
                            var dFi = new FileInfo(dst);
                            if (!dFi.Exists || sFi.Length != dFi.Length)
                            {
                                File.Copy(src, dst, overwrite: true);
                            }
                            BundledMaps.Add(dst);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Security & Migration System: Checks if a local nep_config.json exists in the current directory or script directory.
        /// If found, migrates its configurations to the System AppData folder and removes the local file to keep workspace clean.
        /// </summary>
        private static void MigrateAndCleanupLocalConfig()
        {
            string[] legacySearchPaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "nep_config.json"),
                Path.Combine(ScriptDir, "nep_config.json")
            };

            foreach (string legacyPath in legacySearchPaths)
            {
                try
                {
                    if (File.Exists(legacyPath) && !legacyPath.Equals(SystemConfigPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // If system config does not exist yet, migrate legacy config content
                        if (!File.Exists(SystemConfigPath))
                        {
                            string legacyJson = File.ReadAllText(legacyPath);
                            var loaded = JsonSerializer.Deserialize<NepConfig>(legacyJson);
                            if (loaded != null)
                            {
                                SaveConfig(loaded);
                            }
                        }

                        // Remove legacy config file after migration to keep local directory 100% clean
                        File.Delete(legacyPath);
                    }
                }
                catch { }
            }
        }

        public static NepConfig LoadConfig()
        {
            NepConfig config = new NepConfig();

            // Primary: System AppData configuration file
            if (File.Exists(SystemConfigPath))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize<NepConfig>(File.ReadAllText(SystemConfigPath));
                    if (loaded != null)
                    {
                        config = loaded;
                    }
                }
                catch { }
            }

            foreach (string bundledMap in BundledMaps)
            {
                if (!config.SavedMaps.Contains(bundledMap))
                {
                    config.SavedMaps.Insert(0, bundledMap);
                }
            }

            return config;
        }

        public static void SaveConfig(NepConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(config, options);

                Directory.CreateDirectory(AppDataDir);
                File.WriteAllText(SystemConfigPath, json);
            }
            catch { }
        }

        public static void WriteSessionLog(string pg, string tg, string addr, string port, string uid)
        {
            try
            {
                Directory.CreateDirectory(AppDataDir);
                string text = $"TIMESTAMP: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nPARENT_GUID: {pg}\nPLAYTEST_GUID: {tg}\nTUNNEL_ADDRESS: {addr}\nSERVER_PORT: {port}\nCREATOR_UID: {uid}\n----------------------------------------\n";
                File.AppendAllText(LogFile, text);
            }
            catch { }
        }
    }
}
