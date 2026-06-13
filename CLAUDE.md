# CLAUDE.md — Browser Picker Pro

> This file is for Claude Code. It provides project context, build commands, conventions, and known gotchas to avoid repeating past debugging work.

**Created by Parlee Conseiller, Inc. — Main developer: Jose-Jorge HERNANDEZ**

---

## Project in one sentence

Browser Picker Pro is a Windows WPF app that intercepts HTTP/HTTPS/HTML links and shows a floating browser-selection UI. It has two modes: a browser picker (launched with a URL argument) and a configuration dashboard (launched with no arguments).

---

## Tech stack

| Layer | Technology |
|---|---|
| Language | C# 13, nullable enabled, implicit usings |
| Framework | .NET 10 / WPF (`net10.0-windows`) |
| MVVM | CommunityToolkit.Mvvm 8.3.2 (`[ObservableProperty]`, `[RelayCommand]`) |
| DI | Microsoft.Extensions.DependencyInjection |
| Logging | Serilog → `%LOCALAPPDATA%\BrowserPickerPro\Logs\` |
| Testing | xUnit + FluentAssertions + Moq |
| Window effects | DWM API P/Invoke (Mica = value 2, Acrylic = value 3) |

---

## Repository structure

```
browser-picker-pro/
├── src/
│   ├── BrowserPickerPro.Core/           # Interfaces, models, enums — zero NuGet deps
│   ├── BrowserPickerPro.BrowserDetection/  # Finds installed browsers via filesystem + registry
│   ├── BrowserPickerPro.Rules/          # Domain/wildcard/regex rule matching
│   ├── BrowserPickerPro.Security/       # VirusTotal + Google Safe Browsing providers
│   ├── BrowserPickerPro.Infrastructure/ # Settings, launcher, protocol registrar, DI wiring
│   └── BrowserPickerPro.UI/             # WPF app — windows, views, viewmodels, themes
│       ├── Windows/                     # BrowserPickerWindow, MainWindow
│       ├── Views/                       # Page views (Browsers, Rules, Security, Appearance, etc.)
│       ├── ViewModels/                  # One ViewModel per View
│       ├── Themes/                      # Dark.xaml, Light.xaml, Colors.xaml, Typography.xaml, Controls.xaml
│       ├── Helpers/                     # MicaHelper, ThemeHelper, AnimationHelper, IconHelper
│       ├── Converters/                  # All IValueConverter implementations
│       └── Services/                    # ThemeService, NavigationService, DialogService
├── tests/
│   ├── BrowserPickerPro.UnitTests/
│   └── BrowserPickerPro.IntegrationTests/
├── publish/win-x64/                     # Output from dotnet publish
├── assets/
│   └── icon.ico                         # Real app icon (source of truth)
├── docs/
├── global.json                          # Pins SDK to 10.0.300, rollForward latestMinor
└── CLAUDE.md                            # This file
```

**Dependency rule:** UI → Infrastructure → {BrowserDetection, Rules, Security} → Core. No inner layer imports an outer one.

---

## Essential commands

```powershell
# Build (check for errors)
dotnet build src/BrowserPickerPro.UI/BrowserPickerPro.UI.csproj -c Release

# Run all tests
dotnet test

# Run dashboard
dotnet run --project src/BrowserPickerPro.UI

# Simulate URL interception
dotnet run --project src/BrowserPickerPro.UI -- "https://example.com"

# Publish self-contained single-file EXE → publish/win-x64/BrowserPickerPro.exe
dotnet publish src/BrowserPickerPro.UI/BrowserPickerPro.UI.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish/win-x64
```

Before republishing, ensure no running BrowserPickerPro.exe instance is locking the file:
```powershell
Stop-Process -Name BrowserPickerPro -Force -ErrorAction SilentlyContinue
```

---

## SDK / target framework

- SDK pinned to `10.0.300` in `global.json` (installed: 10.0.300 and 10.0.301)
- All `.csproj` files target `net10.0-windows`
- All package versions use `10.0.0` for Microsoft.Extensions.* packages

---

## WPF .NET 10 gotchas — things that have already burned us

### 1. `Mode=OneWay` on read-only source properties
WPF .NET 10 throws `InvalidOperationException` ("A TwoWay or OneWayToSource binding cannot work on the read-only property") at runtime for bindings where the source property has no setter, if the default binding mode is TwoWay. Always add `Mode=OneWay` explicitly on:
- Computed/expression-bodied properties (`public int Count => ...`)
- Sub-property paths (`{Binding Collection.Count}`)
- `Run.Text` and `Run.Foreground` bindings (Run is FrameworkContentElement, not FrameworkElement)

### 2. `<Run>` cannot have `Style=`
`Run` is a `FrameworkContentElement`. Applying `Style="{StaticResource TextStyle.Body}"` (which has `TargetType="TextBlock"`) to a `<Run>` crashes at XAML load time. Put the `Style` on the parent `<TextBlock>` instead — child `<Run>` elements inherit font/foreground from it.

### 3. `RenderTransform` on a `Window` is forbidden
`AnimationHelper.ScalePop(element)` sets `element.RenderTransform` to a `ScaleTransform`. Calling this on a `Window` throws `InvalidOperationException: The transform is not valid for Window`. Use `FadeIn()` for windows.

### 4. WinUI3-only properties don't exist in WPF
- `StackPanel.Spacing` — does NOT exist in WPF (WinUI3 only). Use `Margin` on children.
- `TextBlock.LetterSpacing` — does NOT exist in WPF. Remove it.

### 5. Theme dictionary nesting
`App.xaml` merges `Themes/Theme.xaml`. `Theme.xaml` in turn merges `Dark.xaml`, `Colors.xaml`, `Typography.xaml`, `Controls.xaml`. When swapping themes at runtime, you must search inside `Theme.xaml`'s `MergedDictionaries` — NOT at the `Application.Resources` top level. The relevant code is in `ThemeHelper.ApplyTheme()`.

### 6. Single-file publish and paths
Use `Environment.ProcessPath` (not `Assembly.Location`) when you need the path to the running EXE. `Assembly.Location` returns empty string in single-file publish, triggering IL3000 warnings and runtime nulls.

---

## Theme system

```
App.xaml
  └─ Themes/Theme.xaml (merged)
       ├─ Themes/Dark.xaml   ← swapped at runtime by ThemeHelper
       ├─ Themes/Colors.xaml ← Brush.* resources referencing Theme.* colors
       ├─ Themes/Typography.xaml
       └─ Themes/Controls.xaml
