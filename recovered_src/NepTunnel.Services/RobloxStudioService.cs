using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NepTunnel.Services;

public static class RobloxStudioService
{
	public record StudioInstallation(string Name, string Path, string Type, bool IsRecommended);

	public const string VINEGAR = "__VINEGAR__";

	private static readonly List<Process> _spawnedProcesses = new List<Process>();

	private static readonly object _procLock = new object();

	public static Action<string, string>? OnStudioError;

	private static IEnumerable<string> SafeGetFiles(string rootPath, string searchPattern)
	{
		Queue<string> pending = new Queue<string>();
		pending.Enqueue(rootPath);
		while (pending.Count > 0)
		{
			string currentDir = pending.Dequeue();
			string[] array = Array.Empty<string>();
			try
			{
				array = Directory.GetFiles(currentDir, searchPattern);
			}
			catch
			{
			}
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				yield return array2[i];
			}
			string[] array3 = Array.Empty<string>();
			try
			{
				array3 = Directory.GetDirectories(currentDir);
			}
			catch
			{
			}
			string[] array4 = array3;
			foreach (string item in array4)
			{
				pending.Enqueue(item);
			}
		}
	}

	public static List<StudioInstallation> GetDetectedStudioInstallations()
	{
		List<StudioInstallation> list = new List<StudioInstallation>();
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
				string text = Path.Combine(folderPath, "Roblox Studio");
				if (Directory.Exists(text))
				{
					foreach (string f in SafeGetFiles(text, "RobloxStudioBeta.exe").OrderByDescending(delegate(string path)
					{
						try
						{
							return File.GetLastWriteTime(path);
						}
						catch
						{
							return DateTime.MinValue;
						}
					}))
					{
						if (!list.Any((StudioInstallation i) => i.Path.Equals(f, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(new StudioInstallation("Roblox Studio RSM (Mod Manager)", f, "RSM", IsRecommended: true));
						}
					}
				}
				string text2 = Path.Combine(folderPath, "Roblox Studio Mod Manager");
				if (Directory.Exists(text2))
				{
					foreach (string f2 in SafeGetFiles(text2, "RobloxStudioBeta.exe").OrderByDescending(delegate(string path)
					{
						try
						{
							return File.GetLastWriteTime(path);
						}
						catch
						{
							return DateTime.MinValue;
						}
					}))
					{
						if (!list.Any((StudioInstallation i) => i.Path.Equals(f2, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(new StudioInstallation("Roblox Studio RSM (Mod Manager)", f2, "RSM", IsRecommended: true));
						}
					}
				}
				string text3 = Path.Combine(folderPath, "Bloxstrap", "Versions");
				if (Directory.Exists(text3))
				{
					foreach (string f3 in SafeGetFiles(text3, "RobloxStudioBeta.exe").OrderByDescending(delegate(string path)
					{
						try
						{
							return File.GetLastWriteTime(path);
						}
						catch
						{
							return DateTime.MinValue;
						}
					}))
					{
						if (!list.Any((StudioInstallation i) => i.Path.Equals(f3, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(new StudioInstallation("Bloxstrap Studio (" + Path.GetFileName(Path.GetDirectoryName(f3)) + ")", f3, "Bloxstrap", IsRecommended: false));
						}
					}
				}
				string text4 = Path.Combine(folderPath, "Roblox", "Versions");
				if (Directory.Exists(text4))
				{
					foreach (string f4 in SafeGetFiles(text4, "RobloxStudioBeta.exe").OrderByDescending(delegate(string path)
					{
						try
						{
							return File.GetLastWriteTime(path);
						}
						catch
						{
							return DateTime.MinValue;
						}
					}))
					{
						if (!list.Any((StudioInstallation i) => i.Path.Equals(f4, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(new StudioInstallation("Roblox Studio Oficial (" + Path.GetFileName(Path.GetDirectoryName(f4)) + ")", f4, "Oficial", IsRecommended: false));
						}
					}
				}
				string text5 = Path.Combine(folderPath2, "Roblox", "Versions");
				if (Directory.Exists(text5))
				{
					foreach (string f5 in SafeGetFiles(text5, "RobloxStudioBeta.exe").OrderByDescending(delegate(string path)
					{
						try
						{
							return File.GetLastWriteTime(path);
						}
						catch
						{
							return DateTime.MinValue;
						}
					}))
					{
						if (!list.Any((StudioInstallation i) => i.Path.Equals(f5, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(new StudioInstallation("Roblox Studio Oficial (Program Files)", f5, "Oficial", IsRecommended: false));
						}
					}
				}
			}
		}
		catch
		{
		}
		return list.OrderByDescending(delegate(StudioInstallation i)
		{
			try
			{
				return File.GetLastWriteTime(i.Path);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}).ToList();
	}

	public static string GetStudioPath()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			List<StudioInstallation> detectedStudioInstallations = GetDetectedStudioInstallations();
			if (detectedStudioInstallations.Count > 0)
			{
				StudioInstallation studioInstallation = detectedStudioInstallations.OrderByDescending(delegate(StudioInstallation i)
				{
					try
					{
						return File.GetLastWriteTime(i.Path);
					}
					catch
					{
						return DateTime.MinValue;
					}
				}).FirstOrDefault();
				if (studioInstallation != null && File.Exists(studioInstallation.Path))
				{
					return studioInstallation.Path;
				}
			}
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "RobloxStudioBeta.exe");
			if (File.Exists(text))
			{
				return text;
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			string text2 = "/Applications/RobloxStudio.app/Contents/MacOS/RobloxStudio";
			string text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications/RobloxStudio.app/Contents/MacOS/RobloxStudio");
			if (File.Exists(text2))
			{
				return text2;
			}
			if (File.Exists(text3))
			{
				return text3;
			}
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Roblox", "Versions");
			if (Directory.Exists(path))
			{
				List<string> list = (from f in Directory.GetFiles(path, "RobloxStudio", SearchOption.AllDirectories)
					orderby File.GetLastWriteTime(f)
					select f).ToList();
				if (list.Count > 0)
				{
					return list.Last();
				}
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			try
			{
				using Process process = Process.Start(new ProcessStartInfo("flatpak", "info org.vinegarhq.Vinegar")
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				});
				process?.WaitForExit(5000);
				if (process != null && process.ExitCode == 0)
				{
					return "__VINEGAR__";
				}
			}
			catch
			{
			}
		}
		return "";
	}

	private static ProcessStartInfo BuildCmd(string studio, List<string> args)
	{
		if (studio == "__VINEGAR__")
		{
			List<string> list = new List<string>();
			list.Add("run");
			list.Add("org.vinegarhq.Vinegar");
			list.Add("studio");
			list.Add("--");
			list.AddRange(args);
			ProcessStartInfo processStartInfo = new ProcessStartInfo("flatpak")
			{
				UseShellExecute = false
			};
			{
				foreach (string item in list)
				{
					processStartInfo.ArgumentList.Add(item);
				}
				return processStartInfo;
			}
		}
		ProcessStartInfo processStartInfo2 = new ProcessStartInfo(studio)
		{
			UseShellExecute = false
		};
		foreach (string arg in args)
		{
			processStartInfo2.ArgumentList.Add(arg);
		}
		return processStartInfo2;
	}

	public static void LaunchServer(string studio, string port, string uid, string pg, string tg)
	{
		List<string> args = new List<string>
		{
			"-task", "StartServer", "-placeId", "0", "-universeId", "0", "-placeVersion", "1", "-port", port,
			"-creatorId", uid, "-creatorType", "1", "-numTestServerPlayersUponStartup", "1", "-userid", uid, "-parentSessionGuid", pg,
			"-playTestSessionGuid", tg, "-instanceId", "StudioServer"
		};
		ProcessStartInfo startInfo = BuildCmd(studio, args);
		try
		{
			Process proc = Process.Start(startInfo);
			if (proc == null)
			{
				return;
			}
			proc.EnableRaisingEvents = true;
			proc.Exited += delegate
			{
				try
				{
					int exitCode = proc.ExitCode;
					if (exitCode != 0)
					{
						OnStudioError?.Invoke($"⚠ Roblox Studio Server exited unexpectedly (Exit Code: 0x{exitCode:X8})", "err");
						OnStudioError?.Invoke("  Possible cause: Roblox cloud API degradation or corrupt map file.", "warn");
					}
				}
				catch
				{
				}
			};
			lock (_procLock)
			{
				_spawnedProcesses.Add(proc);
			}
		}
		catch (Exception ex)
		{
			OnStudioError?.Invoke("✗ Failed to launch Studio Server: " + ex.Message, "err");
		}
	}

	public static void LaunchClient(string studio, string server, string port, string pg, string tg, string uid = "1000", string inst = "StudioPlayer_0")
	{
		List<string> args = new List<string>
		{
			"-task", "StartClient", "-placeId", "0", "-universeId", "0", "-placeVersion", "1", "-server", server,
			"-port", port, "-parentSessionGuid", pg, "-playTestSessionGuid", tg, "-instanceId", inst
		};
		ProcessStartInfo startInfo = BuildCmd(studio, args);
		try
		{
			Process proc = Process.Start(startInfo);
			if (proc == null)
			{
				return;
			}
			proc.EnableRaisingEvents = true;
			proc.Exited += delegate
			{
				try
				{
					int exitCode = proc.ExitCode;
					if (exitCode != 0)
					{
						OnStudioError?.Invoke($"⚠ Roblox Studio Client exited unexpectedly (Exit Code: 0x{exitCode:X8})", "err");
						OnStudioError?.Invoke("  Possible cause: Remote Host closed connection or invalid tunnel port.", "warn");
					}
				}
				catch
				{
				}
			};
			lock (_procLock)
			{
				_spawnedProcesses.Add(proc);
			}
		}
		catch (Exception ex)
		{
			OnStudioError?.Invoke("✗ Failed to launch Studio Client: " + ex.Message, "err");
		}
	}

	public static void StopAllStudioProcesses()
	{
		lock (_procLock)
		{
			foreach (Process spawnedProcess in _spawnedProcesses)
			{
				try
				{
					if (!spawnedProcess.HasExited)
					{
						spawnedProcess.CloseMainWindow();
						if (!spawnedProcess.WaitForExit(1500))
						{
							spawnedProcess.Kill(entireProcessTree: true);
						}
					}
				}
				catch
				{
				}
			}
			_spawnedProcesses.Clear();
		}
	}
}
