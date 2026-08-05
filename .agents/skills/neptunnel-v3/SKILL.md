---
name: neptunnel-v3
description: Guía técnica completa, especificaciones de arquitectura, protocolos de red, banderas CLI de Roblox Studio y workflow de desarrollo para NepTunnel v3.
---

# 🚀 NepTunnel v3: Architecture & Protocol Specification Guide

Esta guía contiene la especificación técnica completa de **NepTunnel** para guiar el desarrollo de la nueva versión (v3) en cualquier nueva sesión de chat o entorno de trabajo.

---

## 🏛️ 1. Estructura de Arquitectura Modular

El sistema está dividido estrictamente en dos capas desacopladas:

```
NepTunnel/
├── 📁 Views/                 <-- Capa de Presentación (UI)
│   ├── MainMenuView.cs       # Panel Principal, estado del Studio y atajos
│   ├── HostViews.cs          # Vista de Configuración de Host (UID, Port, HostAddr, Map)
│   ├── JoinViews.cs          # Vista de Configuración de Cliente (Username, JoinAddr)
│   └── ToolViews.cs          # Visor de Tutoriales e Imágenes Modales
│
├── 📁 Services/              <-- Capa de Lógica de Negocio y Motores de Red
│   ├── RobloxStudioService.cs# Auto-detección de Studio y ejecución CLI (-task StartServer/StartClient)
│   ├── UdpProxy.cs           # Motor de Proxy UDP de latencia ultra baja (puerto local 55555)
│   ├── RbxmBridgeServer.cs   # Servidor Puente HTTP local (puerto 7878)
│   ├── ConfigManager.cs      # Almacenamiento seguro en AppData (%LocalAppData%\NepTunnel\nep_config.json)
│   ├── PluginInstaller.cs    # Auto-instalación de plugins en Studio
│   ├── ScriptInjector.cs    # Inyección de scripts de servidor/cliente
│   ├── LocalizationService.cs# Motor de Idiomas Trilingüe (ES, EN, PT)
│   └── EchoService.cs        # Servidor y Cliente de Pruebas de Paquetes UDP (Echo)
│
├── App.xaml / App.xaml.cs    # Punto de Entrada WPF y Recursos de Estilo
└── MainWindow.xaml.cs       # Enrutador de Eventos Liviano y Navegación de Vistas
```

---

## 📡 2. Especificación de Protocolos y Red

### A. Motor de Proxy UDP (`UdpProxy.cs`)
- **Puerto Local del Proxy**: `55555`
- **Función**: Intercepta los paquetes UDP de Roblox Studio enviados a `127.0.0.1:55555` y los reenvía a la dirección del túnel remoto (Playit.gg, ngrok, Cloudflare).
- **NAT Keep-Alive**: Incluye mecanismo de prevención de agotamiento de puertos en enrutadores domésticos.

### B. Servidor Puente HTTP Local (`RbxmBridgeServer.cs`)
- **Puerto de Escucha**: `http://127.0.0.1:7878/`
- **Endpoints Clave**:
  - `GET /identity`: Devuelve JSON con el rol (`host`/`client`), `name` (Username), `uid` (UserId) y estado de importación.
  - `GET /poll`: Consulta si hay modelos `.rbxm` pendientes de descargar.
  - `GET /download`: Permite a Roblox Studio descargar el archivo `.rbxm` listo en la carpeta staging.
  - `POST /queue`: Encola modelos `.rbxm` desde la interfaz de NepTunnel hacia Studio.

---

## 💻 3. Banderas CLI para Lanzamiento de Roblox Studio (`RobloxStudioService.cs`)

### Modo Servidor (Host):
```bash
"RobloxStudioBeta.exe" -task StartServer -placeId 0 -universeId 0 -placeVersion 1 -port <PUERTO_LOCAL> -creatorId <UID> -creatorType 1 -numTestServerPlayersUponStartup 1 -userid <UID> -parentSessionGuid <GUID1> -playTestSessionGuid <GUID2> -instanceId StudioServer
```

### Modo Cliente (Join):
```bash
"RobloxStudioBeta.exe" -task StartClient -placeId 0 -universeId 0 -placeVersion 1 -server 127.0.0.1 -port 55555 -parentSessionGuid <GUID1> -playTestSessionGuid <GUID2> -instanceId StudioClient
```

---

## 🔐 4. Almacenamiento Seguro y Migración (`ConfigManager.cs`)

- **Ruta de Configuración Principal**:
  `%LocalAppData%\NepTunnel\nep_config.json` (`C:\Users\<Usuario>\AppData\Local\NepTunnel\nep_config.json`)
- **Separación de Direcciones**:
  - `HostAddr`: Almacena la dirección Playit del Host (ej: `manzana.gl.at.ply.gg:20573`).
  - `JoinAddr`: Almacena la dirección del servidor de un amigo a conectarse (ej: `pera.gl.at.ply.gg:12345`).
  - **REGLA CRÍTICA**: `JoinAddr` y `HostAddr` son **100% independientes**. Conectarse al servidor de un amigo NUNCA debe sobreescribir `HostAddr`.
- **Auto-Migración y Limpieza**:
  Si el sistema detecta un `nep_config.json` heredado en la carpeta de trabajo, migra automáticamente sus datos al `%LocalAppData%` del sistema y elimina el archivo local para mantener el espacio de trabajo limpio.

---

## ⚡ 5. Estrategia de Auto-Inyección sin Plugins Manuales

Para evitar que los usuarios tengan que emparejar o instalar un plugin manualmente:
1. Al lanzar Studio via CLI, NepTunnel inyecta un `Script` de sesión de pocos segundos.
2. Al iniciar la partida, el script realiza un `HttpService:GetAsync("http://127.0.0.1:7878/identity")` pasados 2 segundos.
3. NepTunnel devuelve la identidad del jugador y sincroniza nombres sin requerir interacción manual.

---

## 🛠️ 6. Comandos de Compilación y Publicación (.NET 8)

```bash
# Compilar proyecto en modo Release
dotnet build NepTunnel.csproj -c Release

# Publicar ejecutable único independiente sin dependencias externas
dotnet publish NepTunnel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false
```
