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
                    if (text2.Contains("BlackHouseNameSyncScript"))
                    {
                        return (success: true, message: "✓ El script 'BlackHouseNameSyncScript' ya está inyectado en este mapa.");
                    }
                    int num = text2.IndexOf("class=\"ServerScriptService\"");
                    if (num > 0)
                    {
                        int startIndex = text2.IndexOf(">", num) + 1;
                        string value = "\n\t<Item class=\"Script\" referent=\"RBXBlackHouseSyncScript\">\n\t\t<Properties>\n\t\t\t<string name=\"Name\">BlackHouseNameSyncScript</string>\n\t\t\t<ProtectedString name=\"Source\"><![CDATA[" + luauSource + "]]></ProtectedString>\n\t\t</Properties>\n\t</Item>";
                        string contents2 = text2.Insert(startIndex, value);
                        File.WriteAllText(targetMapPath, contents2);
                        return (success: true, message: "✓ ¡Script inyectado exitosamente en ServerScriptService del mapa!");
                    }
                }
                if (RbxmBridgeServer.QueueRbxm(text).ok)
                {
                    return (success: true, message: "✓ Script inyectado y preparado para importación automática al abrir Roblox Studio!");
                }
                return (success: true, message: "✓ Script inyectado generado en: " + text);
            }
            catch (Exception ex)
            {
                return (success: false, message: "Error al inyectar script: " + ex.Message);
            }
        }
    }
}
