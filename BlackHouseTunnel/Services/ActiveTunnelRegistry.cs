using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class PublishedTunnel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ServerName { get; set; } = "";
        public string HostUsername { get; set; } = "";
        public string RemoteAddress { get; set; } = "";
        public int VisibilityMode { get; set; } = 0; // 0: Global, 1: Servidor, 2: Privadito
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class ActiveTunnelRegistry
    {
        private static readonly object SyncLock = new object();
        private static readonly string RegistryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackHouseTunnel",
            "active_tunnels.json");

        public static void PublishTunnel(PublishedTunnel tunnel)
        {
            lock (SyncLock)
            {
                var list = LoadTunnels();
                list.RemoveAll(t => t.HostUsername.Equals(tunnel.HostUsername, StringComparison.OrdinalIgnoreCase));
                list.Add(tunnel);
                SaveTunnels(list);
            }
        }

        public static void UnpublishTunnel(string hostUsername)
        {
            lock (SyncLock)
            {
                var list = LoadTunnels();
                list.RemoveAll(t => t.HostUsername.Equals(hostUsername, StringComparison.OrdinalIgnoreCase));
                SaveTunnels(list);
            }
        }

        public static List<PublishedTunnel> GetVisibleTunnelsForUser(DiscordUser user)
        {
            lock (SyncLock)
            {
                var list = LoadTunnels();
                // Filter out stale tunnels older than 12 hours
                list.RemoveAll(t => (DateTime.UtcNow - t.CreatedAt).TotalHours > 12);
                SaveTunnels(list);

                return list.Where(t =>
                {
                    if (t.VisibilityMode == 0) return true; // Global
                    if (t.VisibilityMode == 1) return user.IsMemberOfGuild || user.IsStaffOrAdmin; // Servidor
                    if (t.VisibilityMode == 2) return user.IsPrivadito || user.IsStaffOrAdmin; // Privadito
                    return false;
                }).ToList();
            }
        }

        private static List<PublishedTunnel> LoadTunnels()
        {
            try
            {
                if (File.Exists(RegistryPath))
                {
                    string json = File.ReadAllText(RegistryPath);
                    return JsonSerializer.Deserialize<List<PublishedTunnel>>(json) ?? new List<PublishedTunnel>();
                }
            }
            catch
            {
            }
            return new List<PublishedTunnel>();
        }

        private static void SaveTunnels(List<PublishedTunnel> list)
        {
            try
            {
                string dir = Path.GetDirectoryName(RegistryPath)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.ReadAllText(RegistryPath); // check access
                File.WriteAllText(RegistryPath, json);
            }
            catch
            {
            }
        }
    }
}
