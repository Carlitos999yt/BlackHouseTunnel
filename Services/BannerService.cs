using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NepTunnel.Services
{
    // Service to handle loading application banner and logo vector assets.
    public static class BannerService
    {
        public static string BannerCachePath => Path.Combine(ConfigManager.AssetsDir, "banner.jpg");
        public static string LogoCachePath => Path.Combine(ConfigManager.AssetsDir, "logo.png");

        // Returns resolution-independent vector banner drawing source.
        public static Task<ImageSource?> GetBannerImageAsync()
        {
            return Task.Run<ImageSource?>(() =>
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
                    // 2. Return native vector banner image
                    return VectorAssetService.CreateVectorBanner();
                }
            });
        }

        // Returns resolution-independent vector logo drawing source.
        public static Task<ImageSource?> GetLogoImageAsync()
        {
            return Task.Run<ImageSource?>(() =>
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
                    // 2. Return native vector logo image
                    return VectorAssetService.CreateVectorLogo();
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
