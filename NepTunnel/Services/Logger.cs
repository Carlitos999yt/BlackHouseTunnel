using System;
using System.IO;

namespace NepTunnel.Services;

public static class Logger
{
	public static string LogDir { get; }

	public static string LatestLogPath { get; }

	static Logger()
	{
		LogDir = Path.Combine(ConfigManager.AppDataDir, "logs");
		LatestLogPath = Path.Combine(LogDir, "latest.log");
		try
		{
			Directory.CreateDirectory(LogDir);
			if (File.Exists(LatestLogPath))
			{
				FileInfo fileInfo = new FileInfo(LatestLogPath);
				if (fileInfo.Length > 0)
				{
					string text = fileInfo.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
					string text2 = Path.Combine(LogDir, "log_" + text + ".log");
					if (!File.Exists(text2))
					{
						File.Move(LatestLogPath, text2);
					}
				}
			}
			File.WriteAllText(LatestLogPath, $"=== NepTunnel Session Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
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
			FileInfo fileInfo = null;
			DateTime dateTime = DateTime.MinValue;
			FileInfo[] array = files;
			foreach (FileInfo fileInfo2 in array)
			{
				if (fileInfo2.LastWriteTime > dateTime)
				{
					dateTime = fileInfo2.LastWriteTime;
					fileInfo = fileInfo2;
				}
			}
			if (fileInfo != null && File.Exists(fileInfo.FullName))
			{
				using (FileStream stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using StreamReader streamReader = new StreamReader(stream);
					string text = streamReader.ReadToEnd();
					Log("--- Captured Roblox Studio Log (" + fileInfo.Name + ") ---");
					Log(text);
					return text;
				}
			}
		}
		catch (Exception ex)
		{
			LogError("Failed to fetch Roblox Studio log", ex);
		}
		return null;
	}
}
