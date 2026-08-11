using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Services
{
    public class DiscordApiService
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public async Task<(bool Success, string ErrorMessage)> UpdateGuildMemberNicknameAsync(string accessToken, string guildId, string userId, string nickname, string botToken = "")
        {
            if (string.IsNullOrWhiteSpace(guildId)) guildId = "1529015986135502951";
            if (string.IsNullOrWhiteSpace(botToken)) botToken = TokenProtector.GetDefaultBotToken();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return (false, "No se encontró el ID de usuario de Discord.");
            }

            string lastError = "No se pudo conectar con la API de Discord.";

            try
            {
                // Method 1: Try User's OAuth Bearer token (modifies @me, which IS the user when using OAuth)
                if (!string.IsNullOrWhiteSpace(accessToken) && !accessToken.StartsWith("MTU"))
                {
                    var req = new HttpRequestMessage(HttpMethod.Patch, $"https://discord.com/api/v10/guilds/{guildId}/members/@me/nick");
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    req.Content = new StringContent(JsonSerializer.Serialize(new { nick = nickname }), System.Text.Encoding.UTF8, "application/json");
                    var resp = await HttpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        Logger.Log($"[DiscordApi] Updated nick for user {userId} via OAuth token.");
                        return (true, "");
                    }
                }

                // Method 2: Use Bot API targeting SPECIFIC USER ID (/guilds/{guildId}/members/{userId})
                // NEVER use @me with Bot Token as @me with Bot Token modifies the BOT itself!
                if (!string.IsNullOrWhiteSpace(botToken))
                {
                    var reqBot = new HttpRequestMessage(HttpMethod.Patch, $"https://discord.com/api/v10/guilds/{guildId}/members/{userId}");
                    reqBot.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                    reqBot.Content = new StringContent(JsonSerializer.Serialize(new { nick = nickname }), System.Text.Encoding.UTF8, "application/json");
                    var respBot = await HttpClient.SendAsync(reqBot);

                    if (respBot.IsSuccessStatusCode)
                    {
                        Logger.Log($"[DiscordApi] Successfully updated nick for user {userId} to '{nickname}' via Bot API.");
                        return (true, "");
                    }
                    else
                    {
                        string errBody = await respBot.Content.ReadAsStringAsync();
                        Logger.Log($"[DiscordApi] Bot nick update failed ({respBot.StatusCode}): {errBody}");

                        if (respBot.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            lastError = "Discord rechazó la acción (Error HTTP 403 Forbidden).\n\n⚠️ REGLA DE JERARQUÍA DE DISCORD:\nDiscord NO permite que un Bot cambie el apodo del Dueño del Servidor ni de usuarios que tengan un Rol ubicado por encima del Rol del Bot en la Jerarquía de Roles del Servidor.";
                        }
                        else if (respBot.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            lastError = "El Bot no tiene permisos suficientes para editar apodos en el Servidor (Asegúrate de otorgarle el permiso 'MANAGE_NICKNAMES').";
                        }
                        else
                        {
                            lastError = $"Discord devolvió el código HTTP {(int)respBot.StatusCode}: {errBody}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Logger.Log($"[DiscordApi] Error updating nick: {ex.Message}");
            }

            return (false, lastError);
        }

        public async Task<DiscordUser?> GetUserProfileAndGuildMemberAsync(string accessToken, string guildId, string botToken = "")
        {
            if (string.IsNullOrWhiteSpace(guildId)) guildId = "1529015986135502951";
            if (string.IsNullOrWhiteSpace(botToken)) botToken = TokenProtector.GetDefaultBotToken();

            try
            {
                // 1. Get User Profile (/users/@me)
                var userReq = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
                userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var userResp = await HttpClient.SendAsync(userReq);
                if (!userResp.IsSuccessStatusCode)
                    return null;

                string userJson = await userResp.Content.ReadAsStringAsync();
                using var userDoc = JsonDocument.Parse(userJson);
                var root = userDoc.RootElement;

                var user = new DiscordUser
                {
                    Id = root.GetProperty("id").GetString() ?? "",
                    Username = root.GetProperty("username").GetString() ?? "",
                    GlobalName = root.TryGetProperty("global_name", out var gName) && gName.ValueKind != JsonValueKind.Null 
                        ? gName.GetString() ?? "" 
                        : "",
                    Discriminator = root.TryGetProperty("discriminator", out var disc) ? disc.GetString() ?? "0" : "0",
                    AvatarHash = root.TryGetProperty("avatar", out var av) && av.ValueKind != JsonValueKind.Null 
                        ? av.GetString() 
                        : null
                };

                // 2. Check Member of Guild (Tri-level verification: Bot API -> User Guild Member API -> User Guilds List)
                string? memberJson = null;
                if (!string.IsNullOrWhiteSpace(botToken))
                {
                    try
                    {
                        var botMemberReq = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/guilds/{guildId}/members/{user.Id}");
                        botMemberReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                        var botMemberResp = await HttpClient.SendAsync(botMemberReq);
                        if (botMemberResp.IsSuccessStatusCode)
                        {
                            user.IsMemberOfGuild = true;
                            memberJson = await botMemberResp.Content.ReadAsStringAsync();
                        }
                    }
                    catch { }
                }

                if (memberJson == null)
                {
                    try
                    {
                        var memberReq = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/users/@me/guilds/{guildId}/member");
                        memberReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        var memberResp = await HttpClient.SendAsync(memberReq);
                        if (memberResp.IsSuccessStatusCode)
                        {
                            user.IsMemberOfGuild = true;
                            memberJson = await memberResp.Content.ReadAsStringAsync();
                        }
                    }
                    catch { }
                }

                // Fallback Level 3: Check /users/@me/guilds list (Universal Discord `guilds` scope check)
                if (!user.IsMemberOfGuild)
                {
                    try
                    {
                        var guildsReq = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me/guilds");
                        guildsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        var guildsResp = await HttpClient.SendAsync(guildsReq);
                        if (guildsResp.IsSuccessStatusCode)
                        {
                            string gJson = await guildsResp.Content.ReadAsStringAsync();
                            using var gDoc = JsonDocument.Parse(gJson);
                            if (gDoc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var gElem in gDoc.RootElement.EnumerateArray())
                                {
                                    string id = gElem.GetProperty("id").GetString() ?? "";
                                    if (id.Equals(guildId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        user.IsMemberOfGuild = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(memberJson))
                {
                    using var memberDoc = JsonDocument.Parse(memberJson);
                    var mRoot = memberDoc.RootElement;

                    if (mRoot.TryGetProperty("nick", out var nickProp) && nickProp.ValueKind != JsonValueKind.Null)
                    {
                        user.ServerNick = nickProp.GetString() ?? "";
                    }

                    if (mRoot.TryGetProperty("roles", out var rolesElem) && rolesElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in rolesElem.EnumerateArray())
                        {
                            if (r.GetString() is string roleId)
                            {
                                user.RoleIds.Add(roleId);
                            }
                        }
                    }

                    await ResolveRolesAsync(user, guildId, botToken);
                }

                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordApiService Error]: {ex.Message}");
                return null;
            }
        }

        public async Task<List<DiscordRole>> FetchGuildRolesAsync(string guildId = "", string botToken = "")
        {
            if (string.IsNullOrWhiteSpace(guildId)) guildId = "1529015986135502951";
            if (string.IsNullOrWhiteSpace(botToken)) botToken = TokenProtector.GetDefaultBotToken();

            var list = new List<DiscordRole>();
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/guilds/{guildId}/roles");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                var resp = await HttpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        string id = elem.GetProperty("id").GetString() ?? "";
                        string name = elem.GetProperty("name").GetString() ?? "";
                        int pos = elem.TryGetProperty("position", out var p) ? p.GetInt32() : 0;
                        uint col = elem.TryGetProperty("color", out var c) ? c.GetUInt32() : 0;
                        if (!string.IsNullOrEmpty(id) && !name.Equals("@everyone", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new DiscordRole { Id = id, Name = name, Position = pos, Color = col });
                        }
                    }
                    list.Sort((a, b) => b.Position.CompareTo(a.Position));
                }
            }
            catch { }
            return list;
        }

        public async Task<List<DiscordUser>> GetGuildOnlineMembersAsync(string guildId, string botToken)
        {
            var membersList = new List<DiscordUser>();
            if (string.IsNullOrWhiteSpace(guildId) || string.IsNullOrWhiteSpace(botToken))
            {
                return membersList;
            }

            try
            {
                // Fetch Guild Roles
                Dictionary<string, (string Name, uint Color)> roleMap = new();
                var rolesReq = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/guilds/{guildId}/roles");
                rolesReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                var rolesResp = await HttpClient.SendAsync(rolesReq);
                if (rolesResp.IsSuccessStatusCode)
                {
                    string rolesJson = await rolesResp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(rolesJson);
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        string rId = elem.GetProperty("id").GetString() ?? "";
                        string rName = elem.GetProperty("name").GetString() ?? "";
                        uint rColor = elem.TryGetProperty("color", out var c) ? c.GetUInt32() : 0;
                        roleMap[rId] = (rName, rColor);
                    }
                }

                // Fetch Guild Members (/guilds/{guildId}/members?limit=1000)
                var membersReq = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/guilds/{guildId}/members?limit=1000");
                membersReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                var membersResp = await HttpClient.SendAsync(membersReq);

                if (membersResp.IsSuccessStatusCode)
                {
                    string membersJson = await membersResp.Content.ReadAsStringAsync();
                    using var mDoc = JsonDocument.Parse(membersJson);
                    foreach (var elem in mDoc.RootElement.EnumerateArray())
                    {
                        if (elem.TryGetProperty("user", out var uElem))
                        {
                            var dUser = new DiscordUser
                            {
                                Id = uElem.GetProperty("id").GetString() ?? "",
                                Username = uElem.GetProperty("username").GetString() ?? "",
                                GlobalName = uElem.TryGetProperty("global_name", out var g) && g.ValueKind != JsonValueKind.Null ? g.GetString() ?? "" : "",
                                AvatarHash = uElem.TryGetProperty("avatar", out var av) && av.ValueKind != JsonValueKind.Null ? av.GetString() : null
                            };

                            if (elem.TryGetProperty("nick", out var n) && n.ValueKind != JsonValueKind.Null)
                            {
                                dUser.ServerNick = n.GetString() ?? "";
                            }

                            if (elem.TryGetProperty("roles", out var rArray) && rArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var r in rArray.EnumerateArray())
                                {
                                    if (r.GetString() is string rId)
                                    {
                                        dUser.RoleIds.Add(rId);
                                        if (roleMap.TryGetValue(rId, out var rInfo))
                                        {
                                            dUser.RoleNames.Add(rInfo.Name);
                                        }
                                    }
                                }
                            }

                            DeterminePrimaryRoleAndSpecialFlags(dUser);
                            membersList.Add(dUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordApiService GetGuildOnlineMembersAsync Error]: {ex.Message}");
            }

            return membersList;
        }

        private async Task ResolveRolesAsync(DiscordUser user, string guildId, string botToken)
        {
            Dictionary<string, (string Name, uint Color)> roleMap = new();

            if (!string.IsNullOrWhiteSpace(botToken))
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/guilds/{guildId}/roles");
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                    var resp = await HttpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            string rId = elem.GetProperty("id").GetString() ?? "";
                            string rName = elem.GetProperty("name").GetString() ?? "";
                            uint rColor = elem.TryGetProperty("color", out var c) ? c.GetUInt32() : 0;
                            roleMap[rId] = (rName, rColor);
                        }
                    }
                }
                catch
                {
                    // Fallback
                }
            }

            List<string> roleNames = new();
            foreach (var rId in user.RoleIds)
            {
                if (roleMap.TryGetValue(rId, out var info))
                {
                    roleNames.Add(info.Name);
                }
                else
                {
                    roleNames.Add(rId);
                }
            }
            user.RoleNames = roleNames;

            DeterminePrimaryRoleAndSpecialFlags(user);
        }

        public void DeterminePrimaryRoleAndSpecialFlags(DiscordUser user)
        {
            var namesLower = user.RoleNames.Select(r => r.ToLowerInvariant()).ToList();

            // Match by exact Discord Role ID or by Name substring
            user.IsOwner = user.RoleIds.Contains("1535464249814679663") || namesLower.Any(r => r.Contains("owner") || r.Contains("dueño"));
            user.IsMod = user.RoleIds.Contains("1530819254507802736") || namesLower.Any(r => r.Contains("mod") || r.Contains("moderador"));
            user.IsStaffOrAdmin = user.IsOwner || user.IsMod || user.RoleIds.Contains("1535464923998715994") || user.RoleIds.Contains("1529016731941601382") || namesLower.Any(r => r.Contains("staff") || r.Contains("superior") || r.Contains("admin"));
            user.IsPrivadito = user.RoleIds.Contains("1531465096302301305") || namesLower.Any(r => r.Contains("privadito"));
            user.IsHoster = user.RoleIds.Contains("1529275468535300168") || namesLower.Any(r => r.Contains("hoster") || r.Contains("host"));

            // Determine Primary Role (Priority Order: Owner -> Mod -> Staff -> Hoster -> Privadito -> Default)
            if (user.IsOwner || user.RoleIds.Contains("1535464249814679663") || namesLower.Any(r => r.Contains("owner") || r.Contains("dueño")))
            {
                user.PrimaryRole = "Owner";
                user.PrimaryRoleColor = "#FFD700"; // Gold
            }
            else if (user.IsMod || user.RoleIds.Contains("1530819254507802736") || namesLower.Any(r => r.Contains("mod") || r.Contains("moderador")))
            {
                user.PrimaryRole = "Mod";
                user.PrimaryRoleColor = "#A855F7"; // Morado
            }
            else if (user.RoleIds.Contains("1535464923998715994") || user.RoleIds.Contains("1529016731941601382") || namesLower.Any(r => r.Contains("staff") || r.Contains("superior") || r.Contains("admin")))
            {
                user.PrimaryRole = "Staff";
                user.PrimaryRoleColor = "#EF4444"; // Rojo
            }
            else if (user.IsHoster || user.RoleIds.Contains("1529275468535300168") || namesLower.Any(r => r.Contains("hoster")))
            {
                user.PrimaryRole = "Hoster";
                user.PrimaryRoleColor = "#9B59B6";
            }
            else if (user.IsPrivadito || user.RoleIds.Contains("1531465096302301305") || namesLower.Any(r => r.Contains("privadito")))
            {
                user.PrimaryRole = "Privadito";
                user.PrimaryRoleColor = "#F59E0B";
            }
            else if (namesLower.Any(r => r.Contains("usuario") || r.Contains("member")))
            {
                user.PrimaryRole = "Usuario";
                user.PrimaryRoleColor = "#5865F2";
            }
            else
            {
                string? firstNonNumeric = user.RoleNames.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r) && !r.All(char.IsDigit));
                user.PrimaryRole = !string.IsNullOrEmpty(firstNonNumeric) ? firstNonNumeric : "Usuario";
                user.PrimaryRoleColor = "#5865F2";
            }
        }

        public async Task<List<DiscordUser>> GetGuildWidgetMembersAsync(string guildId)
        {
            var list = new List<DiscordUser>();
            if (string.IsNullOrWhiteSpace(guildId)) return list;

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/guilds/{guildId}/widget.json");
                var resp = await HttpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("members", out var membersArr) && membersArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in membersArr.EnumerateArray())
                        {
                            string uname = m.GetProperty("username").GetString() ?? "";
                            string avatarUrl = m.TryGetProperty("avatar_url", out var av) && av.ValueKind != JsonValueKind.Null ? av.GetString() ?? "" : "";

                            if (!string.IsNullOrEmpty(uname))
                            {
                                var dUser = new DiscordUser
                                {
                                    Username = uname,
                                    ServerNick = uname,
                                    GlobalName = uname,
                                    DirectAvatarUrl = avatarUrl
                                };
                                list.Add(dUser);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return list;
        }

        public async Task<string?> PostTunnelEmbedToChannelAsync(string channelId, string botToken, PublishedTunnel tunnel)
        {
            if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(botToken)) return null;

            try
            {
                int color = 7045760; // Default Global
                if (!string.IsNullOrWhiteSpace(tunnel.CustomEmbedColorHex))
                {
                    try
                    {
                        string hex = tunnel.CustomEmbedColorHex.TrimStart('#');
                        color = Convert.ToInt32(hex, 16);
                    }
                    catch { color = 11032055; }
                }
                else
                {
                    color = tunnel.VisibilityMode switch
                    {
                        2 => 16766720, // Gold (Privadito)
                        1 => 5793266,  // Blurple/Cyan (Servidor)
                        _ => 7045760   // Dark Slate Gray (Global / Público)
                    };
                }

                string scopeText = !string.IsNullOrWhiteSpace(tunnel.CustomBadgeLabel)
                    ? tunnel.CustomBadgeLabel
                    : (!string.IsNullOrWhiteSpace(tunnel.CustomRuleName)
                        ? $"⚡ Regla: {tunnel.CustomRuleName}"
                        : tunnel.VisibilityMode switch
                        {
                            1 => "🛡️ Host para todo el Servidor",
                            2 => "🔒 Host Privado (Solo Privadito)",
                            _ => "🌐 Host Abierto (Público Global)"
                        });

                var fieldsList = new List<object>
                {
                    new { name = "🆔 ID del Host", value = $"`{tunnel.HostId}`", inline = true },
                    new { name = "👤 Creado / Hosteado por", value = tunnel.HostUsername, inline = true },
                    new { name = "🎯 Dirigido a / Alcance", value = scopeText, inline = true }
                };

                if (tunnel.RequiresAccessKey)
                {
                    fieldsList.Add(new { name = "🔑 Llave de Acceso", value = "🔒 Requiere Llave Privada", inline = false });
                }

                string embedTitle = tunnel.VisibilityMode == 2
                    ? $"🖥️ Servidor\u200B de Host: {tunnel.ServerName}"
                    : $"🖥️ Servidor de Host: {tunnel.ServerName}";

                var embedObj = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = embedTitle,
                            description = "🚀 **Túnel de Servidor Activo en BlackHouseTunnel**",
                            color = color,
                            fields = fieldsList.ToArray(),
                            footer = new { text = "BlackHouseTunnel • Sincronización en Vivo" },
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(embedObj);
                var req = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/v10/channels/{channelId}/messages");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                req.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var resp = await HttpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    string respJson = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(respJson);
                    return doc.RootElement.GetProperty("id").GetString();
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<string?> PostPlayitMappingEmbedAsync(string playitChannelId, string botToken, PublishedTunnel tunnel)
        {
            if (string.IsNullOrWhiteSpace(playitChannelId) || string.IsNullOrWhiteSpace(botToken)) return null;

            try
            {
                string playitTitle = tunnel.VisibilityMode == 2
                    ? $"🔗 PLAYIT\u200B_MAP:{tunnel.HostId}"
                    : $"🔗 PLAYIT_MAP:{tunnel.HostId}";

                var embedObj = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = playitTitle,
                            description = "Contenido de mapeo privado de red para BlackHouseTunnel",
                            color = 3447003,
                            fields = new[]
                            {
                                new { name = "HostId", value = tunnel.HostId, inline = true },
                                new { name = "ServerName", value = tunnel.ServerName, inline = true },
                                new { name = "Host", value = tunnel.HostUsername, inline = true },
                                new { name = "RemoteAddress", value = tunnel.RemoteAddress, inline = true },
                                new { name = "AccessKey", value = tunnel.AccessKey ?? "", inline = true },
                                new { name = "VisibilityMode", value = tunnel.VisibilityMode.ToString(), inline = true },
                                new { name = "CustomRuleName", value = tunnel.CustomRuleName ?? "", inline = true },
                                new { name = "CustomBadgeLabel", value = tunnel.CustomBadgeLabel ?? "", inline = true },
                                new { name = "CustomColorHex", value = tunnel.CustomEmbedColorHex ?? "", inline = true },
                                new { name = "AllowedRoleIds", value = string.Join(",", tunnel.AllowedRoleIds), inline = true },
                                new { name = "AllowedUserIds", value = string.Join(",", tunnel.AllowedUserIds), inline = true },
                                new { name = "MinAppVersion", value = string.IsNullOrWhiteSpace(tunnel.MinAppVersion) ? "1.3.1" : tunnel.MinAppVersion, inline = true }
                            },
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(embedObj);
                var req = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/v10/channels/{playitChannelId}/messages");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                req.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var resp = await HttpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    string respJson = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(respJson);
                    return doc.RootElement.GetProperty("id").GetString();
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<bool> DeleteTunnelEmbedAsync(string channelId, string botToken, string messageId)
        {
            if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(messageId)) return false;

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Delete, $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                var resp = await HttpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PublishedTunnel>> FetchChannelTunnelEmbedsAsync(string channelId, string botToken, string playitChannelId = "")
        {
            var list = new List<PublishedTunnel>();
            if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(botToken)) return list;

            // Map hostId -> PublishedTunnel meta from playit channel
            var playitMap = new Dictionary<string, PublishedTunnel>(StringComparer.OrdinalIgnoreCase);
            var serverNameMap = new Dictionary<string, PublishedTunnel>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(playitChannelId))
            {
                try
                {
                    var pReq = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/channels/{playitChannelId}/messages?limit=100");
                    pReq.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                    var pResp = await HttpClient.SendAsync(pReq);
                    if (pResp.IsSuccessStatusCode)
                    {
                        string pJson = await pResp.Content.ReadAsStringAsync();
                        using var pDoc = JsonDocument.Parse(pJson);
                        foreach (var msgElem in pDoc.RootElement.EnumerateArray())
                        {
                            if (msgElem.TryGetProperty("embeds", out var embedsArr) && embedsArr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var embed in embedsArr.EnumerateArray())
                                {
                                    string rawTitle = embed.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                                    string title = rawTitle.Replace("\u200B", "");
                                    if (!title.Contains("PLAYIT_MAP:", StringComparison.OrdinalIgnoreCase)) continue;

                                    string hId = title.Replace("🔗 PLAYIT_MAP:", "").Replace("PLAYIT_MAP:", "").Trim();
                                    var meta = new PublishedTunnel { HostId = hId };

                                    if (embed.TryGetProperty("fields", out var fArr) && fArr.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var field in fArr.EnumerateArray())
                                        {
                                            string fn = field.GetProperty("name").GetString() ?? "";
                                            string fv = field.GetProperty("value").GetString() ?? "";
                                            if (fn.Equals("HostId", StringComparison.OrdinalIgnoreCase)) meta.HostId = fv.Trim();
                                            else if (fn.Equals("RemoteAddress", StringComparison.OrdinalIgnoreCase)) meta.RemoteAddress = fv.Trim();
                                            else if (fn.Equals("AccessKey", StringComparison.OrdinalIgnoreCase)) meta.AccessKey = fv.Trim();
                                            else if (fn.Equals("ServerName", StringComparison.OrdinalIgnoreCase)) meta.ServerName = fv.Trim();
                                            else if (fn.Equals("Host", StringComparison.OrdinalIgnoreCase)) meta.HostUsername = fv.Trim();
                                            else if (fn.Equals("VisibilityMode", StringComparison.OrdinalIgnoreCase) && int.TryParse(fv, out var vm)) meta.VisibilityMode = vm;
                                            else if (fn.Equals("CustomRuleName", StringComparison.OrdinalIgnoreCase)) meta.CustomRuleName = fv.Trim();
                                            else if (fn.Equals("CustomBadgeLabel", StringComparison.OrdinalIgnoreCase)) meta.CustomBadgeLabel = fv.Trim();
                                            else if (fn.Equals("CustomColorHex", StringComparison.OrdinalIgnoreCase)) meta.CustomEmbedColorHex = fv.Trim();
                                            else if (fn.Equals("AllowedRoleIds", StringComparison.OrdinalIgnoreCase)) meta.AllowedRoleIds = fv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                                            else if (fn.Equals("AllowedUserIds", StringComparison.OrdinalIgnoreCase)) meta.AllowedUserIds = fv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                                            else if (fn.Equals("MinAppVersion", StringComparison.OrdinalIgnoreCase)) meta.MinAppVersion = fv.Trim();
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(meta.HostId) && !string.IsNullOrEmpty(meta.RemoteAddress))
                                    {
                                        playitMap[meta.HostId] = meta;
                                    }
                                    if (!string.IsNullOrEmpty(meta.ServerName) && !string.IsNullOrEmpty(meta.RemoteAddress))
                                    {
                                        serverNameMap[meta.ServerName] = meta;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/channels/{channelId}/messages?limit=100");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);
                var resp = await HttpClient.SendAsync(req);

                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    foreach (var msgElem in doc.RootElement.EnumerateArray())
                    {
                        if (msgElem.TryGetProperty("author", out var authorElem) && authorElem.TryGetProperty("bot", out var isBotElem) && isBotElem.GetBoolean())
                        {
                            if (msgElem.TryGetProperty("embeds", out var embedsArr) && embedsArr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var embed in embedsArr.EnumerateArray())
                                {
                                    string rawTitle = embed.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                                    string title = rawTitle.Replace("\u200B", "");
                                    if (!title.Contains("Servidor de Host", StringComparison.OrdinalIgnoreCase)) continue;

                                    string serverName = title.Replace("🖥️ Servidor de Host:", "").Replace("Servidor de Host:", "").Trim();
                                    string hostUser = "";
                                    string remoteAddr = "";
                                    string accessKey = "";
                                    string hostId = "";
                                    int visMode = 0;

                                    if (embed.TryGetProperty("color", out var colVal))
                                    {
                                        int col = colVal.GetInt32();
                                        if (col == 16766720) visMode = 2; // Gold color for Privadito
                                        else if (col == 5793266) visMode = 1; // Discord Blurple for Servidor
                                    }

                                    if (embed.TryGetProperty("fields", out var fieldsArr) && fieldsArr.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var field in fieldsArr.EnumerateArray())
                                        {
                                            string fName = field.GetProperty("name").GetString() ?? "";
                                            string fVal = field.GetProperty("value").GetString() ?? "";

                                            if (fName.Contains("ID del Host"))
                                            {
                                                hostId = fVal.Replace("`", "").Trim();
                                            }
                                            else if (fName.Contains("Creado") || fName.Contains("Hosteado"))
                                            {
                                                hostUser = fVal;
                                            }
                                            else if (fName.Contains("Dirección") || fName.Contains("Túnel"))
                                            {
                                                remoteAddr = fVal.Replace("`", "").Trim();
                                            }
                                            else if (fName.Contains("Dirigido") || fName.Contains("Alcance"))
                                            {
                                                if (fVal.Contains("Privado", StringComparison.OrdinalIgnoreCase) || fVal.Contains("Privadito", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    visMode = 2;
                                                }
                                                else if (fVal.Contains("Servidor", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    visMode = 1;
                                                }
                                                else if (fVal.Contains("Global", StringComparison.OrdinalIgnoreCase) || fVal.Contains("Abierto", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    visMode = 0;
                                                }
                                                else
                                                {
                                                    visMode = 3;
                                                }
                                            }
                                        }
                                    }

                                    PublishedTunnel? metaFromPlayit = null;
                                    if (!string.IsNullOrEmpty(hostId) && playitMap.TryGetValue(hostId, out var pData))
                                    {
                                        metaFromPlayit = pData;
                                    }
                                    else if (serverNameMap.TryGetValue(serverName, out var pNameData))
                                    {
                                        metaFromPlayit = pNameData;
                                    }

                                    var item = new PublishedTunnel
                                    {
                                        Id = msgElem.GetProperty("id").GetString() ?? Guid.NewGuid().ToString(),
                                        HostId = !string.IsNullOrEmpty(hostId) ? hostId : "HOST-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(),
                                        ServerName = serverName,
                                        HostUsername = hostUser,
                                        RemoteAddress = metaFromPlayit != null ? metaFromPlayit.RemoteAddress : remoteAddr,
                                        VisibilityMode = metaFromPlayit != null ? metaFromPlayit.VisibilityMode : visMode,
                                        AccessKey = metaFromPlayit != null ? metaFromPlayit.AccessKey : accessKey,
                                        MinAppVersion = metaFromPlayit != null ? metaFromPlayit.MinAppVersion : "1.3.1",
                                        CustomRuleName = metaFromPlayit?.CustomRuleName,
                                        CustomBadgeLabel = metaFromPlayit?.CustomBadgeLabel,
                                        CustomEmbedColorHex = metaFromPlayit?.CustomEmbedColorHex,
                                        AllowedRoleIds = metaFromPlayit?.AllowedRoleIds ?? new List<string>(),
                                        AllowedUserIds = metaFromPlayit?.AllowedUserIds ?? new List<string>(),
                                        DiscordMessageId = msgElem.GetProperty("id").GetString()
                                    };

                                    list.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return list;
        }
    }
}
