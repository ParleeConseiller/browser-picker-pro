<!--
  File: ARCHITECTURE.md
  Description: Project layering, responsibilities and the v2.0 resident/launcher runtime design.
  Author: Jose-Jorge HERNANDEZ
  Company: Parlee Conseiller, Inc.
  Date: 2026-06-12
  Last edit date: 2026-11-25
  Version: 1.1.0
-->
# Architecture — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## Runtime model (v2.0): resident tray app + native launcher

```
 user clicks a link
        │  Windows runs the registered handler
        ▼
 BrowserPickerProLauncher.exe  (NativeAOT, ~1.9 MB, no .NET boot)
        │ 1. Mutex "Local\BrowserPickerPro.Resident.<user>" exists?
        │ 2. AllowSetForegroundWindow(ASFW_ANY)   ← passes the user's foreground right on
        │ 3. "OPEN\t<qpc ticks>\t<url>"  →  named pipe (CurrentUserOnly)  ← "OK"
        │    (no resident → start BrowserPickerPro.exe "<url>" instead = cold path)
        ▼
 BrowserPickerPro.exe  (resident, tray icon)
   IpcServer ─► Dispatcher ─► PickerHost.HandleUrlAsync
                                 ├─ rule match → IBrowserLauncher (no window at all)
                                 └─ ApplySettings (only what changed) → PrepareForRequest
                                    → BrowserPickerWindow.Reveal(): move onto the monitor
                                      under the cursor, Play() video, PickerRevealAnimator
   after pick / Esc:  Conceal() → exit animation → park off-screen (still "shown",
                      tool window, video paused) → focus back to the previous app on cancel
```

- **Why parked, not hidden:** `Show()` on the transparent (layered) window cost ~55 ms; moving an already-shown window is almost free. The window is a tool window, so it never shows in Alt+Tab or the taskbar.
- **Why a separate launcher:** the main exe is a 147 MB self-contained single file; even just forwarding the URL means booting .NET first (~450 ms measured). The launcher does it in a few ms.
- **Legacy mode:** `KeepRunningInTray=false` restores the v1 one-shot behaviour (no mutex, no pipe).
- Key types: `Core/Ipc/IpcProtocol` (shared with the launcher via a linked source file), `UI/Services/{IpcServer, PickerHost, TrayHost}`, `UI/Helpers/{PickerRevealAnimator, NativeMethods}`.

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

### `BrowserPickerPro.Launcher` (v2.0)
NativeAOT `WinExe`, registered as the link handler. Compiles `Core/Ipc/IpcProtocol.cs` in directly (no project references) to stay tiny. See `src/BrowserPickerPro.Launcher/module.md`.

### `BrowserPickerPro.UI`
WPF presentation layer using MVVM:
- `App.xaml.cs` — single-instance check (forward to resident and exit), builds DI container, starts resident tray mode (or legacy one-shot mode)
- **Services (v2.0)**: `IpcServer` (pipe), `PickerHost` (one warm picker window + VM), `TrayHost` (notification icon)
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
