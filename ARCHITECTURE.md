# Architecture — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## Overview

Browser Picker Pro follows **Clean Architecture** principles with a strict separation of concerns across six projects. The dependency rule flows inward: UI depends on Infrastructure, Infrastructure depends on Core. No inner layer depends on an outer one.

```
┌─────────────────────────────────────────────────────┐
│                   BrowserPickerPro.UI                │  ← Presentation
│         (WPF, MVVM, ViewModels, Views, Windows)      │
└───────────────────────┬─────────────────────────────┘
                        │ depends on
┌───────────────────────▼─────────────────────────────┐
│             BrowserPickerPro.Infrastructure          │  ← Application
│       (DI, Settings, Launcher, Registrar, Logging)   │
└──┬───────────────┬──────────────┬────────────────────┘
   │               │              │
   ▼               ▼              ▼
BrowserDetection  Rules        Security
   │               │              │
   └───────────────┴──────────────┘
                   │ depend on
┌──────────────────▼──────────────────────────────────┐
│               BrowserPickerPro.Core                  │  ← Domain
│       (Models, Interfaces, Enums, Extensions)        │
└─────────────────────────────────────────────────────┘
```

---

## Project Responsibilities

### `BrowserPickerPro.Core`
Pure domain layer. No dependencies on external packages (only abstractions).
- **Models**: `Browser`, `BrowserProfile`, `BrowserRule`, `AppSettings`, `UrlRequest`, `SecurityResult`
- **Interfaces**: `IBrowserDetectionService`, `IRulesEngine`, `ISecurityProvider`, `ISettingsService`, `IBrowserLauncher`, `IProtocolRegistrar`
- **Enums**: `AppTheme`, `RuleType`, `RuleAction`, `SecurityLevel`, `BrowserType`, `NavigationSection`, `PickerWindowStyle`, `PickerCardLayout`, `PickerBackgroundMode`
- **`AppSettings` picker properties**: `PickerWindowStyle`, `PickerCardLayout`, `PickerBackgroundMode`, `PickerBackgroundImagePath`, `PickerDarkTintOpacity`, `PickerCardSize`

### `BrowserPickerPro.BrowserDetection`
Discovers installed browsers using multiple strategies:
1. Known relative paths in `%ProgramFiles%`, `%ProgramFilesX86%`, `%LocalAppData%`
2. Registry lookup via `HKLM\SOFTWARE\Clients\StartMenuInternet`
3. Browser-specific registry keys (Chrome BLBeacon, Edge BLBeacon, etc.)

Also discovers Chromium profiles (reads `Preferences` JSON) and Firefox profiles (`profiles.ini`).

### `BrowserPickerPro.Rules`
Stateless pattern matching engine:
- `DomainMatcher` — host comparison with www-stripping
- `WildcardMatcher` — `*` and `?` globbing via regex translation
- `RegexMatcher` — full .NET regex with timeout protection
- `RulesEngine` — sorts by priority, returns first match

### `BrowserPickerPro.Security`
Pluggable security provider system:
- `ISecurityProvider` interface — any provider implements `CheckUrlAsync`
- `SecurityProviderBase` — handles errors/timeouts, wraps provider in try/catch
- `SecurityOrchestrator` — fans out to all active providers, aggregates results

### `BrowserPickerPro.Infrastructure`
Cross-cutting concerns and service implementations:
- `SettingsService` — JSON persistence in `%AppData%\BrowserPickerPro`
- `BrowserLauncherService` — `Process.Start` with profile arguments
- `ProtocolRegistrarService` — writes HKCU registry for protocol + file associations
- `ServiceCollectionExtensions` — one-call DI registration and Serilog setup

### `BrowserPickerPro.UI`
WPF presentation layer using MVVM:
- `App.xaml.cs` — builds DI container, routes to picker or dashboard
- **Windows**: `BrowserPickerWindow` (mode 1 — glassmorphism, `AllowsTransparency=True`), `MainWindow` (mode 2)
- **Views** (Pages): `BrowsersView`, `RulesView`, `SecurityView`, `AppearanceView`, `ImportExportView`, `AboutView`, `SetupView`
- **ViewModels**: One per view, all inherit `ObservableObject` from CommunityToolkit.Mvvm
- **Helpers**: `MicaHelper` (DWM P/Invoke), `ThemeHelper`, `IconHelper`, `AnimationHelper`
- **Themes**: Merged resource dictionaries — `Dark.xaml`, `Light.xaml`, `Colors.xaml`, `Typography.xaml`, `Controls.xaml` (includes `ScrollBar.Minimal` and `ScrollViewer.Modern`)

---

## Two Application Modes

### Mode 1 — Browser Selection (launched with URL argument)
```
Windows launches BrowserPickerPro.exe "https://example.com"
    │
    ▼
App.OnStartup() — parses UrlRequest
    │
    ├─── RulesEngine.FindMatchingRule()
    │         If rule found → launch browser directly → exit
    │
    └─── Show BrowserPickerWindow
              User clicks card → IBrowserLauncher.LaunchAsync() → exit
```

### Mode 2 — Configuration Dashboard (no arguments)
```
User runs BrowserPickerPro.exe
    │
    ▼
App.OnStartup() — no args → MainWindow.Show()
    │
    ▼
MainWindow → NavigationService → ContentFrame (Page navigation)
    │
    ├── BrowsersView    (detect, enable/disable, rename)
    ├── RulesView       (CRUD rules, editor panel)
    ├── SecurityView    (provider config, API keys)
    ├── AppearanceView  (theme, effects, picker options)
    ├── ImportExportView (JSON backup/restore)
    ├── SetupView       (protocol registration wizard)
    └── AboutView       (credits, version, links)
```

---

## Data Flow — Settings

```
%AppData%\BrowserPickerPro\
    ├── settings.json    ← AppSettings (theme, toggles, security keys)
    ├── browsers.json    ← List<Browser> (saved customizations)
    └── rules.json       ← List<BrowserRule>

%LocalAppData%\BrowserPickerPro\Logs\
    └── browserPickerPro-YYYYMMDD.log
```

---

## Threading Model

- All I/O operations are `async Task` — never blocks the UI thread
- Browser detection runs on `Task.Run` (CPU-bound registry scanning)
- Security checks use `Task.WhenAll` for parallel provider calls
- All ViewModel mutations happen on the UI thread via `CommunityToolkit.Mvvm` property notification

---

*© 2026 Parlee Conseiller, Inc.*
