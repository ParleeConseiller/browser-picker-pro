<!--
  File: CHANGELOG.md
  Description: Release history of Browser Picker Pro (Keep a Changelog format).
  Author: Jose-Jorge HERNANDEZ
  Company: Parlee Conseiller, Inc.
  Date: 2026-06-12
  Last edit date: 2026-11-26
  Version: 2.1.0
-->
# Changelog

All notable changes to Browser Picker Pro will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.1.0] — 2026-11-26

### Fixed
- **Links and .html files did nothing at all when Browser Picker Pro was the default browser.** The resident tray app had been started from an elevated (administrator) shell, so it ran at High integrity. Links come from normal Medium-integrity apps (Explorer, Discord, mail…), and their launcher can't open an elevated process's named pipe. The launcher fell back to starting `BrowserPickerPro.exe`, which found the resident mutex, failed to forward the URL just as silently, and exited with code 0. Result: no picker, no log line, no crash report.
  - The app now **never runs elevated**: started elevated, it relaunches itself at the normal integrity level through the desktop's Explorer (`IShellDispatch2.ShellExecute` on the desktop shell view) and exits. When UAC is off (Explorer elevated too) it stays put and logs a warning instead of looping.
  - **No more silent drops**: if a second instance can't reach the resident app, it now shows a one-shot picker itself and logs a warning.

### Added
- `src/BrowserPickerPro.UI/Helpers/Elevation.cs` — elevation check and the unelevated relaunch.

---

## [2.0.0] — 2026-11-25

### Added
- **Resident tray mode** — Browser Picker Pro now stays running in the notification area (H.NotifyIcon.Wpf 2.4.1). The picker window, its view model and the background video are created once, pre-warmed, and reused for every link. After a pick (or Esc) the picker hides back to the tray instead of exiting. Tray menu: Settings…, Refresh browsers, Start with Windows, Exit.
- **`BrowserPickerProLauncher.exe`** (new project `src/BrowserPickerPro.Launcher`, NativeAOT, ~1.9 MB) — the registered link handler. It hands the URL to the resident app over a per-user named pipe (`IpcProtocol`) and exits in a few milliseconds; with no resident instance it cold-starts `BrowserPickerPro.exe`. Measured click → picker first frame: **~50–80 ms** (v1.1: ~1.5 s). See `artifacts/picker_latency.ipynb`.
- **Opening animations** (`PickerRevealAnimator`, setting `PickerOpenAnimation`): Circle reveal from the cursor with a glowing ring (default), Zoom & focus, Spring rise, CRT power-on, Surprise me (random), None. Smooth exit animation on hide. Respects "Smooth animations" and Windows' reduced-motion setting. **Preview picker** button in Settings → Appearance.
- **Start with Windows** (`IStartupRegistrar`, HKCU Run key with `--tray`) and **Keep running in the tray** settings; the latter set to off keeps the v1 one-shot behaviour.
- Picker opens centred on the monitor under the cursor (per-monitor DPI aware) and gives the focus back to the previous app on cancel.
- `scripts/measure-picker-latency.ps1` — repeatable click-to-screen latency measurement.
- Single-instance handling: a second `BrowserPickerPro.exe` forwards its URL to the resident instance and exits.
- `technical_memory/technical_memory.ipynb`, `docs/BUGS.md`, `docs/operations_guide.html`, `docs/executive_overview.html`, `.assetignore`.

### Fixed
- **"Remember for this domain" did nothing** — the checkbox had an empty placeholder. It now saves a priority-50 domain rule (`RememberChoiceRules`) that wins over the rule that showed the picker.
- **Command-line injection through crafted URLs** — a URL containing `"` could break out of its quotes and add browser switches (e.g. `--renderer-cmd-prefix`). URLs are now sanitized (`UrlArgumentSanitizer`): quotes percent-encoded, control characters removed, trailing backslashes doubled, switch-like values rejected.
- **Profile ids changed on every detection**, so rules targeting a profile broke. `BrowserListMerger` keeps existing profile ids; rule-matched launches now also honour the rule's profile (v1 ignored it).
- **Closing the dashboard left an invisible process running** (legacy mode, `ShutdownMode=OnExplicitShutdown`).
- Browser detection (disk + registry scan) no longer runs on every link; it runs once in the background and after the dashboard closes.
- Serilog file size capped at 10 MB per file (default was 1 GB).

### Changed
- Protocol registration points `shell\open\command` at the launcher when it is deployed; an existing registration is refreshed automatically on startup. Installer (`Product.wxs`) ships the launcher and registers it.
- CI workflows set up .NET 9 while the solution targets .NET 10 (they only built because the runner image happens to ship a .NET 10 SDK) — now use `global.json`. Release workflow publishes the launcher too.
- `build.ps1 -Publish` also publishes the launcher, stops only instances running from the output folder (by PID), and works on Windows PowerShell 5.1.
- Removed redundant `Microsoft.Win32.Registry` / `System.Text.Json` package references (NU1510) — build is now warning-free.
- Product version 1.0.0 → 2.0.0 in `Directory.Build.props` (it lagged behind the changelog).

---

## [1.1.0] — 2026-06-14

