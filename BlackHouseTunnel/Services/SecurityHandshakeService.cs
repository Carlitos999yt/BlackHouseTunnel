using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class SecurityHandshakeService
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Valida en el lado del HOST si el cliente que intenta conectarse realmente
        /// posee un Access Token de Discord válido y es miembro activo del servidor exigido.
        /// Esto previene que usuarios con ejecutables modificados (hackeados) puedan conectarse.
        /// </summary>
        public async Task<bool> ValidateRemoteClientMembershipAsync(string clientAccessToken, string requiredGuildId)
        {
            if (string.IsNullOrWhiteSpace(clientAccessToken) || string.IsNullOrWhiteSpace(requiredGuildId))
            {
                return false;
            }

            try
            {
                // Petición directa a los servidores oficiales de Discord desde la máquina del HOST
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/users/@me/guilds/{requiredGuildId}/member");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientAccessToken);

                var resp = await HttpClient.SendAsync(req);

                // Si Discord responde 200 OK, el cliente es 100% legítimo y miembro del servidor.
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    // Verificación exitosa
                    return true;
                }

                // 404 Not Found u otro código significa que no es miembro o el token es falso
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecurityHandshake Error]: {ex.Message}");
                return false;
            }
        }
    }
}
