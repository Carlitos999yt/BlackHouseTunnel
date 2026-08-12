using System;
using System.IO;
using System.Security;

namespace BlackHouseTunnel.Services
{
    public static class ScriptInjector
    {
        public static string GenerateRbxmxScript(string scriptName, string luauCode)
        {
            return $"<roblox xmlns:xmime=\"http://www.w3.org/2005/05/xmlmime\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://www.roblox.com/roblox.xsd\" version=\"4\">\n\t<Meta name=\"ExplicitAutoJoints\">true</Meta>\n\t<Item class=\"Script\" referent=\"RBXBlackHouseScript001\">\n\t\t<Properties>\n\t\t\t<BinaryString name=\"AttributesSerialize\"></BinaryString>\n\t\t\t<bool name=\"Disabled\">false</bool>\n\t\t\t<Content name=\"LinkedSource\"><null></null></Content>\n\t\t\t<string name=\"Name\">{SecurityElement.Escape(scriptName)}</string>\n\t\t\t<string name=\"ScriptGuid\">{{{Guid.NewGuid().ToString().ToUpper()}}}</string>\n\t\t\t<ProtectedString name=\"Source\"><![CDATA[{luauCode}]]></ProtectedString>\n\t\t\t<int64 name=\"SourceAssetId\">-1</int64>\n\t\t\t<BinaryString name=\"Tags\"></BinaryString>\n\t\t</Properties>\n\t</Item>\n</roblox>";
        }

        public static string GetSecurityLuauScript(string hostUsername)
        {
            string cleanHost = string.IsNullOrWhiteSpace(hostUsername) ? "Player" : hostUsername.Trim();
            return $@"-- BlackHouseTunnel Security & Player Verification Script
local HttpService = game:GetService(""HttpService"")
local Players = game:GetService(""Players"")

local hostUser = ""{SecurityElement.Escape(cleanHost)}""

print(""[BlackHouseSecurity] Security Enforcement active for Host: "" .. hostUser)

Players.PlayerAdded:Connect(function(player)
    task.wait(0.2)
    local pName = player.Name

    -- Ghost Player Cleanup: Destroy leftover ghost player with same Name or UserId
    for _, oldP in ipairs(Players:GetPlayers()) do
        if oldP ~= player and (oldP.Name:lower() == pName:lower() or (oldP.UserId == player.UserId and player.UserId > 0)) then
            print(""[BlackHouseSecurity] Ghost player detected! Destroying leftover instance for: "" .. oldP.Name)
            pcall(function() oldP:Destroy() end)
        end
    end

    if pName:lower() == hostUser:lower() or pName:lower():sub(1,6) == ""player"" then
        print(""[BlackHouseSecurity] Local host / player authorized: "" .. pName)
        return
    end

    local url = ""http://127.0.0.1:7878/verify_player?name="" .. HttpService:UrlEncode(pName)
    local ok, resJson = pcall(function()
        return HttpService:GetAsync(url)
    end)

    local isAuth = false
    if ok and resJson then
        local parseOk, data = pcall(function()
            return HttpService:JSONDecode(resJson)
        end)
        if parseOk and data and data.authorized then
            isAuth = true
        end
    end

    if not isAuth then
        print(""[BlackHouseSecurity] Unauthorized client detected: "" .. pName .. "" - KICKING!"")
        player:Kick(""\n\n⛔ ACCESO DENEGADO\n\nDebes ingresar utilizando la aplicación oficial BlackHouseTunnel con tu cuenta de Discord autenticada.\n"")
    else
        print(""[BlackHouseSecurity] Client verified successfully: "" .. pName)
    end
end)
";
        }

        public static (bool success, string message) InjectScriptIntoMap(string targetMapPath, string luauSource)
        {
            try
            {
                string contents = GenerateRbxmxScript("BlackHouseNameSyncScript", luauSource);
                string text = Path.Combine(Logger.AppDataDir, "BlackHouseNameSyncScript.rbxmx");
                Directory.CreateDirectory(Logger.AppDataDir);
                File.WriteAllText(text, contents);
                if (!string.IsNullOrWhiteSpace(targetMapPath) && File.Exists(targetMapPath) && Path.GetExtension(targetMapPath).ToLowerInvariant() == ".rbxlx")
                {
                    string text2 = File.ReadAllText(targetMapPath);
                    int num = text2.IndexOf("class=\"ServerScriptService\"");
                    if (num > 0)
                    {
                        int startIndex = text2.IndexOf(">", num) + 1;
                        string value = "\n\t<Item class=\"Script\" referent=\"RBXBlackHouseSyncScript\">\n\t\t<Properties>\n\t\t\t<string name=\"Name\">BlackHouseNameSyncScript</string>\n\t\t\t<ProtectedString name=\"Source\"><![CDATA[" + luauSource + "]]></ProtectedString>\n\t\t</Properties>\n\t</Item>";
                        string contents2 = text2.Insert(startIndex, value);
                        File.WriteAllText(targetMapPath, contents2);
                        return (success: true, message: "✓ ¡Script de Seguridad inyectado exitosamente en ServerScriptService del mapa!");
                    }
                }
                if (RbxmBridgeServer.QueueRbxm(text).ok)
                {
                    return (success: true, message: "✓ Script de Seguridad preparado para importación automática al abrir Roblox Studio!");
                }
                return (success: true, message: "✓ Script de Seguridad generado en: " + text);
            }
            catch (Exception ex)
            {
                return (success: false, message: "Error al inyectar script: " + ex.Message);
            }
        }
    }
}
