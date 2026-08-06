using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class OnlinePresenceItem
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string ServerNick { get; set; } = "";
        public string GlobalName { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }

    public static class OnlinePresenceRegistry
    {
        private static readonly object SyncLock = new object();
        private static readonly string PresencePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackHouseTunnel",
            "online_presences.json");

        public static void RegisterHeartbeat(DiscordUser user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username)) return;

            lock (SyncLock)
            {
                var list = LoadPresences();
                list.RemoveAll(p => p.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
                list.Add(new OnlinePresenceItem
                {
                    Id = user.Id,
                    Username = user.Username,
                    ServerNick = user.DisplayNick,
                    GlobalName = user.GlobalName,
                    AvatarUrl = user.AvatarUrl,
                    LastSeen = DateTime.UtcNow
                });
                SavePresences(list);
            }
        }

        public static List<DiscordUser> GetActiveAppUsers()
        {
            lock (SyncLock)
            {
                var list = LoadPresences();
                // Active within last 2 minutes
                list.RemoveAll(p => (DateTime.UtcNow - p.LastSeen).TotalMinutes > 2);
                SavePresences(list);

                return list.Select(p =>
                {
                    var u = new DiscordUser
                    {
                        Id = p.Id,
                        Username = p.Username,
                        ServerNick = p.ServerNick,
                        GlobalName = p.GlobalName,
                        DirectAvatarUrl = p.AvatarUrl
                    };
                    return u;
                }).ToList();
            }
        }

        private static List<OnlinePresenceItem> LoadPresences()
        {
            try
            {
                if (File.Exists(PresencePath))
                {
                    string json = File.ReadAllText(PresencePath);
                    return JsonSerializer.Deserialize<List<OnlinePresenceItem>>(json) ?? new List<OnlinePresenceItem>();
                }
            }
            catch
            {
            }
            return new List<OnlinePresenceItem>();
        }

        private static void SavePresences(List<OnlinePresenceItem> list)
        {
            try
            {
                string dir = Path.GetDirectoryName(PresencePath)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PresencePath, json);
            }
            catch
            {
            }
        }
    }
}