```

- All XAML must use `{DynamicResource Brush.Something}` — never `StaticResource` for anything that should respond to theme changes.
- `ThemeService.Apply(AppTheme, ResourceDictionary)` drives live switching.
- Theme radio buttons use `EnumToBoolConverter` with `ConverterParameter=Dark` / `ConverterParameter=Light` / `ConverterParameter=System`.

---

## Converters (in `Converters/Converters.cs`, registered in `App.xaml`)

| Key | Class | Notes |
|---|---|---|
| `BoolToVisibility` | `BoolToVisibilityConverter` | true → Visible |
| `InverseBoolToVisibility` | `InverseBoolToVisibilityConverter` | true → Collapsed |
| `InverseBool` | `InverseBoolConverter` | negates bool |
| `NullToVisibility` | `NullToVisibilityConverter` | null or int 0 → Collapsed |
| `StringToVisibility` | `StringToVisibilityConverter` | empty/null → Collapsed |
| `EnumToBool` | `EnumToBoolConverter` | use with `ConverterParameter=EnumValue` |
| `SecurityLevelToBrush` | `SecurityLevelToBrushConverter` | |
| `SecurityLevelToIcon` | `SecurityLevelToIconConverter` | |
| `UsageCountToLabel` | `UsageCountToLabelConverter` | |

---

## Browser detection

`BrowserDetectionService` checks:
1. Known relative paths under `%ProgramFiles%`, `%ProgramFiles(x86)%`, `%LOCALAPPDATA%`, `%LOCALAPPDATA%\Programs`
2. Registry key for version confirmation
3. `HKLM\SOFTWARE\Clients\StartMenuInternet` + `WOW6432Node` fallback

Detected on this machine: Chrome, Edge, Opera (GX path), Zen Browser, Opera Stable (registry).

---

## Protocol registration

`ProtocolRegistrarService` writes to `HKCU` only — no admin required. It registers:
- ProgId: `BrowserPickerProHTML`
- URL associations: `http`, `https`, `ftp`
- File associations: `.html`, `.htm`, `.url`
- `HKCU\SOFTWARE\RegisteredApplications`

After registration the user must still manually select Browser Picker Pro in Windows Settings → Default Apps. Windows 11 enforces this for security.

---

## Logging

Log files: `%LOCALAPPDATA%\BrowserPickerPro\Logs\browserPickerPro-YYYYMMDD.log`

To check crash details without reading log files, use:
```powershell
Get-WinEvent -LogName Application -MaxEvents 20 |
    Where-Object { $_.Id -eq 1026 -and $_.Message -like "*BrowserPickerPro*" } |
    Select-Object -First 1 | ForEach-Object { $_.Message }
```

---

## App icon

- Source: `assets/icon.ico` (69 KB, valid ICO, 1 image)
- Build copy: `src/BrowserPickerPro.UI/Assets/Icons/app.ico` (copied before publish)
- Referenced in csproj as `<ApplicationIcon>Assets\Icons\app.ico</ApplicationIcon>`
- Embedded as Win32 resource; confirmed present via `[System.Drawing.Icon]::ExtractAssociatedIcon()`
- If Explorer shows old icon after republish, clear icon cache:
  ```powershell
  Stop-Process -Name explorer -Force
  Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\iconcache*.db" -Force
  Start-Process explorer.exe
  ```

---

## Known working state (as of 2026-06-12)

- **Build**: 0 errors, informational NU1510 warnings only (redundant packages, safe to ignore)
- **Tests**: 47/47 passing
- **Published EXE**: `publish/win-x64/BrowserPickerPro.exe` — ~145 MB, self-contained, win-x64
- **Dashboard**: opens, all pages navigate correctly
- **Browser picker**: opens on URL argument, shows detected browsers, Escape closes
- **Theme switching**: Light/Dark/System radio buttons work, brushes update live
- **Windows Setup page**: loads, Register/Unregister/Open Default Apps buttons work
- **Icon**: real icon embedded in EXE

---

*All source file headers must include: `// Browser Picker Pro — Created by Parlee Conseiller, Inc.`*
