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
            string listenerPrefix = redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/";

            using var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add(listenerPrefix);
                listener.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DiscordAuthService] HttpListener Error: {ex.Message}");
                // Fallback to dynamic port if 5000 is occupied
                return null;
            }

            string oauthUrl = $"https://discord.com/oauth2/authorize?client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify%20guilds%20guilds.members.read";

            try
            {
                // Open browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = oauthUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DiscordAuthService] Failed to open browser: {ex.Message}");
                listener.Stop();
                return null;
            }

            // Wait for incoming HTTP request
            var context = await listener.GetContextAsync();
            var req = context.Request;
            var resp = context.Response;

            string? code = req.QueryString["code"];

            // Send friendly HTML response to browser
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
            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            resp.OutputStream.Close();
            listener.Stop();

            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            // Exchange code for access token
            return await ExchangeCodeForTokenAsync(code, redirectUri);
        }

        private async Task<string?> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            try
            {
                using var client = new HttpClient();
                var values = new Dictionary<string, string>
                {
                    { "client_id", _config.ClientId },
                    { "client_secret", _config.ClientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", redirectUri }
                };

                var content = new FormUrlEncodedContent(values);
                var tokenResp = await client.PostAsync("https://discord.com/api/v10/oauth2/token", content);
                if (!tokenResp.IsSuccessStatusCode)
                {
                    string errStr = await tokenResp.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[DiscordAuthService] Token Exchange Failed: {errStr}");
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
