using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NepTunnel.Services;

public static class ConfigManager
{
	public static string ScriptDir { get; }

	public static string AppDataDir { get; }

	public static string LogFile { get; }

	public static string AssetsDir { get; }

	public static List<string> BundledMaps { get; }

	private static List<string> GetConfigSearchPaths()
	{
		List<string> list = new List<string>();
		string item = Path.Combine(AppDataDir, "nep_config.json");
		list.Add(item);
		string item2 = Path.Combine(Directory.GetCurrentDirectory(), "nep_config.json");
		if (!list.Contains(item2))
		{
			list.Add(item2);
		}
		string item3 = Path.Combine(ScriptDir, "nep_config.json");
		if (!list.Contains(item3))
		{
			list.Add(item3);
		}
		try
		{
			string fullPath = Path.GetFullPath(Path.Combine(ScriptDir, "..", "..", "..", "nep_config.json"));
			if (!list.Contains(fullPath))
			{
				list.Add(fullPath);
			}
			string fullPath2 = Path.GetFullPath(Path.Combine(ScriptDir, "..", "..", "..", "..", "nep_config.json"));
			if (!list.Contains(fullPath2))
			{
				list.Add(fullPath2);
			}
		}
		catch
		{
		}
		return list;
	}

	static ConfigManager()
	{
		ScriptDir = AppDomain.CurrentDomain.BaseDirectory;
		AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NepTunnel");
		LogFile = Path.Combine(AppDataDir, "SESSION_INFO.txt");
		AssetsDir = Path.Combine(AppDataDir, "bundled_assets");
		BundledMaps = new List<string>();
		InitBundledAssets();
	}

	private static void InitBundledAssets()
	{
		string[] array = new string[2] { "MapsforNepfile.rbxm", "CleanedAnimsNepFile.rbxm" };
		try
		{
			Directory.CreateDirectory(AssetsDir);
			string[] array2 = array;
			foreach (string path in array2)
			{
				string text = Path.Combine(ScriptDir, path);
				string text2 = Path.Combine(AssetsDir, path);
				if (!File.Exists(text))
				{
					continue;
				}
				try
				{
					FileInfo fileInfo = new FileInfo(text);
					FileInfo fileInfo2 = new FileInfo(text2);
					if (!fileInfo2.Exists || fileInfo.Length != fileInfo2.Length)
					{
						File.Copy(text, text2, overwrite: true);
					}
					BundledMaps.Add(text2);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	public static NepConfig LoadConfig()
	{
		NepConfig nepConfig = new NepConfig();
		foreach (string configSearchPath in GetConfigSearchPaths())
		{
			try
			{
				if (File.Exists(configSearchPath))
				{
					NepConfig nepConfig2 = JsonSerializer.Deserialize<NepConfig>(File.ReadAllText(configSearchPath));
					if (nepConfig2 != null)
					{
						nepConfig = nepConfig2;
						break;
					}
				}
			}
			catch
			{
			}
		}
		foreach (string bundledMap in BundledMaps)
		{
			if (!nepConfig.SavedMaps.Contains(bundledMap))
			{
				nepConfig.SavedMaps.Insert(0, bundledMap);
			}
		}
		return nepConfig;
	}

	public static void SaveConfig(NepConfig config)
	{
		try
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true
			};
			string contents = JsonSerializer.Serialize(config, options);
			try
			{
				Directory.CreateDirectory(AppDataDir);
			}
			catch
			{
			}
			foreach (string configSearchPath in GetConfigSearchPaths())
			{
				try
				{
					string directoryName = Path.GetDirectoryName(configSearchPath);
					if (!string.IsNullOrEmpty(directoryName))
					{
						Directory.CreateDirectory(directoryName);
						File.WriteAllText(configSearchPath, contents);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	public static string WriteSessionLog(string pg, string tg, string tunnelAddr, string port, string uid)
	{
		string value;
		string value2;
		if (tunnelAddr.Contains(':'))
		{
			string[] array = tunnelAddr.Split(':', 2);
			value = array[0];
			value2 = array[1];
		}
		else
		{
			value = tunnelAddr;
			value2 = port;
		}
		string text = $"powershell -ExecutionPolicy Bypass -Command \"$p = Get-ChildItem -Path $env:LOCALAPPDATA\\Roblox\\Versions -Filter RobloxStudioBeta.exe -Recurse | Select-Object -First 1 -ExpandProperty FullName; Start-Process -FilePath $p -ArgumentList '-task StartClient -placeId 0 -universeId 0 -placeVersion 0 -server {value} -port {value2} -parentSessionGuid {pg} -playTestSessionGuid {tg} -instanceId StudioPlayer_0'\"";
		string item = $"\"/Applications/RobloxStudio.app/Contents/MacOS/RobloxStudio\" -task StartClient -placeId 0 -universeId 0 -placeVersion 0 -server {value} -port {value2} -parentSessionGuid {pg} -playTestSessionGuid {tg} -instanceId StudioPlayer_0";
		string item2 = $"flatpak run org.vinegarhq.Vinegar studio -- -task StartClient -placeId 0 -universeId 0 -placeVersion 0 -server {value} -port {value2} -parentSessionGuid {pg} -playTestSessionGuid {tg} -instanceId StudioPlayer_0";
		List<string> contents = new List<string>
		{
			"==========================================================",
			"  NEP TUNNEL - ROBLOX STUDIO SESSION LOG                  ",
			"==========================================================",
			$"Date       : {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
			"User ID    : " + uid,
			"Address    : " + tunnelAddr,
			"Server Local Port: " + port,
			"",
			"-- WINDOWS (Command Prompt) --",
			text,
			"",
			"-- MAC (Terminal) --",
			item,
			"",
			"-- LINUX / VINEGAR --",
			item2,
			"",
			"=========================================================="
		};
		try
		{
			File.WriteAllLines(LogFile, contents);
		}
		catch
		{
		}
		return text;
	}
}
