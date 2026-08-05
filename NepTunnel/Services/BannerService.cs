using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NepTunnel.Services;

public static class BannerService
{
	public static string BannerCachePath => Path.Combine(ConfigManager.AssetsDir, "banner.jpg");

	public static string LogoCachePath => Path.Combine(ConfigManager.AssetsDir, "logo.png");

	public static Task<BitmapImage?> GetBannerImageAsync()
	{
		return Task.Run(delegate
		{
			try
			{
				Uri uriSource = new Uri("pack://application:,,,/bundled_assets/banner.jpg", UriKind.Absolute);
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.UriSource = uriSource;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
			catch
			{
				if (File.Exists(BannerCachePath))
				{
					return LoadBitmapFromFile(BannerCachePath);
				}
				return (BitmapImage?)null;
			}
		});
	}

	public static Task<BitmapImage?> GetLogoImageAsync()
	{
		return Task.Run(delegate
		{
			try
			{
				Uri uriSource = new Uri("pack://application:,,,/bundled_assets/logo.png", UriKind.Absolute);
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.UriSource = uriSource;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
			catch
			{
				if (File.Exists(LogoCachePath))
				{
					return LoadBitmapFromFile(LogoCachePath);
				}
				return (BitmapImage?)null;
			}
		});
	}

	private static BitmapImage? LoadBitmapFromFile(string path)
	{
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return bitmapImage;
		}
		catch
		{
			return null;
		}
	}
}
