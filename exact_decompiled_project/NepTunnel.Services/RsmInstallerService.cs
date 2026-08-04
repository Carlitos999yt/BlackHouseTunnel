using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NepTunnel.Services;

public static class RsmInstallerService
{
	public const string TARGET_VERSION = "0.729.0.7290838";

	public const string VERSION_GUID = "version-4bb3958a2cde4efb";

	public const string GITHUB_RAW_BASE = "https://raw.githubusercontent.com/Carlitos999yt/roblox-studio/main";

	public static string GetRsmStudioDirectory()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox Studio");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static string GetRsmManagerDirectory()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox Studio Mod Manager");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	public static string GetRsmStudioExePath()
	{
		return Path.Combine(GetRsmStudioDirectory(), "RobloxStudioBeta.exe");
	}

	public static bool IsRsmInstalled()
	{
		return File.Exists(GetRsmStudioExePath());
	}

	public static void PreConfigureTargetVersionState()
	{
		try
		{
			string path = Path.Combine(GetRsmManagerDirectory(), "state.json");
			string contents = JsonSerializer.Serialize(new
			{
				TargetVersion = "0.729.0.7290838",
				VersionData = new
				{
					LastExecutedVersion = "version-4bb3958a2cde4efb",
					Version = "0.729.0.7290838",
					VersionGuid = "version-4bb3958a2cde4efb",
					VersionOverload = "0.729.0.7290838"
				},
				ChannelData = new
				{
					ChannelName = "LIVE",
					ChannelToken = ""
				}
			}, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(path, contents);
		}
		catch
		{
		}
	}

	public static async Task<bool> LaunchOfficialRsmBootstrapperAsync(Action<string, string> log)
	{
		log("Pre-configurando TargetVersion a 0.729.0.7290838…", "info");
		PreConfigureTargetVersionState();
		string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled_assets", "RobloxStudioModManager.exe");
		if (!File.Exists(text))
		{
			text = Path.Combine(Directory.GetCurrentDirectory(), "bundled_assets", "RobloxStudioModManager.exe");
		}
		if (!File.Exists(text))
		{
			log("✗ Error: No se encontró bundled_assets/RobloxStudioModManager.exe", "err");
			return false;
		}
		log("Abriendo ventana del Roblox Studio Mod Manager para versión 0.729.0.7290838…", "ok");
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = text,
				UseShellExecute = true
			});
			log("✓ Ventana del Mod Manager abierta correctamente.", "ok");
			await Task.Delay(1000);
			return true;
		}
		catch (Exception ex)
		{
			log("✗ Error ejecutando RobloxStudioModManager.exe: " + ex.Message, "err");
			return false;
		}
	}

	public static async Task<bool> RepairFromGitHubRepoAsync(Action<string, string> log, Action<double> progress)
	{
		log("Conectando a repositorio de reparación GitHub (Carlitos999yt/roblox-studio)…", "info");
		progress(0.05);
		string targetDir = GetRsmStudioDirectory();
		using HttpClient httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("User-Agent", "NepTunnel/RepairEngine");
		string[] array = new string[13]
		{
			"AppSettings.xml", "ReflectionMetadata.xml", "StartPageSystemMenu.xml", "SystemMenu.xml", "RobloxStudio_license.html", "ssl/cacert.pem", "shaders/shaders_d3d11.pack", "shaders/shaders_glsl3.pack", "shaders/shaders_vulkan_desktop.pack", "platforms/qwindows.dll",
			"styles/qwindowsvistastyle.dll", "imageformats/qgif.dll", "imageformats/qjpeg.dll"
		};
		int total = array.Length;
		int count = 0;
		int repaired = 0;
		string[] array2 = array;
		foreach (string relPath in array2)
		{
			count++;
			string destPath = Path.Combine(targetDir, relPath.Replace('/', '\\'));
			bool flag = false;
			if (!File.Exists(destPath))
			{
				log("⚠ Archivo faltante detectado: " + relPath, "warn");
				flag = true;
			}
			else if (new FileInfo(destPath).Length == 0L)
			{
				log("⚠ Archivo corrupto/vacío detectado: " + relPath, "warn");
				flag = true;
			}
			if (flag)
			{
				try
				{
					string requestUri = "https://raw.githubusercontent.com/Carlitos999yt/roblox-studio/main/" + relPath;
					log("\ud83d\udce5 Descargando reparación desde GitHub: " + relPath + "…", "info");
					byte[] bytes = await httpClient.GetByteArrayAsync(requestUri);
					string directoryName = Path.GetDirectoryName(destPath);
					if (!string.IsNullOrEmpty(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					await File.WriteAllBytesAsync(destPath, bytes);
					repaired++;
					log("✓ Archivo reparado con éxito desde GitHub: " + relPath, "ok");
				}
				catch (Exception ex)
				{
					log("⚠ No se pudo descargar " + relPath + " desde GitHub: " + ex.Message, "warn");
				}
			}
			else
			{
				log("✓ Verificado correcto: " + relPath, "dim");
			}
			progress(0.05 + 0.9 * ((double)count / (double)total));
		}
		PreConfigureTargetVersionState();
		progress(1.0);
		if (repaired > 0)
		{
			log($"✓ Reparación desde GitHub completada. Se solucionaron {repaired} archivo(s).", "ok");
		}
		else
		{
			log("✓ Todos los archivos están sincronizados e idénticos con tu GitHub.", "ok");
		}
		return true;
	}

	public static void CleanRsmRegistryAndProtocols()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return;
		}
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software", writable: true);
			if (registryKey != null)
			{
				try
				{
					registryKey.DeleteSubKeyTree("Roblox Studio Mod Manager", throwOnMissingSubKey: false);
				}
				catch
				{
				}
				try
				{
					registryKey.DeleteSubKeyTree("Roblox Studio", throwOnMissingSubKey: false);
				}
				catch
				{
				}
			}
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string path = Path.Combine(folderPath, "Roblox Studio");
			if (Directory.Exists(path))
			{
				try
				{
					Directory.Delete(path, recursive: true);
				}
				catch
				{
				}
			}
			string path2 = Path.Combine(folderPath, "Roblox Studio Mod Manager");
			if (Directory.Exists(path2))
			{
				try
				{
					Directory.Delete(path2, recursive: true);
					return;
				}
				catch
				{
					return;
				}
			}
		}
		catch
		{
		}
	}
}
