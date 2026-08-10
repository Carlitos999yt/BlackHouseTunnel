using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public static class DiscordRpcService
    {
        private const string CLIENT_ID = "1534613209523294349";
        private static NamedPipeClientStream? _pipe;
        private static bool _isConnected = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            Task.Run(() => ConnectPipe());
        }

        private static void ConnectPipe()
        {
            lock (_lock)
            {
                if (_isConnected) return;
                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut);
                            pipe.Connect(500);
                            _pipe = pipe;
                            _isConnected = true;
                            break;
                        }
                        catch { }
                    }

                    if (!_isConnected || _pipe == null) return;

                    // Send Handshake Payload (Opcode 0)
                    var handshakePayload = JsonSerializer.Serialize(new { v = 1, client_id = CLIENT_ID });
                    WriteFrame(0, handshakePayload);

                    // Read Handshake Reply
                    ReadFrame();

                    // Update Initial Activity
                    SetPresenceInMenu();
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DiscordRPC] Connection error: {ex.Message}");
                    _isConnected = false;
                    _pipe = null;
                }
            }
        }

        private static void WriteFrame(int op, string json)
        {
            if (_pipe == null || !_pipe.IsConnected) return;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                byte[] header = new byte[8];
                BitConverter.GetBytes(op).CopyTo(header, 0);
                BitConverter.GetBytes(bytes.Length).CopyTo(header, 4);

                _pipe.Write(header, 0, 8);
                _pipe.Write(bytes, 0, bytes.Length);
                _pipe.Flush();
            }
            catch
            {
                _isConnected = false;
            }
        }

        private static string ReadFrame()
        {
            if (_pipe == null || !_pipe.IsConnected) return "";
            try
            {
                byte[] header = new byte[8];
                int read = _pipe.Read(header, 0, 8);
                if (read < 8) return "";
                int len = BitConverter.ToInt32(header, 4);
                byte[] buffer = new byte[len];
                _pipe.Read(buffer, 0, len);
                return Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                _isConnected = false;
                return "";
            }
        }

        public static void UpdatePresence(string details, string state, string largeImageKey = "logo", string largeImageText = "BlackHouse Tunnel")
        {
            var config = ConfigManager.CurrentConfig;
            if (!config.EnableDiscordRpc)
            {
                ClearPresence();
                return;
            }

            if (!_isConnected)
            {
                ConnectPipe();
                if (!_isConnected) return;
            }

            try
            {
                var nonce = Guid.NewGuid().ToString();
                var activity = new
                {
                    cmd = "SET_ACTIVITY",
                    args = new
                    {
                        pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                        activity = new
                        {
                            details = details,
                            state = state,
                            assets = new
                            {
                                large_image = largeImageKey,
                                large_text = largeImageText
                            }
                        }
                    },
                    nonce = nonce
                };

                string json = JsonSerializer.Serialize(activity);
                WriteFrame(1, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"[DiscordRPC] Update error: {ex.Message}");
            }
        }

        public static void SetPresenceInMenu()
        {
            UpdatePresence("En el Menú Principal", "BlackHouse Tunnel Active");
        }

        public static void SetPresenceHosting(string serverName)
        {
            string name = string.IsNullOrWhiteSpace(serverName) ? "Servidor Privado" : serverName;
            UpdatePresence($"Hosteando: {name}", "Servidor de Roblox Activo");
        }

        public static void SetPresenceConnected(string serverName, bool isRegisteredTunnel = true)
        {
            if (isRegisteredTunnel && !string.IsNullOrWhiteSpace(serverName))
            {
                UpdatePresence($"Conectado a: {serverName}", "Jugando en Túnel de Host");
            }
            else
            {
                UpdatePresence("Conectado a un Host", "Jugando en Túnel Remoto");
            }
        }

        public static void ClearPresence()
        {
            if (!_isConnected) return;
            try
            {
                var nonce = Guid.NewGuid().ToString();
                var activity = new
                {
                    cmd = "SET_ACTIVITY",
                    args = new
                    {
                        pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                        activity = (object?)null
                    },
                    nonce = nonce
                };
                string json = JsonSerializer.Serialize(activity);
                WriteFrame(1, json);
            }
            catch { }
        }
    }
}