### Added
- **Glassmorphism picker window** — Complete redesign of `BrowserPickerWindow`. `AllowsTransparency="True"`, dark gradient background with radial accent glow, top glass-highlight line, and a semi-transparent glass tint overlay. Cards use `#18FFFFFF` background with gradient border.
- **Drag-to-reorder browser cards** — Cards in the picker can be reordered by dragging. Order is persisted across sessions via `OrderIndex`. Implemented with native WPF `DragDrop` (no third-party library); target card dims to 45% opacity on `DragEnter`.
- **Staggered entrance animations** — Browser cards animate in one by one (opacity 0→1, Y-translate 28→0, CubicEase 380ms) with a per-card delay of 80ms + 65ms × index.
- **Premium 3D card hover effect** — On hover: 1.08 scale, ±14° skew tilt tracking the cursor, −5 px Y lift, enlarged `DropShadowEffect` (created in code, not XAML). A specular shine `RadialGradientBrush` (created in code) follows the cursor position across the card surface, simulating a moving light reflection. On `MouseLeave`: `ElasticEase` (Oscillations=2, Springiness=5) spring-back over 480ms — card bounces through neutral before settling.
- **Customizable picker settings** (all in Settings → Appearance → Picker Window Style):
  - *Window mode*: Modal (floating, shadow, corner radius) or Fullscreen overlay
  - *Card layout*: WrapGrid or Single Row (horizontal scroll)
  - *Background*: Dark gradient (default) or Custom file (image, GIF, MP4, WebM — videos loop automatically)
  - *Dark tint slider*: 0–90% overlay opacity
  - *Card size slider*: 140–260 px
- **Custom scrollbar** (`ScrollViewer.Modern` + `ScrollBar.Minimal`) — 8 px pill-shaped thumb, no arrows, fully transparent track, replaces the default Windows scrollbar in the picker.

### Technical
- New enums in `Core/Enums/AppEnums.cs`: `PickerWindowStyle`, `PickerCardLayout`, `PickerBackgroundMode`
- Six new properties on `AppSettings` for picker configuration
- `BrowserPickerViewModel` exposes `CardWidth`/`CardHeight` for data-bound card sizing
- `BrowserItemViewModel` gains `IsEntranceVisible` for staggered animation triggering
- All card transforms and effects created in code-behind to avoid WPF DataTemplate `Freezable` freeze (see `EnsureTiltTransform`, `EnsureShadow`, `FindShineLayer`)

---

## [1.0.1] — 2026-06-12

### Fixed
- **Crash on Windows Setup page** — `<Run Style=...>` elements in SetupView.xaml used a `TargetType="TextBlock"` style on `Run` (a `FrameworkContentElement`). WPF .NET 10 enforces the type check strictly. Style moved to parent `<TextBlock>`.
- **Crash when opening URL / .url files** — `AnimationHelper.ScalePop()` attempted to set `RenderTransform` on `BrowserPickerWindow` (a `Window`). WPF prohibits transforms on windows. Replaced with `FadeIn()`.
- **Theme always stayed dark** — `ThemeHelper.ApplyTheme` searched `Application.Resources.MergedDictionaries` for `Dark.xaml`/`Light.xaml`, but those are nested one level deeper inside `Theme.xaml`'s own `MergedDictionaries`. The old theme was never removed; fix navigates into the correct level.
- **Theme radio buttons non-functional** — Appearance page RadioButtons for Light/Dark had no bindings; System used a null converter. Added `EnumToBoolConverter` and wired all three buttons.
- **Crash on Browsers page** — WPF .NET 10 treats the default binding mode as TwoWay for several properties, causing `InvalidOperationException` on read-only source properties (`ProfileCount`, `Rules.Count`, `SecurityResult.OverallLevel`). Explicit `Mode=OneWay` added to affected bindings.
- **Profile badge always visible** — `NullToVisibilityConverter` returned `Visible` for any `int` (since ints are never null). Updated to treat `0` as `Collapsed`.
- **Rules empty-state binding broken** — Replaced `Rules.Count` + `NullToVisibilityConverter` (wrong logic for zero-state) with `IsRulesEmpty` bool property + `BoolToVisibilityConverter`.

---

## [1.0.0] — 2026-06-12

### Added
- Initial release of Browser Picker Pro
- Browser Selection Mode: beautiful floating picker window with animated cards
- Configuration Dashboard with Fluent Design / Mica effect
- Automatic detection of Chrome, Edge, Firefox, Brave, Vivaldi, Opera, Opera GX, Waterfox, LibreWolf, Zen, Floorp, Arc
- Rules Engine: domain, wildcard, regex, and exact URL matching
- Multi-profile support for Chromium-based browsers and Firefox
- Security integration: VirusTotal and Google Safe Browsing providers
- Dark mode, light mode, and system-aware automatic theme
- Import/Export configuration as JSON
- Windows protocol registration (HTTP, HTTPS, FTP, HTML, HTM, URL)
- Setup Wizard with step-by-step instructions
- Keyboard navigation (Escape to cancel, Enter to select)
- Serilog rolling log files in `%LOCALAPPDATA%\BrowserPickerPro\Logs`
- GitHub Actions CI/CD workflows

### Technical
- .NET 10, WPF, MVVM with CommunityToolkit.Mvvm
- Clean Architecture with separate Core, Infrastructure, BrowserDetection, Rules, and Security projects
- DWM API integration for Mica and Acrylic window effects
- Microsoft.Extensions.DependencyInjection for full DI support
- xUnit + FluentAssertions + Moq test suite

---

*Created by Parlee Conseiller, Inc. — Main developer: Jose-Jorge HERNANDEZ*
