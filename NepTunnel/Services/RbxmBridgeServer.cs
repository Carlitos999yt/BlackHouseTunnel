using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace NepTunnel.Services;

public static class RbxmBridgeServer
{
	public const int BRIDGE_PORT = 7878;

	private static readonly string StagingDir = Path.Combine(Path.GetTempPath(), "rbxm_bridge");

	private static string? _bridgePending = null;

	private static readonly object LockObj = new object();

	private static HttpListener? _listener = null;

	private static bool _isRunning = false;

	private static readonly ConcurrentQueue<string> ClientNicknamesQueue = new ConcurrentQueue<string>();

	public static string ActiveUsername { get; set; } = "Player";

	public static string ActiveUid { get; set; } = "0";

	public static bool ScriptsImported { get; set; } = false;

	public static bool ForceScriptImport { get; set; } = false;

	public static bool IsRunning => _isRunning;

	public static void RegisterClientNickname(string nickname)
	{
		if (!string.IsNullOrWhiteSpace(nickname) && nickname != "Player" && nickname != "<ur user id here>")
		{
			ClientNicknamesQueue.Enqueue(nickname.Trim());
			Logger.Log("[Bridge] Registered remote client nickname: '" + nickname.Trim() + "'");
		}
	}

	public static bool Start()
	{
		if (_isRunning && _listener != null && _listener.IsListening)
		{
			return true;
		}

		Stop();

		Directory.CreateDirectory(StagingDir);

		for (int attempt = 1; attempt <= 3; attempt++)
		{
			try
			{
				_listener = new HttpListener();
				_listener.Prefixes.Add($"http://127.0.0.1:{BRIDGE_PORT}/");
				_listener.Start();
				_isRunning = true;
				Task.Run((Func<Task?>)ListenLoop);
				Logger.Log($"[Bridge] Started HTTP Bridge Server on port {BRIDGE_PORT}.");
				return true;
			}
			catch (Exception ex)
			{
				Stop();
				Logger.Log($"[Bridge] Start attempt {attempt} failed: {ex.Message}");
				if (attempt < 3)
				{
					System.Threading.Thread.Sleep(150);
				}
			}
		}

		try
		{
			using (var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMilliseconds(500) })
			{
				var resp = client.GetAsync($"http://127.0.0.1:{BRIDGE_PORT}/identity").Result;
				if (resp.IsSuccessStatusCode)
				{
					_isRunning = true;
					Logger.Log($"[Bridge] Detected existing active Bridge Server on port {BRIDGE_PORT}.");
					return true;
				}
			}
		}
		catch
		{
		}

