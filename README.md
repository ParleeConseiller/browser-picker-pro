# Browser Picker Pro

> **Choose the right browser every time.**

A modern, beautiful Windows application that intercepts web links and lets you choose which browser to open them in — with rules, profiles, and optional security checks.

**Created by Parlee Conseiller, Inc.**  
**Main developer: Jose-Jorge HERNANDEZ**

---

## Screenshots

| Browser Picker | Dashboard |
|---|---|
| *Floating picker window with animated browser cards* | *Full configuration dashboard with Mica effect* |

---

## Features

- **Beautiful Browser Picker** — Floating Fluent Design window with animated cards, Mica/Acrylic effect
- **Smart Rules Engine** — Route URLs by domain, wildcard, or regex to specific browsers
- **Multi-Profile Support** — Chrome profiles, Firefox profiles, Edge profiles
- **Browser Detection** — Automatically finds Chrome, Edge, Firefox, Brave, Vivaldi, Opera, Opera GX, and more
- **Security Checks** — Optional VirusTotal and Google Safe Browsing integration
- **Import/Export** — Full configuration backup and restore
- **Windows Integration** — Register as default HTTP/HTTPS/HTML handler (no admin required)
- **Light & Dark Mode** — System-aware with manual override
- **Keyboard Navigation** — Full keyboard support, Escape to cancel

---

## Quick Start

### Download

Download the latest release from [GitHub Releases](https://github.com/parleeconseiller/browser-picker-pro/releases).

### Build from source

```powershell
git clone https://github.com/parleeconseiller/browser-picker-pro.git
cd browser-picker-pro
.\scripts\build.ps1 -Test -Publish
```

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

### Register as Default Browser

1. Run `BrowserPickerPro.exe` (no arguments) to open the dashboard
2. Navigate to **Windows Setup**
3. Click **Register as Browser**
4. Follow the instructions to set it in Windows Default Apps

---

## Usage

### Browser Selection Mode

When any application opens a web link, Browser Picker Pro appears with a card for each installed browser. Click a card to open the URL in that browser.

- **Single click** — Select and open
- **Enter key** — Open in selected browser
- **Escape** — Cancel and use system default

### Configuration Dashboard

Run without arguments to open the full dashboard:

```
BrowserPickerPro.exe
```

---

## Rules Engine

Create rules to automatically route URLs without showing the picker:

| Pattern | Type | Description |
|---|---|---|
| `youtube.com` | Domain | Matches youtube.com and all subdomains |
| `*.google.com*` | Wildcard | Matches any google.com subdomain with any path |
| `https?://.*\.bank\.com` | Regex | Complex pattern matching |
| `https://github.com` | Exact | Exact URL match only |

---

## Security

The optional security module checks URLs before displaying the picker:

- **VirusTotal** — 70+ antivirus engine scan
- **Google Safe Browsing** — Google's threat database

Both require free API keys. See [API_INTEGRATIONS.md](docs/API_INTEGRATIONS.md).

---

## Technology Stack

| Component | Technology |
|---|---|
| UI | WPF + .NET 9 |
| Architecture | MVVM, Clean Architecture, SOLID |
| DI | Microsoft.Extensions.DependencyInjection |
| MVVM | CommunityToolkit.Mvvm |
| Logging | Serilog |
| Effects | DWM API (Mica / Acrylic) |
| Storage | JSON (System.Text.Json) |

---

## Project Structure

```
BrowserPickerPro/
├── src/
│   ├── BrowserPickerPro.UI/           # WPF application, views, viewmodels
│   ├── BrowserPickerPro.Core/         # Domain models, interfaces, enums
│   ├── BrowserPickerPro.Infrastructure/  # Settings, DI, logging
│   ├── BrowserPickerPro.BrowserDetection/ # Browser discovery engine
│   ├── BrowserPickerPro.Rules/        # Rules engine, matchers
│   └── BrowserPickerPro.Security/     # Security providers
├── tests/
│   ├── BrowserPickerPro.UnitTests/
│   └── BrowserPickerPro.IntegrationTests/
├── docs/
├── scripts/
└── installer/
```

---

## License

MIT License — see [LICENSE.md](LICENSE.md)

© 2025 Parlee Conseiller, Inc. All rights reserved.
