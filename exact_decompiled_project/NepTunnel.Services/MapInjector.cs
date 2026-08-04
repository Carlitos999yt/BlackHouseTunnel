using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NepTunnel.Services;

public static class MapInjector
{
	public static string GetRuntimeServerPlace()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "server.rbxl");
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine(folderPath, "Library", "Application Support", "Roblox", "server.rbxl");
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine(folderPath2, ".var", "app", "org.vinegarhq.Vinegar", "data", "Roblox", "server.rbxl");
		}
		return "";
	}

	public static bool InjectMap(string mapPath)
	{
		if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
		{
			string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled_assets", "default_baseplate.rbxlx");
			if (!File.Exists(text))
			{
				return false;
			}
			mapPath = text;
		}
		string runtimeServerPlace = GetRuntimeServerPlace();
		if (string.IsNullOrEmpty(runtimeServerPlace))
		{
			return false;
		}
		try
		{
			string directoryName = Path.GetDirectoryName(runtimeServerPlace);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			if (File.Exists(runtimeServerPlace))
			{
				try
				{
					File.Delete(runtimeServerPlace);
				}
				catch
				{
				}
			}
			File.Copy(mapPath, runtimeServerPlace, overwrite: true);
			Console.WriteLine("[MapInjector] Successfully injected map: " + mapPath + " -> " + runtimeServerPlace);
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[MapInjector] Failed to inject: " + ex.Message);
			return false;
		}
	}
}
