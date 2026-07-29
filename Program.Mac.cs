using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NepTunnel.Services;

namespace NepTunnel
{
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

            // Detect Language or System Culture
            var cfg = ConfigManager.LoadConfig();
            if (string.IsNullOrEmpty(cfg.Language))
            {
                cfg.Language = LocalizationService.DetectDefaultSystemLanguage();
                ConfigManager.SaveConfig(cfg);
            }
            LocalizationService.CurrentLanguage = cfg.Language;

            // Detect Studio Path on Mac
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
                Console.WriteLine($"\n[✓] Roblox Studio detected: {studioPath}");
                Console.ResetColor();
            }

            // Start RBXM Bridge Server
            RbxmBridgeServer.Start();
            Console.WriteLine($"[✓] Studio Bridge Server running on http://127.0.0.1:{RbxmBridgeServer.BRIDGE_PORT}");

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

                string? choice = Console.ReadLine()?.Trim();
                if (choice == "5") break;

                switch (choice)
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
                }
            }

            // Cleanup on Exit
            RbxmBridgeServer.Stop();
            UdpProxy.StopProxy(wait: false);
            RobloxStudioService.StopAllStudioProcesses();
            Console.WriteLine("Goodbye!");
        }

        private static async Task RunHostMode(string studioPath)
        {
            var cfg = ConfigManager.LoadConfig();
            Console.WriteLine("\n--- HOST SESSION CONFIG ---");
            Console.WriteLine($"User ID     : {cfg.Uid}");
            Console.WriteLine($"Local Port  : {cfg.Port}");
            Console.WriteLine($"Tunnel Addr : {cfg.Addr}");
            if (!string.IsNullOrEmpty(cfg.Map))
                Console.WriteLine($"Map File    : {cfg.Map}");

            Console.Write("\nPress Enter to Launch Server (or 'c' to cancel): ");
            if (Console.ReadLine()?.Trim().ToLower() == "c") return;

            string pg = Guid.NewGuid().ToString().ToUpper();
            string tg = Guid.NewGuid().ToString().ToUpper();

            if (!string.IsNullOrEmpty(cfg.Map) && File.Exists(cfg.Map))
            {
                Console.WriteLine($"Injecting map: {Path.GetFileName(cfg.Map)}...");
                MapInjector.InjectMap(cfg.Map);
            }

            Console.WriteLine("Launching Roblox Studio Server on macOS...");
            RobloxStudioService.LaunchServer(studioPath, cfg.Port, cfg.Uid, pg, tg);

            ConfigManager.WriteSessionLog(pg, tg, cfg.Addr, cfg.Port, cfg.Uid);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[✓] SERVER IS LIVE!");
            Console.WriteLine($"    Share this address with friends: {cfg.Addr}");
            Console.ResetColor();

            Console.WriteLine("\nPress Enter to Stop Server & Return to Menu...");
            Console.ReadLine();

            RobloxStudioService.StopAllStudioProcesses();
            Console.WriteLine("Server stopped.");
        }

        private static async Task RunJoinMode(string studioPath)
        {
            var cfg = ConfigManager.LoadConfig();
            Console.Write($"\nEnter Tunnel Address [{cfg.Addr}]: ");
            string inputAddr = Console.ReadLine()?.Trim() ?? "";
            string addr = string.IsNullOrEmpty(inputAddr) ? cfg.Addr : inputAddr;

            if (!addr.Contains(':'))
            {
                Console.WriteLine("[!] Invalid address format. Expected host:port");
                return;
            }

            var parts = addr.Split(':', 2);
            string host = parts[0];
            int port = int.Parse(parts[1]);

            Console.WriteLine($"Starting UDP Proxy to {host}:{port}...");
            bool ok = UdpProxy.StartProxy(host, port);
            if (!ok)
            {
                Console.WriteLine($"[!] Failed to bind local proxy port {UdpProxy.PROXY_PORT}");
                return;
            }

            Console.WriteLine("Warming tunnel...");
            UdpProxy.WarmTunnel();

            string pg = Guid.NewGuid().ToString().ToUpper();
            string tg = Guid.NewGuid().ToString().ToUpper();

            Console.WriteLine("Launching Roblox Studio Client on macOS...");
            RobloxStudioService.LaunchClient(studioPath, "127.0.0.1", UdpProxy.PROXY_PORT.ToString(), pg, tg, "StudioPlayer_MacProxy");

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
            var cfg = ConfigManager.LoadConfig();
            Console.Write($"Enter Tunnel Address to test [{cfg.Addr}]: ");
            string inputAddr = Console.ReadLine()?.Trim() ?? "";
            string addr = string.IsNullOrEmpty(inputAddr) ? cfg.Addr : inputAddr;

            if (!addr.Contains(':'))
            {
                Console.WriteLine("[!] Invalid address.");
                return;
            }

            var parts = addr.Split(':', 2);
            await EchoClient.RunEchoTestAsync((msg, tag) =>
            {
                if (tag == "ok") Console.ForegroundColor = ConsoleColor.Green;
                else if (tag == "err") Console.ForegroundColor = ConsoleColor.Red;
                else if (tag == "warn") Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(msg);
                Console.ResetColor();
            }, parts[0], int.Parse(parts[1]));
        }

        private static void ShowSettings()
        {
            var cfg = ConfigManager.LoadConfig();
            Console.WriteLine("\n--- CURRENT CONFIGURATION ---");
            Console.WriteLine($"Config File Path: {ConfigManager.LogFile}");
            Console.WriteLine($"User ID         : {cfg.Uid}");
            Console.WriteLine($"Server Port     : {cfg.Port}");
            Console.WriteLine($"Tunnel Address  : {cfg.Addr}");
            Console.WriteLine($"Language        : {cfg.Language}");
        }
    }
}
