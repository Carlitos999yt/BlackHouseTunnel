using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BlackHouseTunnel.Models
{
    public class CustomAccessRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string RuleName { get; set; } = "Regla Personalizada";
        public List<string> AllowedRoleIds { get; set; } = new();
        public List<string> AllowedUserIds { get; set; } = new();
        public bool RequireAccessKey { get; set; } = false;
        public string CustomAccessKey { get; set; } = "";
        public string EmbedColorHex { get; set; } = "#A855F7";
        public string BadgeLabel { get; set; } = "⚡ Regla Personalizada";
    }

    public class AppConfig
    {
        public string ClientId { get; set; } = "1534613209523294349";

        [JsonIgnore]
        public string ClientSecret { get; set; } = "";

        public string RedirectUri { get; set; } = "http://localhost:5000/callback";
        public string GuildId { get; set; } = "1529015986135502951";

        public string ProtectedBotToken { get; set; } = "Dzg0GSUMNg0+H3BOdhY5GC4RAVo7NygJdmhsKw4nXCwXW14Mb0tsMj0YDSV5KDoELFxpUwQCCCcgKiVEFFxBD3UlPDc7OS0u";

        [JsonIgnore]
        public string BotToken
        {
            get => !string.IsNullOrEmpty(ProtectedBotToken) ? BlackHouseTunnel.Services.TokenProtector.Unprotect(ProtectedBotToken) : "";
            set => ProtectedBotToken = BlackHouseTunnel.Services.TokenProtector.Protect(value);
        }

        public string ChannelId { get; set; } = "1529169033482600659";
        public string PlayitChannelId { get; set; } = "1535670567040974898"; // Dedicated Discord channel for Playit address mapping
        public int LocalServerPort { get; set; } = 5000;

        // System Preferences
        public string Language { get; set; } = "es";
        public string ThemeMode { get; set; } = "Dark";
        public string SelectedStudioPath { get; set; } = "";
        public bool EnableDiscordRpc { get; set; } = true;

        // Saved Host Form Fields
        public string SavedUserId { get; set; } = "";
        public string SavedUsername { get; set; } = "";
        public string SavedServerName { get; set; } = "";
        public int SavedUdpPort { get; set; } = 55555;
        public string SavedRemoteHostAddress { get; set; } = "";
        public string SavedMapPath { get; set; } = "";
        public int SavedVisibilityOptionIndex { get; set; } = 0;
        public string SavedAccessKey { get; set; } = "";

        // Saved Custom Access Rules
        public List<CustomAccessRule> SavedCustomRules { get; set; } = new();

        // Auto-Login Session Token
        public string? SavedAccessToken { get; set; } = null;
    }
}
