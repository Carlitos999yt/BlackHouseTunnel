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
        public string ServerName { get; set; } = "";
        public string HostUsername { get; set; } = "";
        public string RemoteAddress { get; set; } = "";
        public int VisibilityMode { get; set; } = 0; // 0: Global, 1: Servidor, 2: Privadito
        public string AccessKey { get; set; } = ""; // Key/Password for private hosts
        public bool RequiresAccessKey => !string.IsNullOrWhiteSpace(AccessKey);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? DiscordMessageId { get; set; } = null;
        public string? PlayitMessageId { get; set; } = null;
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
            string? msgId = null;

            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(channel))
            {
                msgId = await ApiService.PostTunnelEmbedToChannelAsync(channel, token, tunnel);
                tunnel.DiscordMessageId = msgId;

                if (!string.IsNullOrWhiteSpace(config.PlayitChannelId))
                {
                    string? playitMsgId = await ApiService.PostPlayitMappingEmbedAsync(config.PlayitChannelId, token, tunnel);
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

        public static async Task UnpublishTunnelAsync(string hostUsername, string? messageId = null, string? playitMessageId = null)
        {
            var config = ConfigManager.CurrentConfig;
            string token = !string.IsNullOrWhiteSpace(config.BotToken) ? config.BotToken : TokenProtector.GetDefaultBotToken();
            string channel = !string.IsNullOrWhiteSpace(config.ChannelId) ? config.ChannelId : "1529169033482600659";

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (!string.IsNullOrWhiteSpace(messageId) && !string.IsNullOrWhiteSpace(channel))
                {
                    await ApiService.DeleteTunnelEmbedAsync(channel, token, messageId);
                }
                if (!string.IsNullOrWhiteSpace(playitMessageId) && !string.IsNullOrWhiteSpace(config.PlayitChannelId))
                {
                    await ApiService.DeleteTunnelEmbedAsync(config.PlayitChannelId, token, playitMessageId);
                }
            }

            lock (SyncLock)
            {
                var list = LoadTunnels();
                var matching = list.FirstOrDefault(t => t.HostUsername.Equals(hostUsername, StringComparison.OrdinalIgnoreCase));
                if (matching != null && !string.IsNullOrWhiteSpace(matching.DiscordMessageId) && string.IsNullOrWhiteSpace(messageId))
                {
                    Task.Run(() => ApiService.DeleteTunnelEmbedAsync(channel, token, matching.DiscordMessageId));
                }
                list.RemoveAll(t => t.HostUsername.Equals(hostUsername, StringComparison.OrdinalIgnoreCase));
                SaveTunnels(list);
            }
        }

        public static async Task<List<PublishedTunnel>> GetVisibleTunnelsForUserAsync(DiscordUser user)
        {
            var config = ConfigManager.CurrentConfig;
            string token = !string.IsNullOrWhiteSpace(config.BotToken) ? config.BotToken : TokenProtector.GetDefaultBotToken();
            string channel = !string.IsNullOrWhiteSpace(config.ChannelId) ? config.ChannelId : "1529169033482600659";
            List<PublishedTunnel> channelTunnels = new List<PublishedTunnel>();

            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(channel))
            {
                channelTunnels = await ApiService.FetchChannelTunnelEmbedsAsync(channel, token, config.PlayitChannelId);
            }

            lock (SyncLock)
            {
                // Only keep tunnels that are actively present in live Discord channel embeds
                SaveTunnels(channelTunnels);

                return channelTunnels.Where(t =>
                {
                    if (t.VisibilityMode == 0) return true;
                    if (t.VisibilityMode == 1) return user.IsMemberOfGuild || user.IsStaffOrAdmin;
                    if (t.VisibilityMode == 2) return user.IsPrivadito || user.IsStaffOrAdmin;
                    return false;
                }).ToList();
            }
        }

        public static List<PublishedTunnel> GetVisibleTunnelsForUser(DiscordUser user)
        {
            lock (SyncLock)
            {
                var localList = LoadTunnels();
                return localList.Where(t =>
                {
                    if (t.VisibilityMode == 0) return true;
                    if (t.VisibilityMode == 1) return user.IsMemberOfGuild || user.IsStaffOrAdmin;
                    if (t.VisibilityMode == 2) return user.IsPrivadito || user.IsStaffOrAdmin;
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
                File.WriteAllText(RegistryPath, json);
            }
            catch
            {
            }
        }
    }
}
