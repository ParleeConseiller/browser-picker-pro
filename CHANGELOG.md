# Changelog

All notable changes to Browser Picker Pro will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
