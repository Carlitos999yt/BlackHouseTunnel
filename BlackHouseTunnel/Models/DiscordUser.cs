using System.Collections.Generic;
using System.Linq;

namespace BlackHouseTunnel.Models
{
    public class DiscordUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ServerNick { get; set; } = string.Empty; // Guild Nickname
        public string GlobalName { get; set; } = string.Empty;
        public string Discriminator { get; set; } = "0";
        public string? AvatarHash { get; set; } = null;
        public string? DirectAvatarUrl { get; set; } = null;
        public bool IsMemberOfGuild { get; set; } = false;
        public bool IsBot { get; set; } = false;
        public List<string> RoleIds { get; set; } = new();
        public List<string> RoleNames { get; set; } = new();
        public string PrimaryRole { get; set; } = "Usuario";
        public string PrimaryRoleColor { get; set; } = "#5865F2";

        // Special Role Badges
        public bool IsPrivadito { get; set; } = false;
        public bool IsHoster { get; set; } = false;
        public bool IsStaffOrAdmin { get; set; } = false;
        public bool IsOwner { get; set; } = false;
        public bool IsMod { get; set; } = false;
        public bool IsSuperior => IsOwner || PrimaryRole.Equals("Owner", System.StringComparison.OrdinalIgnoreCase) || RoleNames.Any(r => r.ToLowerInvariant().Contains("owner") || r.ToLowerInvariant().Contains("superior"));
        public bool IsCanHostOrManage => IsStaffOrAdmin || IsOwner || IsMod || IsSuperior || IsHoster;

        public string AvatarUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(DirectAvatarUrl))
                {
                    return DirectAvatarUrl;
                }
                if (!string.IsNullOrEmpty(AvatarHash) && !string.IsNullOrEmpty(Id))
                {
                    string ext = AvatarHash.StartsWith("a_") ? "gif" : "png";
                    return $"https://cdn.discordapp.com/avatars/{Id}/{AvatarHash}.{ext}?size=128";
                }
                return "https://cdn.discordapp.com/embed/avatars/0.png";
            }
        }

        public string CustomNickname { get; set; } = string.Empty;

        // Top Line 1: CustomNickname if set, otherwise ServerNick, GlobalName or Username
        public string DisplayNick => !string.IsNullOrWhiteSpace(CustomNickname) ? CustomNickname : (!string.IsNullOrWhiteSpace(ServerNick) ? ServerNick : (!string.IsNullOrWhiteSpace(GlobalName) ? GlobalName : Username));
        public string DisplayName => DisplayNick;

        // Line 2: Original Handle @username
        public string Handle => $"@{Username}";
    }

    public class DiscordRole
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
        public uint Color { get; set; }
    }
}
