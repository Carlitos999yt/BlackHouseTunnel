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
        public static readonly string CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.3.7";
        public static string LatestVersion { get; private set; } = "1.3.7";
        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string LatestDownloadUrl { get; private set; } = "";
        public static string DownloadedUpdatePath { get; private set; } = "";
        public static event EventHandler? OnUpdateStatusChanged;

        private static readonly HttpClient Client;

        static UpdateService()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10
            };
            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // Clean up any old leftover temp update binaries on startup
            try
            {
                string tempExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");
                if (File.Exists(tempExe))
                {
                    File.Delete(tempExe);
                }
            }
            catch { }
        }

        /// <summary>
        /// Step 1: Query GitHub Releases REST API for latest version and direct EXE asset URL.
        /// </summary>
        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                string apiUrl = "https://api.github.com/repos/Carlitos999yt/BlackHouseTunnel/releases/latest";
                var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                var resp = await Client.SendAsync(req);

                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("tag_name", out var tagProp))
                    {
                        string tag = tagProp.GetString()?.TrimStart('v') ?? CurrentVersion;
                        LatestVersion = tag;

                        if (doc.RootElement.TryGetProperty("assets", out var assetsArr) && assetsArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var asset in assetsArr.EnumerateArray())
                            {
                                string name = asset.GetProperty("name").GetString() ?? "";
                                if (name.Equals("BlackHouseTunnel.exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    LatestDownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(LatestDownloadUrl))
                        {
                            LatestDownloadUrl = $"https://github.com/Carlitos999yt/BlackHouseTunnel/releases/download/v{LatestVersion}/BlackHouseTunnel.exe";
                        }

                        if (IsNewerVersion(LatestVersion, CurrentVersion))
                        {
                            IsUpdateAvailable = true;
                            OnUpdateStatusChanged?.Invoke(null, EventArgs.Empty);
                            return;
                        }
                    }
                }

                // Fallback: Check version.json with cache-busting timestamp
                string versionUrl = $"https://raw.githubusercontent.com/Carlitos999yt/BlackHouseTunnel/main/version.json?t={DateTime.UtcNow.Ticks}";
                string fallbackJson = await Client.GetStringAsync(versionUrl);

                using var fDoc = JsonDocument.Parse(fallbackJson);
                if (fDoc.RootElement.TryGetProperty("version", out var vProp))
                {
                    string onlineVersion = vProp.GetString() ?? CurrentVersion;
                    LatestVersion = onlineVersion;

                    if (fDoc.RootElement.TryGetProperty("download_url", out var dlProp) && dlProp.ValueKind != JsonValueKind.Null && !string.IsNullOrWhiteSpace(dlProp.GetString()))
                    {
                        LatestDownloadUrl = dlProp.GetString()!;
                    }
                    else
                    {
                        LatestDownloadUrl = $"https://raw.githubusercontent.com/Carlitos999yt/BlackHouseTunnel/main/Archivos_Compilados_Y_Zips/BlackHouseTunnel.exe";
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
        /// Step 2: Download the update binary with real-time progress callbacks and multi-fallback support.
        /// </summary>
        public static async Task<bool> DownloadUpdateWithProgressAsync(Action<long, long, double> onProgress)
        {
            if (string.IsNullOrWhiteSpace(LatestDownloadUrl))
            {
                LatestDownloadUrl = $"https://github.com/Carlitos999yt/BlackHouseTunnel/releases/download/v{LatestVersion}/BlackHouseTunnel.exe";
            }

            string tempExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");

            try
            {
                if (File.Exists(tempExe))
                {
                    try { File.Delete(tempExe); } catch { }
                }

                // Attempt 1: Streaming download with progress callbacks
                using var response = await Client.GetAsync(LatestDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
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
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UpdateService] Streaming download error: {ex.Message}");
            }

            // Attempt 2 Fallback: Direct ByteArray download if streaming failed
            if (!File.Exists(tempExe) || new FileInfo(tempExe).Length < 10000000) // Expecting ~70 MB
            {
                try
                {
                    byte[] data = await Client.GetByteArrayAsync(LatestDownloadUrl);
                    await File.WriteAllBytesAsync(tempExe, data);
                    onProgress?.Invoke(data.Length, data.Length, 100.0);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UpdateService] ByteArray download error: {ex.Message}");
                }
            }

            if (File.Exists(tempExe) && new FileInfo(tempExe).Length > 10000000)
            {
                DownloadedUpdatePath = tempExe;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Step 3: Replace the EXACT running executable file in its current location and launch it.
        /// </summary>
        public static void ApplyUpdateAndRestart()
        {
            try
            {
                string currentExe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                string updateExe = DownloadedUpdatePath;
                if (string.IsNullOrWhiteSpace(updateExe) || !File.Exists(updateExe) || new FileInfo(updateExe).Length < 10000000)
                {
                    updateExe = Path.Combine(Path.GetTempPath(), "BlackHouseTunnel_Update.exe");
                }

                if (!File.Exists(updateExe) || new FileInfo(updateExe).Length < 10000000)
                {
                    DarkMessageBox.Show("El archivo de actualización no se ha descargado completamente. Por favor presiona Reintentar.", "Error de Actualización", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                int currentPid = Process.GetCurrentProcess().Id;

                // PowerShell script:
                // 1. Force closes current PID
                // 2. Copies temp update EXE over the EXACT running EXE location (currentExe)
                // 3. Deletes temp update EXE from %TEMP%
                // 4. Starts the updated EXE
                string psCommand = $"Start-Sleep -Milliseconds 500; Get-Process -Id {currentPid} -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Seconds 1; Copy-Item -Path '{updateExe}' -Destination '{currentExe}' -Force; Remove-Item -Path '{updateExe}' -Force -ErrorAction SilentlyContinue; Start-Process -FilePath '{currentExe}'";

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

        public static bool IsNewerVersion(string online, string current)
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
