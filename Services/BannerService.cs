using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NepTunnel.Services
{
    // Service to handle downloading and caching application banner and logo images asynchronously.
    public static class BannerService
    {
        public const string BG_IMG_URL = "https://gaming-cdn.com/img/products/1756/pcover/1756.jpg?v=1649173756";
        public const string LOGO_URL = "https://i.imgur.com/68Bdv5u_d.webp?maxwidth=760&fidelity=grand";

        public static string BannerCachePath => Path.Combine(ConfigManager.AssetsDir, "banner.jpg");
        public static string LogoCachePath => Path.Combine(ConfigManager.AssetsDir, "logo.png");

        // Loads the banner image from local disk cache or downloads it from remote URL.
        public static async Task<BitmapImage?> GetBannerImageAsync()
        {
            try
            {
                if (File.Exists(BannerCachePath))
                {
                    return LoadBitmapFromFile(BannerCachePath);
                }

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                byte[] data = await client.GetByteArrayAsync(BG_IMG_URL);

                Directory.CreateDirectory(ConfigManager.AssetsDir);
                await File.WriteAllBytesAsync(BannerCachePath, data);

                return LoadBitmapFromFile(BannerCachePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[banner] Download failed: {ex.Message}");
                return null;
            }
        }

        // Loads the application logo image from local disk cache or downloads it from remote URL.
        public static async Task<BitmapImage?> GetLogoImageAsync()
        {
            try
            {
                if (File.Exists(LogoCachePath))
                {
                    return LoadBitmapFromFile(LogoCachePath);
                }

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                byte[] data = await client.GetByteArrayAsync(LOGO_URL);

                Directory.CreateDirectory(ConfigManager.AssetsDir);
                await File.WriteAllBytesAsync(LogoCachePath, data);

                return LoadBitmapFromFile(LogoCachePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[logo] Download failed: {ex.Message}");
                return null;
            }
        }

        // Helper method to safely instantiate and freeze a BitmapImage from a file path.
        private static BitmapImage? LoadBitmapFromFile(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
