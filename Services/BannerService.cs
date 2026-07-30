using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NepTunnel.Services
{
    // Service to handle loading application banner and logo images asynchronously.
    public static class BannerService
    {
        public static string BannerCachePath => Path.Combine(ConfigManager.AssetsDir, "banner.jpg");
        public static string LogoCachePath => Path.Combine(ConfigManager.AssetsDir, "logo.png");

        // Loads the banner image from WPF embedded assembly resources or local disk.
        public static Task<BitmapImage?> GetBannerImageAsync()
        {
            return Task.Run<BitmapImage?>(() =>
            {
                try
                {
                    // 1. Try embedded WPF Assembly Pack URI
                    var packUri = new Uri("pack://application:,,,/bundled_assets/banner.jpg", UriKind.Absolute);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = packUri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    // 2. Fallback to local file path
                    if (File.Exists(BannerCachePath))
                    {
                        return LoadBitmapFromFile(BannerCachePath);
                    }
                    return null;
                }
            });
        }

        // Loads the application logo image from WPF embedded assembly resources or local disk.
        public static Task<BitmapImage?> GetLogoImageAsync()
        {
            return Task.Run<BitmapImage?>(() =>
            {
                try
                {
                    // 1. Try embedded WPF Assembly Pack URI
                    var packUri = new Uri("pack://application:,,,/bundled_assets/logo.png", UriKind.Absolute);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = packUri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    // 2. Fallback to local file path
                    if (File.Exists(LogoCachePath))
                    {
                        return LoadBitmapFromFile(LogoCachePath);
                    }
                    return null;
                }
            });
        }

        // Helper method to safely load and freeze a BitmapImage from a file path.
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
