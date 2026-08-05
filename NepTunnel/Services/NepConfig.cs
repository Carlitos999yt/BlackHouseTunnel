using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NepTunnel.Services
{
    public class NepConfig
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = "";

        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("port")]
        public string Port { get; set; } = "55555";

        [JsonPropertyName("host_addr")]
        public string HostAddr { get; set; } = "";

        [JsonPropertyName("join_addr")]
        public string JoinAddr { get; set; } = "";

        [JsonPropertyName("addr")]
        public string Addr
        {
            get => HostAddr ?? "";
            set => HostAddr = value;
        }

        [JsonPropertyName("studio")]
        public string Studio { get; set; } = "";

        [JsonPropertyName("map")]
        public string Map { get; set; } = "";

        [JsonPropertyName("saved_maps")]
        public List<string> SavedMaps { get; set; } = new List<string>();

        [JsonPropertyName("lang")]
        public string Lang { get; set; } = "es";

        [JsonIgnore]
        public string Language
        {
            get => Lang;
            set => Lang = value;
        }
    }
}
