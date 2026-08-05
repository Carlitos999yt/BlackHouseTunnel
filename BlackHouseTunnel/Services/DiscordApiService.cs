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

        public async Task<DiscordUser?> GetUserProfileAndGuildMemberAsync(string accessToken, string guildId, string botToken = "")
        {
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

                // 2. Check Member of Guild (/users/@me/guilds/{guildId}/member)
                var memberReq = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v10/users/@me/guilds/{guildId}/member");
                memberReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var memberResp = await HttpClient.SendAsync(memberReq);
                if (memberResp.IsSuccessStatusCode)
                {
                    user.IsMemberOfGuild = true;
                    string memberJson = await memberResp.Content.ReadAsStringAsync();
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
                else
                {
                    user.IsMemberOfGuild = false;
                }

                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordApiService Error]: {ex.Message}");
                return null;
            }
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

            user.IsPrivadito = namesLower.Any(r => r.Contains("privadito"));
            user.IsHoster = namesLower.Any(r => r.Contains("hoster") || r.Contains("host"));
            user.IsStaffOrAdmin = namesLower.Any(r => r.Contains("staff") || r.Contains("superior") || r.Contains("admin") || r.Contains("mod"));

            if (namesLower.Any(r => r.Contains("superior")))
            {
                user.PrimaryRole = "Superior";
                user.PrimaryRoleColor = "#FFD700";
            }
            else if (namesLower.Any(r => r.Contains("staff") || r.Contains("admin")))
            {
                user.PrimaryRole = "Staff";
                user.PrimaryRoleColor = "#E74C3C";
            }
            else if (namesLower.Any(r => r.Contains("hoster")))
            {
                user.PrimaryRole = "Hoster";
                user.PrimaryRoleColor = "#9B59B6";
            }
            else if (namesLower.Any(r => r.Contains("follador")))
            {
                user.PrimaryRole = "Follador";
                user.PrimaryRoleColor = "#E67E22";
            }
            else if (namesLower.Any(r => r.Contains("chica")))
            {
                user.PrimaryRole = "Chica";
                user.PrimaryRoleColor = "#FF69B4";
            }
            else if (namesLower.Any(r => r.Contains("usuario") || r.Contains("member")))
            {
                user.PrimaryRole = "Usuario";
                user.PrimaryRoleColor = "#5865F2";
            }
            else
            {
                string firstRole = user.RoleNames.FirstOrDefault() ?? "Usuario";
                user.PrimaryRole = firstRole;
                user.PrimaryRoleColor = "#5865F2";
            }
        }
    }
}
