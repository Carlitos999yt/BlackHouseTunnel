using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class PublishedTunnel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string HostId { get; set; } = "HOST-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        public string ServerName { get; set; } = "";
        public string HostUsername { get; set; } = "";
        public string RemoteAddress { get; set; } = "";
        public int VisibilityMode { get; set; } = 0; // 0: Global, 1: Servidor, 2: Privadito, >= 3: Custom Rule
        public string AccessKey { get; set; } = ""; // Key/Password for private hosts
        public bool RequiresAccessKey => !string.IsNullOrWhiteSpace(AccessKey);
        public string MinAppVersion { get; set; } = "1.3.1"; // Required minimum app version to join
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? DiscordMessageId { get; set; } = null;
        public string? PlayitMessageId { get; set; } = null;

        // Custom Rule Payload properties
        public string? CustomRuleName { get; set; } = null;
        public string? CustomEmbedColorHex { get; set; } = null;
        public string? CustomBadgeLabel { get; set; } = null;
        public List<string> AllowedRoleIds { get; set; } = new();
        public List<string> AllowedUserIds { get; set; } = new();
    }

    public static class ActiveTunnelRegistry
    {
        private static readonly object SyncLock = new object();
        private static readonly DiscordApiService ApiService = new DiscordApiService();
        private static readonly string RegistryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackHouseTunnel",
            "active_tunnels.json");

        public static async Task<string?> PublishTunnelAsync(PublishedTunnel tunnel)
        {
            var config = ConfigManager.CurrentConfig;
            string token = !string.IsNullOrWhiteSpace(config.BotToken) ? config.BotToken : TokenProtector.GetDefaultBotToken();
            string channel = !string.IsNullOrWhiteSpace(config.ChannelId) ? config.ChannelId : "1529169033482600659";
            string playitChannel = !string.IsNullOrWhiteSpace(config.PlayitChannelId) ? config.PlayitChannelId : "1535670567040974898";
            string? msgId = null;

            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(channel))
            {
                msgId = await ApiService.PostTunnelEmbedToChannelAsync(channel, token, tunnel);
                tunnel.DiscordMessageId = msgId;

                if (!string.IsNullOrWhiteSpace(playitChannel))
                {
                    string? playitMsgId = await ApiService.PostPlayitMappingEmbedAsync(playitChannel, token, tunnel);
                    tunnel.PlayitMessageId = playitMsgId;
                }
            }

            lock (SyncLock)
            {
                var list = LoadTunnels();
                list.RemoveAll(t => t.HostUsername.Equals(tunnel.HostUsername, StringComparison.OrdinalIgnoreCase));
                list.Add(tunnel);
                SaveTunnels(list);
            }

            return msgId;
        }

        public static async Task UnpublishTunnelAsync(string hostUsername, string? messageId = null)
        {
            var config = ConfigManager.CurrentConfig;
            string token = !string.IsNullOrWhiteSpace(config.BotToken) ? config.BotToken : TokenProtector.GetDefaultBotToken();
            string channel = !string.IsNullOrWhiteSpace(config.ChannelId) ? config.ChannelId : "1529169033482600659";
            string playitChannel = !string.IsNullOrWhiteSpace(config.PlayitChannelId) ? config.PlayitChannelId : "1535670567040974898";

            string? playitMessageId = null;
            lock (SyncLock)
            {
                var list = LoadTunnels();
                var matching = list.FirstOrDefault(t => t.HostUsername.Equals(hostUsername, StringComparison.OrdinalIgnoreCase));
                if (matching != null)
                {
                    if (string.IsNullOrWhiteSpace(messageId)) messageId = matching.DiscordMessageId;
                    playitMessageId = matching.PlayitMessageId;
                }
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (!string.IsNullOrWhiteSpace(messageId) && !string.IsNullOrWhiteSpace(channel))
                {
                    await ApiService.DeleteTunnelEmbedAsync(channel, token, messageId);
                }
                // NOTE: We intentionally DO NOT delete playitChannel messages!
                // The Playit channel acts as a permanent historical audit log of every host created.
            }

            lock (SyncLock)
            {
                var list = LoadTunnels();
                list.RemoveAll(t => t.HostUsername.Equals(hostUsername, StringComparison.OrdinalIgnoreCase));
                SaveTunnels(list);
            }
        }

        public static async Task<List<PublishedTunnel>> GetVisibleTunnelsForUserAsync(DiscordUser user)
        {
            var config = ConfigManager.CurrentConfig;
            string token = !string.IsNullOrWhiteSpace(config.BotToken) ? config.BotToken : TokenProtector.GetDefaultBotToken();
            string channel = !string.IsNullOrWhiteSpace(config.ChannelId) ? config.ChannelId : "1529169033482600659";
            string playitChannel = !string.IsNullOrWhiteSpace(config.PlayitChannelId) ? config.PlayitChannelId : "1535670567040974898";
            List<PublishedTunnel> channelTunnels = new List<PublishedTunnel>();

            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(channel))
            {
                channelTunnels = await ApiService.FetchChannelTunnelEmbedsAsync(channel, token, playitChannel);
            }

            lock (SyncLock)
            {
                // Only keep tunnels that are actively present in live Discord channel embeds
                SaveTunnels(channelTunnels);

                return FilterTunnelsForUser(channelTunnels, user);
            }
        }

        public static List<PublishedTunnel> GetVisibleTunnelsForUser(DiscordUser user)
        {
            lock (SyncLock)
            {
                var localList = LoadTunnels();
                return FilterTunnelsForUser(localList, user);
            }
        }

        private static List<PublishedTunnel> FilterTunnelsForUser(List<PublishedTunnel> tunnels, DiscordUser user)
        {
            return tunnels.Where(t =>
            {
                if (t.VisibilityMode == 0) return true;
                if (t.VisibilityMode == 1) return user.IsMemberOfGuild || user.IsStaffOrAdmin;
                if (t.VisibilityMode == 2) return user.IsPrivadito || user.IsStaffOrAdmin;

                // Custom Rule evaluation (VisibilityMode >= 3 or CustomRuleName set)
                if (t.VisibilityMode >= 3 || !string.IsNullOrWhiteSpace(t.CustomRuleName))
                {
                    if (user.IsStaffOrAdmin || user.IsOwner) return true;
                    if (user.Username.Equals(t.HostUsername, StringComparison.OrdinalIgnoreCase)) return true;

                    // Check Role restrictions if defined
                    if (t.AllowedRoleIds != null && t.AllowedRoleIds.Count > 0)
                    {
                        if (t.AllowedRoleIds.Any(rId => user.RoleIds.Contains(rId))) return true;
                    }

                    // Check User whitelist if defined
                    if (t.AllowedUserIds != null && t.AllowedUserIds.Count > 0)
                    {
                        if (t.AllowedUserIds.Any(uId => uId.Equals(user.Id, StringComparison.OrdinalIgnoreCase) || uId.Equals(user.Username, StringComparison.OrdinalIgnoreCase))) return true;
                    }

                    // If rule specified no role/user whitelist, fallback to guild members
                    if ((t.AllowedRoleIds == null || t.AllowedRoleIds.Count == 0) && (t.AllowedUserIds == null || t.AllowedUserIds.Count == 0))
                    {
                        return user.IsMemberOfGuild;
                    }

                    return false;
                }

                return true;
            }).ToList();
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
                File.WriteAllText(RegistryPath, json);
            }
            catch
            {
            }
        }
    }
}
