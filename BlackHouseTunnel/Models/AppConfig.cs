namespace BlackHouseTunnel.Models
{
    public class AppConfig
    {
        public string ClientId { get; set; } = "1534613209523294349";
        public string ClientSecret { get; set; } = "";
        public string RedirectUri { get; set; } = "http://localhost:5000/callback";
        public string GuildId { get; set; } = "";
        public string BotToken { get; set; } = "";
        public int LocalServerPort { get; set; } = 5000;

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
