using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NepTunnel.Services;

public class NepConfig
{
	[JsonPropertyName("uid")]
	public string Uid { get; set; } = "1344077747";

	[JsonPropertyName("username")]
	public string Username { get; set; } = "";

	[JsonPropertyName("port")]
	public string Port { get; set; } = "55555";

	[JsonPropertyName("host_addr")]
	public string HostAddr { get; set; } = "lost-programs.gl.at.ply.gg:20573";

	[JsonPropertyName("join_addr")]
	public string JoinAddr { get; set; } = "";

	[JsonPropertyName("addr")]
	public string Addr
	{
		get
		{
			if (string.IsNullOrEmpty(HostAddr))
			{
				return "lost-programs.gl.at.ply.gg:20573";
			}
			return HostAddr;
		}
		set
		{
			HostAddr = value;
		}
	}

	[JsonPropertyName("studio")]
	public string Studio { get; set; } = "";

	[JsonPropertyName("map")]
	public string Map { get; set; } = "";

	[JsonPropertyName("import_scripts")]
	public bool ImportScripts { get; set; }

	[JsonPropertyName("language")]
	public string Language { get; set; } = "en";

	[JsonPropertyName("saved_maps")]
	public List<string> SavedMaps { get; set; } = new List<string>();
}
