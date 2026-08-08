using System.Text.Json.Serialization;

namespace BlackHouseTunnel.Models
{
    public class AppConfig
    {
        public string ClientId { get; set; } = "1534613209523294349";

        [JsonIgnore]
        public string ClientSecret { get; set; } = "";

        public string RedirectUri { get; set; } = "http://localhost:5000/callback";
        public string GuildId { get; set; } = "1529015986135502951";

        public string ProtectedBotToken { get; set; } = "";

        [JsonIgnore]
        public string BotToken
        {
            get => !string.IsNullOrEmpty(ProtectedBotToken) ? BlackHouseTunnel.Services.TokenProtector.Unprotect(ProtectedBotToken) : "";
            set => ProtectedBotToken = BlackHouseTunnel.Services.TokenProtector.Protect(value);
        }

        public string ChannelId { get; set; } = "1529169033482600659";
        public string PlayitChannelId { get; set; } = ""; // Mappings channel for playit addresses
        public int LocalServerPort { get; set; } = 5000;

        // System Preferences
        public string Language { get; set; } = "es";
        public string ThemeMode { get; set; } = "Dark";
        public string SelectedStudioPath { get; set; } = "";

        // Saved Host Form Fields
        public string SavedUserId { get; set; } = "";
        public string SavedUsername { get; set; } = "";
        public string SavedServerName { get; set; } = "";
        public int SavedUdpPort { get; set; } = 55555;
        public string SavedRemoteHostAddress { get; set; } = "";
        public string SavedMapPath { get; set; } = "";
        public int SavedVisibilityOptionIndex { get; set; } = 0;

        // Auto-Login Session Token
        public string? SavedAccessToken { get; set; } = null;
    }
}