		_isRunning = false;
		return false;
	}

	private static async Task ListenLoop()
	{
		while (_isRunning && _listener != null && _listener.IsListening)
		{
			try
			{
				HttpListenerContext context = await _listener.GetContextAsync();
				_ = Task.Run(delegate
				{
					HandleRequest(context);
				});
			}
			catch
			{
				if (!_isRunning)
				{
					break;
				}
			}
		}
	}

	private static void SendJson(HttpListenerResponse res, int statusCode, object obj)
	{
		try
		{
			string s = JsonSerializer.Serialize(obj);
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			res.StatusCode = statusCode;
			res.ContentType = "application/json";
			res.ContentLength64 = bytes.Length;
			res.AddHeader("Access-Control-Allow-Origin", "*");
			res.OutputStream.Write(bytes, 0, bytes.Length);
			res.Close();
		}
		catch
		{
		}
	}

	private static void HandleRequest(HttpListenerContext context)
	{
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		if (request.HttpMethod == "OPTIONS")
		{
			response.StatusCode = 204;
			response.AddHeader("Access-Control-Allow-Origin", "*");
			response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
			response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
			response.Close();
			return;
		}
		string text = request.Url?.AbsolutePath ?? "/";
		if (request.HttpMethod == "GET")
		{
			switch (text)
			{
			case "/identity":
			case "/user":
			{
				string text2 = request.QueryString["role"] ?? "";
				string text3;
				switch (text2)
				{
				case "host":
					text3 = (string.IsNullOrWhiteSpace(ActiveUsername) ? "Player" : ActiveUsername);
					break;
				case "client":
				case "next":
				{
					text3 = (!ClientNicknamesQueue.TryDequeue(out string? result2) || result2 == null) ? "Player" : result2;
					break;
				}
				default:
				{
					text3 = (!ClientNicknamesQueue.TryDequeue(out string? result) || result == null) ? (string.IsNullOrWhiteSpace(ActiveUsername) ? "Player" : ActiveUsername) : result;
					break;
				}
				}
				string text4 = (string.IsNullOrWhiteSpace(ActiveUid) ? "1000" : ActiveUid);
				bool forceScriptImport = ForceScriptImport;
				ForceScriptImport = false;
				Logger.Log($"[Bridge] Roblox Studio queried identity (role='{text2}') -> Name: '{text3}', UID: '{text4}', force='{forceScriptImport}'");
				SendJson(response, 200, new
				{
					status = "ok",
					name = text3,
					displayName = text3,
					uid = text4,
					imported = ScriptsImported,
					force_import = forceScriptImport
				});
				break;
			}
			case "/poll":
			{
				string? bridgePending2;
				lock (LockObj)
				{
					bridgePending2 = _bridgePending;
				}
				if (bridgePending2 == null)
				{
					SendJson(response, 200, new
					{
						status = "idle"
					});
				}
				else
				{
					SendJson(response, 200, new
					{
						status = "ready",
						name = bridgePending2,
						staging_dir = StagingDir
					});
				}
				break;
			}
			case "/download":
			{
				string? bridgePending;
				lock (LockObj)
				{
					bridgePending = _bridgePending;
				}
				if (bridgePending == null)
				{
					SendJson(response, 404, new
					{
						error = "no file pending"
					});
					break;
				}
				string fileName = Path.GetFileName(bridgePending);
				string fullPath = Path.GetFullPath(Path.Combine(StagingDir, fileName));
				string fullPath2 = Path.GetFullPath(StagingDir);
				if (!fullPath.StartsWith(fullPath2, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
				{
					SendJson(response, 404, new
					{
						error = "staged file missing or invalid"
					});
					break;
				}
				try
				{
					byte[] array = File.ReadAllBytes(fullPath);
					response.StatusCode = 200;
					response.ContentType = "application/octet-stream";
					response.ContentLength64 = array.Length;
					response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
					response.AddHeader("Access-Control-Allow-Origin", "*");
					response.OutputStream.Write(array, 0, array.Length);
					response.Close();
					break;
				}
				catch (Exception ex)
				{
					SendJson(response, 500, new
					{
						error = ex.Message
					});
					break;
				}
			}
			default:
				SendJson(response, 404, new
				{
					error = "not found"
				});
				break;
			}
			return;
		}
		if (request.HttpMethod == "POST")
		{
			try
			{
				using StreamReader streamReader = new StreamReader(request.InputStream, request.ContentEncoding);
				JsonNode? jsonNode = JsonNode.Parse(streamReader.ReadToEnd());
				if (text == "/queue")
				{
					string text5 = jsonNode?["path"]?.ToString() ?? "";
					string text6 = Path.GetExtension(text5).ToLowerInvariant();
					if (text6 != ".rbxm" && text6 != ".rbxmx")
					{
						SendJson(response, 400, new
						{
							error = "invalid file type: only .rbxm and .rbxmx supported"
						});
					}
					else if (!File.Exists(text5))
					{
						SendJson(response, 400, new
						{
							error = "file not found: " + text5
						});
					}
					else
					{
						string fileName2 = Path.GetFileName(text5);
						string text7 = Path.Combine(StagingDir, fileName2);
						File.Copy(text5, text7, overwrite: true);
						lock (LockObj)
						{
							_bridgePending = fileName2;
						}
						SendJson(response, 200, new
						{
							status = "queued",
							staged = text7
						});
					}
				}
				else if (text == "/clear")
				{
					lock (LockObj)
					{
						_bridgePending = null;
					}
					SendJson(response, 200, new
					{
						status = "cleared"
					});
				}
				else
				{
					SendJson(response, 404, new
					{
						error = "not found"
					});
				}
				return;
			}
			catch (Exception ex2)
			{
				SendJson(response, 500, new
				{
					error = ex2.Message
				});
				return;
			}
		}
		SendJson(response, 404, new
		{
			error = "not found"
		});
	}

	public static (bool ok, string message) QueueRbxm(string path)
	{
		string text = Path.GetExtension(path).ToLowerInvariant();
		if (text != ".rbxm" && text != ".rbxmx")
		{
			return (ok: false, message: "Only .rbxm and .rbxmx files are allowed");
		}
		if (!File.Exists(path))
		{
			return (ok: false, message: "File not found");
		}
		Start();
		string fileName = Path.GetFileName(path);
		string destFileName = Path.Combine(StagingDir, fileName);
		try
		{
			File.Copy(path, destFileName, overwrite: true);
		}
		catch (Exception ex)
		{
			return (ok: false, message: ex.Message);
		}
		lock (LockObj)
		{
			_bridgePending = fileName;
		}
		return (ok: true, message: fileName);
	}

	public static void Stop()
	{
		_isRunning = false;
		try
		{
			_listener?.Stop();
			_listener?.Close();
		}
		catch
		{
		}
		_listener = null;
	}
}
