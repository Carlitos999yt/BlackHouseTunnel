namespace BlackHouseTunnel.Models
{
    public class AppConfig
    {
        public string ClientId { get; set; } = "1534613209523294349";
        public string ClientSecret { get; set; } = "";
        public string RedirectUri { get; set; } = "http://localhost:5000/callback";
        public string GuildId { get; set; } = "";
        public string BotToken { get; set; } = "";
        public string ChannelId { get; set; } = "1531027757365203015";
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
