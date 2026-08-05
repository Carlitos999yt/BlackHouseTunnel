# 🚀 NepTunnel C# (.NET 8 WPF / Cross-Platform)

> High Performance Roblox Studio Local Test Session Tunneling Tool with Roblox Studio Manager (RSM) Integration, RBXM Importer, Echo Connectivity Testing & Trilingual Localization.

---

## 🌟 Overview

**NepTunnel** allows Roblox Studio developers and playtesters to host and join **Local Test Sessions** across remote network tunnels (e.g. Playit.gg, ngrok, Cloudflare, local proxies) without requiring port forwarding.

Re-architected from Python to C# (.NET 8 WPF), NepTunnel delivers ultra-low latency, zero-allocation UDP proxying, automatic multi-installation detection, and seamless Roblox Studio version `0.729.0.7290838` deployment.

---

## ✨ Features

- ⚡ **Zero-Allocation UDP Proxying**: High-speed packet forwarding with router NAT anti-exhaustion safety.
- 🎯 **Roblox Studio Auto-Detection**: Detects and lets you pick between **RSM (Roblox Studio Mod Manager)**, **Bloxstrap Studio**, and **Roblox Studio Oficial**.
- 🛠️ **RSM Bootstrapper & Repair Engine**: Direct GitHub repair engine for corrupted or missing Studio files.
- 🗺️ **RBXM Importer Bridge**: Built-in HTTP bridge server to send `.rbxm` models directly into active Studio sessions.
- 🔊 **Echo Test Suite**: Interactive packet probe test to verify tunnel connectivity before launching playtests.
- 🌐 **Trilingual Localization**: Instant runtime switching between **Español**, **English**, and **Português**.
- 📖 **Interactive 9-Step Tutorial**: Embedded visual guide with click-to-zoom image viewer.

---

### Architecture & Reusable Modules

The codebase is fully decoupled into independent, reusable modules for open-source maintenance and easy reuse in future projects:

```
NepTunnel/
├── 📁 Views/                 <-- UI Presentation Components
│   ├── MainMenuView.cs       # Main Dashboard & Studio Status UI
│   ├── HostViews.cs          # Server Hosting Setup & Controls
│   ├── JoinViews.cs          # Client Join Setup & Controls
│   └── ToolViews.cs          # Tutorial Carousel & Modals
│
├── 📁 Services/              <-- Core Business Logic & Protocol Engine
│   ├── RobloxStudioService.cs# Studio Detection, CLI Execution (-task StartServer/StartClient)
│   ├── UdpProxy.cs           # Low-Latency High-Performance UDP Proxy Engine
│   ├── RbxmBridgeServer.cs   # HTTP Bridge Server (port 7878) for Studio Plugin IPC
│   ├── ConfigManager.cs      # System AppData Storage & Local Config Migration
│   ├── PluginInstaller.cs    # Roblox Studio Plugin Auto-Installer
│   ├── ScriptInjector.cs    # Automatic Script Injection Service
│   ├── LocalizationService.cs# Trilingual Dictionary (ES, EN, PT)
│   ├── EchoService.cs        # UDP Echo Test Server & Client
│   └── IconFactory.cs        # Vector SVG Icon Builder
│
├── App.xaml / App.xaml.cs    # WPF Application Entry Point & Resource Dictionary
└── MainWindow.xaml.cs       # Lightweight Event Router & View Navigator
```

---

## 👥 Authors & Core Contributors

- **Carlitos999yt**: Lead Project Creator & Architect ([GitHub Profile](https://github.com/Carlitos999yt))
- **Antigravity AI (Gemini 3.6 Flash / Google DeepMind)**: AI Pair Programmer & Lead System Co-Architect

---

## 💖 Special Thanks & Credits

- **⭐ Beta Tester Extraordinaire**: **`leshe♡`** *(Special thanks for invaluable testing, feedback, and bug reports!)*
- **Roblox Studio Mod Manager Engine**: [MaximumADHD](https://github.com/MaximumADHD/Roblox-Studio-Mod-Manager) (`StudioBootstrapper`)

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)

### Build Steps

```bash
# Clone repository
git clone https://github.com/Carlitos999yt/NepTunnel.git
cd NepTunnel/NepTunnel

# Build project
dotnet build NepTunnel.csproj

# Publish single-file executable
dotnet publish NepTunnel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
dotnet publish NepTunnel.csproj -c Release -r win-x64 -o "SingleFile_EXE_Output"
```

---

## 📄 License
Licensed under the [MIT License](LICENSE).
