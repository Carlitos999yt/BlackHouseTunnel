using System;
using System.IO;

namespace BlackHouseTunnel.Services
{
    public static class Logger
    {
        public static string AppDataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackHouseTunnel"
        );

        public static string LogDir => Path.Combine(AppDataDir, "logs");
        public static string LatestLogPath => Path.Combine(LogDir, "latest.log");

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                if (File.Exists(LatestLogPath))
                {
                    FileInfo fileInfo = new FileInfo(LatestLogPath);
                    if (fileInfo.Length > 0)
                    {
                        string timeStamp = fileInfo.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
                        string archivePath = Path.Combine(LogDir, $"log_{timeStamp}.log");
                        if (!File.Exists(archivePath))
                        {
                            File.Move(LatestLogPath, archivePath);
                        }
                    }
                }
                File.WriteAllText(LatestLogPath, $"=== BlackHouseTunnel Session Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
            }
            catch
            {
            }
        }

        public static void Log(string message)
        {
            try
            {
                string text = $"[{DateTime.Now:HH:mm:ss}] {message}";
                File.AppendAllText(LatestLogPath, text + "\n");
                Console.WriteLine(text);
            }
            catch
            {
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            string text = ((ex != null) ? $"{message} | Exception: {ex.Message}\n{ex.StackTrace}" : message);
            Log("[ERROR] " + text);
        }

        public static string? FetchLatestRobloxStudioLog()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "logs");
                if (!Directory.Exists(path))
                {
                    return null;
                }
                FileInfo[] files = new DirectoryInfo(path).GetFiles("*Studio*.log");
                if (files.Length == 0)
                {
                    files = new DirectoryInfo(path).GetFiles("*.log");
                }
                FileInfo? fileInfo = null;
                DateTime dateTime = DateTime.MinValue;
                foreach (FileInfo f in files)
                {
                    if (f.LastWriteTime > dateTime)
                    {
                        dateTime = f.LastWriteTime;
                        fileInfo = f;
                    }
                }
                if (fileInfo != null && File.Exists(fileInfo.FullName))
                {
                    using FileStream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using StreamReader streamReader = new StreamReader(stream);
                    string text = streamReader.ReadToEnd();
                    Log($"--- Captured Roblox Studio Log ({fileInfo.Name}) ---");
                    Log(text);
                    return text;
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to fetch Roblox Studio log", ex);
            }
            return null;
        }
    }
}
