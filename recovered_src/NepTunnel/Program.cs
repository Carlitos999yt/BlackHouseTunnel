using System;
using System.IO;
using System.Threading.Tasks;
using NepTunnel.Services;

namespace NepTunnel;

internal class Program
{
	private static async Task Main(string[] args)
	{
		Console.Clear();
		Console.ForegroundColor = ConsoleColor.Magenta;
		Console.WriteLine("==================================================");
		Console.WriteLine("           NEP TUNNEL  ·  macOS Version           ");
		Console.WriteLine("         Roblox Studio Multi-Player Proxy        ");
		Console.WriteLine("==================================================");
		Console.ResetColor();
		NepConfig nepConfig = ConfigManager.LoadConfig();
		if (string.IsNullOrEmpty(nepConfig.Language))
		{
			nepConfig.Language = LocalizationService.DetectDefaultSystemLanguage();
			ConfigManager.SaveConfig(nepConfig);
		}
		LocalizationService.CurrentLanguage = nepConfig.Language;
		string studioPath = RobloxStudioService.GetStudioPath();
		if (string.IsNullOrEmpty(studioPath))
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("\n[!] Roblox Studio was not found on this Mac.");
			Console.WriteLine("    Please ensure RobloxStudio.app is installed in /Applications.");
			Console.ResetColor();
		}
		else
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("\n[✓] Roblox Studio detected: " + studioPath);
			Console.ResetColor();
		}
		RbxmBridgeServer.Start();
		Console.WriteLine($"[✓] Studio Bridge Server running on http://127.0.0.1:{7878}");
		while (true)
		{
			Console.WriteLine("\n--------------------------------------------------");
			Console.WriteLine(" SELECT AN OPTION:");
			Console.WriteLine("  1. HOST SESSION  (Launch Roblox Server)");
			Console.WriteLine("  2. JOIN SESSION  (Connect to Host Tunnel)");
			Console.WriteLine("  3. ECHO TEST     (Test UDP Latency)");
			Console.WriteLine("  4. SETTINGS      (View/Edit Config & Language)");
			Console.WriteLine("  5. EXIT");
			Console.WriteLine("--------------------------------------------------");
			Console.Write("Choice [1-5]: ");
			switch (Console.ReadLine()?.Trim())
			{
			case "1":
				await RunHostMode(studioPath);
				break;
			case "2":
				await RunJoinMode(studioPath);
				break;
			case "3":
				await RunEchoTest();
				break;
			case "4":
				ShowSettings();
				break;
			default:
				Console.WriteLine("Invalid option.");
				break;
			case "5":
				RbxmBridgeServer.Stop();
				UdpProxy.StopProxy(wait: false);
				RobloxStudioService.StopAllStudioProcesses();
				Console.WriteLine("Goodbye!");
				return;
			}
		}
	}

	private static async Task RunHostMode(string studioPath)
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		Console.WriteLine("\n--- HOST SESSION CONFIG ---");
		Console.WriteLine("User ID     : " + nepConfig.Uid);
		Console.WriteLine("Local Port  : " + nepConfig.Port);
		Console.WriteLine("Tunnel Addr : " + nepConfig.Addr);
		if (!string.IsNullOrEmpty(nepConfig.Map))
		{
			Console.WriteLine("Map File    : " + nepConfig.Map);
		}
		Console.Write("\nPress Enter to Launch Server (or 'c' to cancel): ");
		if (!(Console.ReadLine()?.Trim().ToLower() == "c"))
		{
			string pg = Guid.NewGuid().ToString().ToUpper();
			string tg = Guid.NewGuid().ToString().ToUpper();
			if (!string.IsNullOrEmpty(nepConfig.Map) && File.Exists(nepConfig.Map))
			{
				Console.WriteLine("Injecting map: " + Path.GetFileName(nepConfig.Map) + "...");
				MapInjector.InjectMap(nepConfig.Map);
			}
			Console.WriteLine("Launching Roblox Studio Server on macOS...");
			RobloxStudioService.LaunchServer(studioPath, nepConfig.Port, nepConfig.Uid, pg, tg);
			ConfigManager.WriteSessionLog(pg, tg, nepConfig.Addr, nepConfig.Port, nepConfig.Uid);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("\n[✓] SERVER IS LIVE!");
			Console.WriteLine("    Share this address with friends: " + nepConfig.Addr);
			Console.ResetColor();
			Console.WriteLine("\nPress Enter to Stop Server & Return to Menu...");
			Console.ReadLine();
			RobloxStudioService.StopAllStudioProcesses();
			Console.WriteLine("Server stopped.");
		}
	}

	private static async Task RunJoinMode(string studioPath)
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		Console.Write("\nEnter Tunnel Address [" + nepConfig.Addr + "]: ");
		string text = Console.ReadLine()?.Trim() ?? "";
		string text2 = (string.IsNullOrEmpty(text) ? nepConfig.Addr : text);
		if (!text2.Contains(':'))
		{
			Console.WriteLine("[!] Invalid address format. Expected host:port");
			return;
		}
		string[] array = text2.Split(':', 2);
		string text3 = array[0];
		int num = int.Parse(array[1]);
		Console.WriteLine($"Starting UDP Proxy to {text3}:{num}...");
		if (!UdpProxy.StartProxy(text3, num))
		{
			Console.WriteLine($"[!] Failed to bind local proxy port {55555}");
			return;
		}
		Console.WriteLine("Warming tunnel...");
		UdpProxy.WarmTunnel();
		string pg = Guid.NewGuid().ToString().ToUpper();
		string tg = Guid.NewGuid().ToString().ToUpper();
		Console.WriteLine("Launching Roblox Studio Client on macOS...");
		RobloxStudioService.LaunchClient(studioPath, "127.0.0.1", 55555.ToString(), pg, tg, "StudioPlayer_MacProxy");
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("\n[✓] CONNECTED TO SESSION!");
		Console.ResetColor();
		Console.WriteLine("\nPress Enter to Disconnect & Return to Menu...");
		Console.ReadLine();
		UdpProxy.StopProxy();
		RobloxStudioService.StopAllStudioProcesses();
		Console.WriteLine("Disconnected.");
	}

	private static async Task RunEchoTest()
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		Console.Write("Enter Tunnel Address to test [" + nepConfig.Addr + "]: ");
		string text = Console.ReadLine()?.Trim() ?? "";
		string text2 = (string.IsNullOrEmpty(text) ? nepConfig.Addr : text);
		if (!text2.Contains(':'))
		{
			Console.WriteLine("[!] Invalid address.");
			return;
		}
		string[] array = text2.Split(':', 2);
		await EchoClient.RunEchoTestAsync(delegate(string msg, string tag)
		{
			switch (tag)
			{
			case "ok":
				Console.ForegroundColor = ConsoleColor.Green;
				break;
			case "err":
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case "warn":
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			default:
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
			}
			Console.WriteLine(msg);
			Console.ResetColor();
		}, array[0], int.Parse(array[1]));
	}

	private static void ShowSettings()
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		Console.WriteLine("\n--- CURRENT CONFIGURATION ---");
		Console.WriteLine("Config File Path: " + ConfigManager.LogFile);
		Console.WriteLine("User ID         : " + nepConfig.Uid);
		Console.WriteLine("Server Port     : " + nepConfig.Port);
		Console.WriteLine("Tunnel Address  : " + nepConfig.Addr);
		Console.WriteLine("Language        : " + nepConfig.Language);
	}
}
