using System;
using System.IO;

namespace NepTunnel.Services;

public static class PluginInstaller
{
	public static bool EnsurePluginInstalled(out string statusMessage)
	{
		try
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Plugins");
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "NepBridgePlugin.lua");
			bool flag = File.Exists(text2);
			string text3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled_assets", "NepBridgePlugin.lua");
			if (File.Exists(text3))
			{
				File.Copy(text3, text2, overwrite: true);
				if (flag)
				{
					statusMessage = "✓ Plugin e inyector de nombres reemplazado correctamente en Roblox Studio.";
				}
				else
				{
					statusMessage = "✓ Plugin e inyector de nombres instalado correctamente en Roblox Studio.";
				}
				Console.WriteLine("[PluginInstaller] " + statusMessage);
				return true;
			}
			statusMessage = "✗ No se encontró el archivo del plugin original.";
			return false;
		}
		catch (Exception ex)
		{
			statusMessage = "✗ Error al instalar el plugin: " + ex.Message;
			Console.WriteLine("[PluginInstaller] " + statusMessage);
			return false;
		}
	}

	public static void EnsurePluginInstalled()
	{
		EnsurePluginInstalled(out string _);
	}
}
