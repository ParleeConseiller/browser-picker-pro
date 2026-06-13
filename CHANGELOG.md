# Changelog

All notable changes to Browser Picker Pro will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
