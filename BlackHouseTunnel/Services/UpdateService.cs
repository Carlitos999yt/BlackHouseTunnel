using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public static class UpdateService
    {
        public static readonly string CurrentVersion = "1.2.1";
        public static string LatestVersion { get; private set; } = "1.2.1";
        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string LatestDownloadUrl { get; private set; } = "";
        public static string DownloadedUpdatePath { get; private set; } = "";
        public static event EventHandler? OnUpdateStatusChanged;

        private static readonly HttpClient Client = new HttpClient();

        static UpdateService()
        {
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("BlackHouseTunnel-Updater/1.0");
        }

        /// <summary>
        /// Step 1: Lightly check version.json from GitHub (Tiny ~200 byte check, no binary download yet).
        /// </summary>
        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                string versionUrl = "https://raw.githubusercontent.com/Carlitos999yt/BlackHouseTunnel/main/version.json";
                string json = await Client.GetStringAsync(versionUrl);

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("version", out var vProp))
                {
                    string onlineVersion = vProp.GetString() ?? CurrentVersion;
                    LatestVersion = onlineVersion;

                    if (doc.RootElement.TryGetProperty("download_url", out var dlProp))
                    {
                        LatestDownloadUrl = dlProp.GetString() ?? "";
                    }

                    if (IsNewerVersion(onlineVersion, CurrentVersion))
                    {
                        IsUpdateAvailable = true;
                        OnUpdateStatusChanged?.Invoke(null, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UpdateService] Check update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Step 2: Download the update binary with real-time progress callbacks.
        /// </summary>
        public static async Task<bool> DownloadUpdateWithProgressAsync(Action<long, long, double> onProgress)
        {
            if (string.IsNullOrWhiteSpace(LatestDownloadUrl)) return false;

            try
            {
                string tempExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");
                if (File.Exists(tempExe))
                {
                    try { File.Delete(tempExe); } catch { }
                }

                using var response = await Client.GetAsync(LatestDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0L;
                int read;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    double percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0.0;
                    onProgress?.Invoke(totalRead, totalBytes, percent);
                }

                await fileStream.FlushAsync();
                DownloadedUpdatePath = tempExe;
                return File.Exists(tempExe) && new FileInfo(tempExe).Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UpdateService] Download progress error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Step 3: Replace current executable & restart cleanly using a hidden PowerShell runner.
        /// </summary>
        public static void ApplyUpdateAndRestart()
        {
            try
            {
                string currentExe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                string updateExe = DownloadedUpdatePath;
                if (string.IsNullOrWhiteSpace(updateExe) || !File.Exists(updateExe) || new FileInfo(updateExe).Length == 0)
                {
                    updateExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");
                }

                if (!File.Exists(updateExe) || new FileInfo(updateExe).Length == 0)
                {
                    DarkMessageBox.Show("El archivo de actualización no ha sido descargado correctamente. Por favor intenta descargar de nuevo.", "Error de Actualización", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                int currentPid = Process.GetCurrentProcess().Id;

                // Hidden PowerShell replacement script - No black CMD window, 100% reliable process exit & replacement!
                string psCommand = $"Start-Sleep -Milliseconds 500; Get-Process -Id {currentPid} -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Seconds 1; Copy-Item -Path '{updateExe}' -Destination '{currentExe}' -Force; Start-Process -FilePath '{currentExe}'";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
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
