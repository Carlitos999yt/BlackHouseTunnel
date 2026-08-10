using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackHouseTunnel.Services
{
    public static class LocalizationService
    {
        public static string CurrentLanguage { get; set; } = "es"; // "es", "en", "pt"

        public static string DetectDefaultSystemLanguage()
        {
            try
            {
                string sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                return sysLang switch
                {
                    "es" => "es",
                    "pt" => "pt",
                    _ => "en"
                };
            }
            catch
            {
                return "es";
            }
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["es"] = new()
            {
                ["app_title"] = "BlackHouse Tunnel",
                ["nav_home"] = "Inicio",
                ["nav_host"] = "Crear Host del Servidor",
                ["nav_join"] = "Unirse a Host",
                ["nav_rbxm"] = "Importador Mapas .rbxm",
                ["nav_rsm"] = "Asistente RSM Mod Manager",
                ["nav_echo"] = "Prueba Latencia & Eco UDP",
                ["nav_settings"] = "Ajustes",

                // Home
                ["home_welcome"] = "¡Hola de nuevo, {0}! ⚡",
                ["home_sub"] = "Plataforma de Túneles Seguros para Roblox Studio",
                ["home_quick_host"] = "Servidor Túnel Rápido",
                ["home_quick_join"] = "Unirse a Partida Túnel",
                ["home_online_members"] = "Miembros en Línea",
                ["home_active_tunnels"] = "Túneles de Host Activos",
                ["home_no_tunnels"] = "ℹ️ No hay túneles de host activos en este momento. ¡Crea uno desde la pestaña Host para comenzar!",
                ["btn_reload_tunnels"] = "🔄 Recargar Lista de Túneles Activos",

                // Host View
                ["host_title"] = "🖥️ Configuración Completa de Host de Servidor",
                ["host_sub"] = "Configuración completa de servidor túnel.",
                ["lbl_uid"] = "ID de Usuario de Roblox (User ID / UID)",
                ["lbl_username"] = "Apodo en el Servidor (Username)",
                ["lbl_server_name"] = "Nombre del Servidor Túnel",
                ["lbl_port"] = "Puerto Local UDP",
                ["lbl_addr"] = "Dirección del Túnel Remoto (Host Address)",
                ["lbl_vis"] = "🔒 Visibilidad & Control de Acceso",
                ["lbl_key"] = "🔑 Configuración de Llave de Acceso Secreta (Key)",
                ["lbl_key_hint"] = "Ingresa una contraseña aquí si deseas que los usuarios deban escribir una clave secreta para conectarse a tu túnel.",
                ["lbl_map"] = "Archivo de Mapa Roblox (.rbxl / .rbxlx) [Opcional]",
                ["btn_start_host"] = "🚀 Iniciar Servidor Host",
                ["btn_import_scripts"] = "📄 Importar Scripts",

                ["vis_option_0"] = "🌐 Global (Sin restricciones - Abierto a todos)",
                ["vis_option_1"] = "🛡️ Servidor (Solo miembros del Servidor de Discord)",
                ["vis_option_2"] = "🔒 Exclusivo con Rol (Solo miembros con Rol de Discord)",

                // Join View
                ["join_title"] = "🔗 Conectarse a un Servidor Túnel",
                ["join_sub"] = "Ingresa la dirección o selecciona un host activo.",
                ["lbl_manual_addr"] = "Dirección del Túnel (Host:Puerto)",
                ["btn_connect"] = "⚡ Conectarse & Lanzar Studio",

                // Echo Test View
                ["echo_title"] = "📡 Diagnóstico y Prueba de Latencia UDP",
                ["echo_sub"] = "Verifica el tiempo de respuesta y paquete en vivo.",
                ["btn_run_echo"] = "⚡ Iniciar Prueba de Latencia",

                // Settings
                ["settings_title"] = "⚙️ Configuraciones del Sistema",
                ["settings_lang_theme"] = "🌐 Idioma y Apariencia",
                ["settings_lang_lbl"] = "Idioma de la Aplicación",
                ["settings_theme_lbl"] = "Tema de la Interfaz",
                ["settings_theme_dark"] = "🌙 Oscuro Noche Profunda (Tema por Defecto)",
                ["settings_theme_light"] = "☀️ Claro Moderno (Light Mode)",
                ["settings_discord_sec"] = "🎮 Discord y Presencia en Vivo",
                ["settings_rpc_toggle"] = "Activar Discord Rich Presence (Muestra tu estado en Discord)",
                ["settings_maint_sec"] = "🛠️ Mantenimiento y Roblox Studio",
                ["settings_studio_path"] = "Ejecutable Activo de Roblox Studio",
                ["settings_updates_sec"] = "🔄 Actualizaciones del Sistema",
                ["btn_scan_studio"] = "🎯 Seleccionar Versión...",
                ["btn_browse_studio"] = "📁 Buscar Exe...",
                ["btn_reinstall_studio"] = "🔄 Reinstalar / Actualizar Roblox Studio",
                ["btn_check_updates"] = "🔍 Buscar Actualizaciones Manuales",

                // Profile
                ["profile_edit_nick"] = "Cambiar Mi Apodo",
                ["profile_logout"] = "Cerrar Sesión",
                ["modal_nick_title"] = "👤 Cambiar Mi Apodo en la App",
                ["modal_nick_sub"] = "Ingresa tu nuevo apodo (Se actualizará en la app y en el Servidor de Discord)",
                ["btn_save"] = "💾 Guardar Apodo",
                ["btn_cancel"] = "Cancelar"
            },
            ["en"] = new()
            {
                ["app_title"] = "BlackHouse Tunnel",
                ["nav_home"] = "Home",
                ["nav_host"] = "Create Host Server",
                ["nav_join"] = "Join Host",
                ["nav_rbxm"] = ".rbxm Map Importer",
                ["nav_rsm"] = "RSM Mod Assistant",
                ["nav_echo"] = "Latency & UDP Echo Test",
                ["nav_settings"] = "Settings",

                // Home
                ["home_welcome"] = "Welcome back, {0}! ⚡",
                ["home_sub"] = "Secure Tunnel Platform for Roblox Studio",
                ["home_quick_host"] = "Quick Host Server",
                ["home_quick_join"] = "Join Tunnel Session",
                ["home_online_members"] = "Online Members",
                ["home_active_tunnels"] = "Live Active Tunnels",
                ["home_no_tunnels"] = "ℹ️ No active host tunnels at this moment. Create one from the Host tab to start!",
                ["btn_reload_tunnels"] = "🔄 Refresh Active Tunnels List",

                // Host View
                ["host_title"] = "🖥️ Complete Server Host Configuration",
                ["host_sub"] = "Complete tunnel server configuration.",
                ["lbl_uid"] = "Roblox User ID (User ID / UID)",
                ["lbl_username"] = "Server Nickname (Username)",
                ["lbl_server_name"] = "Tunnel Server Name",
                ["lbl_port"] = "Local UDP Port",
                ["lbl_addr"] = "Remote Tunnel Address (Host Address)",
                ["lbl_vis"] = "🔒 Visibility & Access Control",
                ["lbl_key"] = "🔑 Secret Access Key Configuration (Key)",
                ["lbl_key_hint"] = "Enter a password here if you want users to enter a secret key to connect to your tunnel.",
                ["lbl_map"] = "Roblox Map File (.rbxl / .rbxlx) [Optional]",
                ["btn_start_host"] = "🚀 Start Host Server",
                ["btn_import_scripts"] = "📄 Import Scripts",

                ["vis_option_0"] = "🌐 Global (Open to everyone)",
                ["vis_option_1"] = "🛡️ Server (Discord Server Members only)",
                ["vis_option_2"] = "🔒 Role Exclusive (Members with Discord Role)",

                // Join View
                ["join_title"] = "🔗 Connect to a Tunnel Server",
                ["join_sub"] = "Enter address or select an active host.",
                ["lbl_manual_addr"] = "Tunnel Address (Host:Port)",
                ["btn_connect"] = "⚡ Connect & Launch Studio",

                // Echo Test View
                ["echo_title"] = "📡 UDP Echo & Latency Test",
                ["echo_sub"] = "Check response time and live packet transmission.",
                ["btn_run_echo"] = "⚡ Start Latency Test",

                // Settings
                ["settings_title"] = "⚙️ System Settings",
                ["settings_lang_theme"] = "🌐 Language & Appearance",
                ["settings_lang_lbl"] = "Application Language",
                ["settings_theme_lbl"] = "UI Visual Theme",
                ["settings_theme_dark"] = "🌙 Dark Deep Night (Default Theme)",
                ["settings_theme_light"] = "☀️ Light Modern (Light Mode)",
                ["settings_discord_sec"] = "🎮 Discord & Live Presence",
                ["settings_rpc_toggle"] = "Enable Discord Rich Presence (Show status on Discord)",
                ["settings_maint_sec"] = "🛠️ Maintenance & Roblox Studio",
                ["settings_studio_path"] = "Active Roblox Studio Executable",
                ["settings_updates_sec"] = "🔄 System Updates",
                ["btn_scan_studio"] = "🎯 Select Version...",
                ["btn_browse_studio"] = "📁 Browse Exe...",
                ["btn_reinstall_studio"] = "🔄 Reinstall / Update Roblox Studio",
                ["btn_check_updates"] = "🔍 Check for Updates Manually",

                // Profile
                ["profile_edit_nick"] = "Change My Nickname",
                ["profile_logout"] = "Log Out",
                ["modal_nick_title"] = "👤 Change My Nickname",
                ["modal_nick_sub"] = "Enter your new nickname (Will update in app and Discord Server)",
                ["btn_save"] = "💾 Save Nickname",
                ["btn_cancel"] = "Cancel"
            },
            ["pt"] = new()
            {
                ["app_title"] = "BlackHouse Tunnel",
                ["nav_home"] = "Início",
                ["nav_host"] = "Criar Host do Servidor",
                ["nav_join"] = "Entrar no Host",
                ["nav_rbxm"] = "Importador .rbxm",
                ["nav_rsm"] = "Assistente RSM Mod",
                ["nav_echo"] = "Teste Eco UDP & Latência",
                ["nav_settings"] = "Configurações",

                // Home
                ["home_welcome"] = "Bem-vindo de volta, {0}! ⚡",
                ["home_sub"] = "Plataforma de Túneis Seguros para Roblox Studio",
                ["home_quick_host"] = "Servidor Host Rápido",
                ["home_quick_join"] = "Entrar na Partida",
                ["home_online_members"] = "Membros Online",
                ["home_active_tunnels"] = "Túneis Ativos ao Vivo",
                ["home_no_tunnels"] = "ℹ️ Nenhum túnel de host ativo no momento. Crie um na aba Host para começar!",
                ["btn_reload_tunnels"] = "🔄 Recarregar Lista de Túneis Ativos",

                // Host View
                ["host_title"] = "🖥️ Configuração Completa do Servidor Host",
                ["host_sub"] = "Configuração completa de servidor túnel.",
                ["lbl_uid"] = "ID de Usuário do Roblox (UID)",
                ["lbl_username"] = "Apelido no Servidor (Username)",
                ["lbl_server_name"] = "Nome do Servidor Túnel",
                ["lbl_port"] = "Porta Local UDP",
                ["lbl_addr"] = "Endereço Remoto do Túnel",
                ["lbl_vis"] = "🔒 Visibilidade e Controle de Acesso",
                ["lbl_key"] = "🔑 Configuração de Chave de Acesso Secreta (Key)",
                ["lbl_key_hint"] = "Insira uma senha aqui se desejar proteger seu túnel com chave.",
                ["lbl_map"] = "Arquivo de Mapa Roblox (.rbxl / .rbxlx) [Opcional]",
                ["btn_start_host"] = "🚀 Iniciar Servidor Host",
                ["btn_import_scripts"] = "📄 Importar Scripts",

                ["vis_option_0"] = "🌐 Global (Aberto a todos)",
                ["vis_option_1"] = "🛡️ Servidor (Apenas membros do Servidor)",
                ["vis_option_2"] = "🔒 Exclusivo com Função (Membros com Função do Discord)",

                // Join View
                ["join_title"] = "🔗 Conectar ao Servidor Túnel",
                ["join_sub"] = "Insira o endereço ou selecione um host ativo.",
                ["lbl_manual_addr"] = "Endereço do Túnel (Host:Porta)",
                ["btn_connect"] = "⚡ Conectar e Iniciar Studio",

                // Echo Test View
                ["echo_title"] = "📡 Diagnóstico e Teste de Eco UDP",
                ["echo_sub"] = "Verifique o tempo de resposta e pacote ao vivo.",
                ["btn_run_echo"] = "⚡ Iniciar Teste de Latência",

                // Settings
                ["settings_title"] = "⚙️ Configurações do Sistema",
                ["settings_lang_theme"] = "🌐 Idioma e Aparência",
                ["settings_lang_lbl"] = "Idioma do Aplicativo",
                ["settings_theme_lbl"] = "Tema Visual da Interface",
                ["settings_theme_dark"] = "🌙 Escuro Noite Profunda (Padrão)",
                ["settings_theme_light"] = "☀️ Claro Moderno (Light Mode)",
                ["settings_discord_sec"] = "🎮 Discord e Presença ao Vivo",
                ["settings_rpc_toggle"] = "Ativar Discord Rich Presence (Mostrar status no Discord)",
                ["settings_maint_sec"] = "🛠️ Manutenção e Roblox Studio",
                ["settings_studio_path"] = "Executável Ativo do Roblox Studio",
                ["settings_updates_sec"] = "🔄 Atualizações do Sistema",
                ["btn_scan_studio"] = "🎯 Selecionar Versão...",
                ["btn_browse_studio"] = "📁 Buscar Exe...",
                ["btn_reinstall_studio"] = "🔄 Reinstalar / Atualizar Roblox Studio",
                ["btn_check_updates"] = "🔍 Buscar Actualizações Manualmente",

                // Profile
                ["profile_edit_nick"] = "Alterar Meu Apelido",
                ["profile_logout"] = "Sair da Conta",
                ["modal_nick_title"] = "👤 Alterar Meu Apelido",
                ["modal_nick_sub"] = "Digite seu novo apelido (Atualizará no app e no Servidor do Discord)",
                ["btn_save"] = "💾 Salvar Apelido",
                ["btn_cancel"] = "Cancelar"
            }
        };

        public static string Get(string key, params object[] args)
        {
            string lang = string.IsNullOrWhiteSpace(CurrentLanguage) ? "es" : CurrentLanguage.ToLowerInvariant();
            if (!Translations.ContainsKey(lang)) lang = "es";

            if (Translations[lang].TryGetValue(key, out string? template))
            {
                return args.Length > 0 ? string.Format(template, args) : template;
            }

            if (Translations["es"].TryGetValue(key, out string? fallbackTemplate))
            {
                return args.Length > 0 ? string.Format(fallbackTemplate, args) : fallbackTemplate;
            }

            return key;
        }
    }
}
