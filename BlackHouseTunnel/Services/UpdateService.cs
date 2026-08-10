using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public static class UpdateService
    {
        public static readonly string CurrentVersion = "1.1.0";
        public static string LatestVersion { get; private set; } = "1.1.0";
        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string DownloadedUpdatePath { get; private set; } = "";
        public static event EventHandler? OnUpdateStatusChanged;

        private static readonly HttpClient Client = new HttpClient();
        private static string _latestDownloadUrl = "";

        static UpdateService()
        {
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("BlackHouseTunnel-Updater/1.0");
        }

        public static async Task CheckAndDownloadUpdateAsync()
        {
            try
            {
                // Check version.json from BlackHouseTunnel GitHub repo
                string versionUrl = "https://raw.githubusercontent.com/Carlitos999yt/BlackHouseTunnel/main/version.json";
                string json = await Client.GetStringAsync(versionUrl);

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("version", out var vProp))
                {
                    string onlineVersion = vProp.GetString() ?? "1.1.0";
                    LatestVersion = onlineVersion;

                    if (IsNewerVersion(onlineVersion, CurrentVersion))
                    {
                        IsUpdateAvailable = true;

                        if (doc.RootElement.TryGetProperty("download_url", out var dlProp))
                        {
                            _latestDownloadUrl = dlProp.GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(_latestDownloadUrl))
                            {
                                string tempExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");
                                byte[] data = await Client.GetByteArrayAsync(_latestDownloadUrl);
                                await File.WriteAllBytesAsync(tempExe, data);
                                DownloadedUpdatePath = tempExe;
                            }
                        }

                        OnUpdateStatusChanged?.Invoke(null, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UpdateService] Check update error: {ex.Message}");
            }
        }

        public static void ApplyUpdateAndRestart()
        {
            try
            {
                string currentExe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                string updateExe = DownloadedUpdatePath;
                if (string.IsNullOrWhiteSpace(updateExe) || !File.Exists(updateExe) || new FileInfo(updateExe).Length == 0)
                {
                    updateExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");
                    if (!string.IsNullOrWhiteSpace(_latestDownloadUrl))
                    {
                        try
                        {
                            byte[] data = Client.GetByteArrayAsync(_latestDownloadUrl).GetAwaiter().GetResult();
                            File.WriteAllBytes(updateExe, data);
                            DownloadedUpdatePath = updateExe;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[UpdateService] Synchronous download failed: {ex.Message}");
                        }
                    }
                }

                if (!File.Exists(updateExe) || new FileInfo(updateExe).Length == 0)
                {
                    DarkMessageBox.Show("El archivo de actualización no está listo aún. Por favor intenta de nuevo en unos segundos.", "Actualización en Progreso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                string batchScript = Path.Combine(Path.GetTempPath(), "update_blackhouse.bat");
                string scriptContent = $@"@echo off
if not exist ""{updateExe}"" goto end
:retry
timeout /t 1 /nobreak > nul
copy /y ""{updateExe}"" ""{currentExe}""
if errorlevel 1 goto retry
start """" ""{currentExe}""
:end
del ""%~f0""
";
                File.WriteAllText(batchScript, scriptContent);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batchScript,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UpdateService] Apply update error: {ex.Message}");
            }
        }

        private static bool IsNewerVersion(string online, string current)
        {
            try
            {
                Version vOnline = new Version(online.TrimStart('v'));
                Version vCurrent = new Version(current.TrimStart('v'));
                return vOnline > vCurrent;
            }
            catch
            {
                return online != current;
            }
        }
    }
}
