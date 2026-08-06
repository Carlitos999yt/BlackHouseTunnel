using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class DiscordAuthService
    {
        private readonly AppConfig _config;

        public DiscordAuthService(AppConfig config)
        {
            _config = config;
        }

        public async Task<string?> AuthenticateAsync()
        {
            int port = _config.LocalServerPort > 0 ? _config.LocalServerPort : 5000;
            string redirectUri = !string.IsNullOrWhiteSpace(_config.RedirectUri) ? _config.RedirectUri : $"http://localhost:{port}/callback";

            using var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add($"http://localhost:{port}/callback/");
                listener.Prefixes.Add($"http://127.0.0.1:{port}/callback/");
                listener.Start();
            }
            catch
            {
                try
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add($"http://localhost:{port}/callback/");
                    listener.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DiscordAuthService] HttpListener Error: {ex.Message}");
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        DarkMessageBox.Show($"No se pudo iniciar el servidor local de inicio de sesión en http://localhost:{port}/callback/.\n\n" +
                                            $"Causa: El puerto {port} está ocupado por otra aplicación en tu PC o bloqueado por el Antivirus/Firewall de Windows.\n\n" +
                                            $"Solución: Cierra aplicaciones en segundo plano que usen el puerto {port} o permite la conexión en el Firewall.",
                                            "Error Receptor Local",
                                            System.Windows.MessageBoxButton.OK,
                                            System.Windows.MessageBoxImage.Error);
                    });
                    return null;
                }
            }

            bool useCodeFlow = !string.IsNullOrWhiteSpace(_config.ClientSecret);
            string oauthUrl = useCodeFlow
                ? $"https://discord.com/oauth2/authorize?client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20guilds%20guilds.members.read"
                : $"https://discord.com/oauth2/authorize?client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=token&scope=identify%20guilds%20guilds.members.read";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = oauthUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DiscordAuthService] Failed to open browser: {ex.Message}");
                try { listener.Stop(); } catch { }
                return null;
            }

            if (useCodeFlow)
            {
                return await HandleCodeFlowAsync(listener, redirectUri);
            }
            else
            {
                return await HandleImplicitFlowAsync(listener);
            }
        }

        private async Task<string?> HandleCodeFlowAsync(HttpListener listener, string redirectUri)
        {
            HttpListenerContext context;
            try
            {
                var getContextTask = listener.GetContextAsync();
                var timeoutTask = Task.Delay(120000); // 2 minutes

                var completedTask = await Task.WhenAny(getContextTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    try { listener.Stop(); } catch { }
                    return null;
                }

                context = await getContextTask;
            }
            catch
            {
                try { listener.Stop(); } catch { }
                return null;
            }

            var req = context.Request;
            var resp = context.Response;
            string? code = req.QueryString["code"];

            SendSuccessHtml(resp);
            try { listener.Stop(); } catch { }

            if (string.IsNullOrEmpty(code)) return null;

            string usedRedirectUri = req.Url != null ? req.Url.GetLeftPart(UriPartial.Path).TrimEnd('/') : redirectUri.TrimEnd('/');
            return await ExchangeCodeForTokenAsync(code, usedRedirectUri);
        }

        private async Task<string?> HandleImplicitFlowAsync(HttpListener listener)
        {
            string? tokenFound = null;
            DateTime endTime = DateTime.Now.AddMinutes(2);

            while (DateTime.Now < endTime && tokenFound == null)
            {
                HttpListenerContext context;
                try
                {
                    var getContextTask = listener.GetContextAsync();
                    var timeoutTask = Task.Delay((int)(endTime - DateTime.Now).TotalMilliseconds);

                    var completedTask = await Task.WhenAny(getContextTask, timeoutTask);
                    if (completedTask == timeoutTask) break;

                    context = await getContextTask;
                }
                catch
                {
                    break;
                }

                var req = context.Request;
                var resp = context.Response;

                if (req.Url != null && req.Url.AbsolutePath.Contains("/token"))
                {
                    tokenFound = req.QueryString["access_token"];
                    SendSuccessHtml(resp);
                    try { listener.Stop(); } catch { }
                    break;
                }
                else
                {
                    // Serve JS script to forward hash parameters to /token query
                    string jsBridgeHtml = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>BlackHouseTunnel - Autenticación</title>
    <script>
        if (window.location.hash) {
            var hash = window.location.hash.substring(1);
            window.location.href = window.location.protocol + '//' + window.location.host + '/callback/token?' + hash;
        }
    </script>
    <style>
        body { background-color: #0d0d11; color: #ffffff; font-family: 'Segoe UI', sans-serif; display: flex; height: 100vh; justify-content: center; align-items: center; margin: 0; }
        .card { background: #181820; padding: 40px; border-radius: 16px; border: 1px solid #5865F2; box-shadow: 0 0 30px rgba(88, 101, 242, 0.4); text-align: center; max-width: 400px; }
        h1 { color: #5865F2; font-size: 22px; margin-bottom: 10px; }
        p { color: #aaaaaa; font-size: 14px; }
    </style>
</head>
<body>
    <div class='card'>
        <h1>Procesando Autenticación...</h1>
        <p>Por favor espera un segundo mientras nos conectamos con <strong>BlackHouseTunnel</strong>.</p>
    </div>
</body>
</html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(jsBridgeHtml);
                    resp.ContentLength64 = buffer.Length;
                    resp.ContentType = "text/html";
                    await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    resp.OutputStream.Close();
                }
            }

            try { listener.Stop(); } catch { }
            return tokenFound;
        }

        private void SendSuccessHtml(HttpListenerResponse resp)
        {
            try
            {
                string htmlResponse = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>BlackHouseTunnel - Autenticación</title>
    <style>
        body { background-color: #0d0d11; color: #ffffff; font-family: 'Segoe UI', sans-serif; display: flex; height: 100vh; justify-content: center; align-items: center; margin: 0; }
        .card { background: #181820; padding: 40px; border-radius: 16px; border: 1px solid #5865F2; box-shadow: 0 0 30px rgba(88, 101, 242, 0.4); text-align: center; max-width: 400px; }
        h1 { color: #5865F2; font-size: 24px; margin-bottom: 10px; }
        p { color: #aaaaaa; font-size: 15px; }
    </style>
</head>
<body>
    <div class='card'>
        <h1>¡Autenticación Exitosa!</h1>
        <p>Tu inicio de sesión con Discord en <strong>BlackHouseTunnel</strong> se ha completado.</p>
        <p>Puedes cerrar esta pestaña y volver a la aplicación.</p>
    </div>
</body>
</html>";
                byte[] buffer = Encoding.UTF8.GetBytes(htmlResponse);
                resp.ContentLength64 = buffer.Length;
                resp.ContentType = "text/html";
                resp.OutputStream.Write(buffer, 0, buffer.Length);
                resp.OutputStream.Close();
            }
            catch { }
        }

        private async Task<string?> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            try
            {
                using var client = new HttpClient();
                var values = new Dictionary<string, string>
                {
                    { "client_id", _config.ClientId },
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", redirectUri }
                };

                if (!string.IsNullOrWhiteSpace(_config.ClientSecret))
                {
                    values["client_secret"] = _config.ClientSecret;
                }

                var content = new FormUrlEncodedContent(values);
                var tokenResp = await client.PostAsync("https://discord.com/api/v10/oauth2/token", content);
                if (!tokenResp.IsSuccessStatusCode)
                {
                    string errStr = await tokenResp.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[DiscordAuthService] Token Exchange Failed: {errStr}");
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        DarkMessageBox.Show($"Discord no pudo completar el canje del Token (HTTP {(int)tokenResp.StatusCode}).\n\n" +
                                            $"Respuesta de Discord: {errStr}\n\n" +
                                            $"URI usada: {redirectUri}",
                                            "Error de Autenticación Discord",
                                            System.Windows.MessageBoxButton.OK,
                                            System.Windows.MessageBoxImage.Error);
                    });
                    return null;
                }

                string jsonStr = await tokenResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("access_token", out var tokenElem))
                {
                    return tokenElem.GetString();
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DiscordAuthService] Exchange Exception: {ex.Message}");
                return null;
            }
        }
    }
}
