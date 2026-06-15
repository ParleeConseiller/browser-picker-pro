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

### 7. WPF freezes `Freezable` objects defined in `DataTemplate` XAML
WPF parses and freezes `Freezable` objects (`TransformGroup`, `ScaleTransform`, `SkewTransform`, `DropShadowEffect`, etc.) during XAML loading — even when placed directly on elements inside a `DataTemplate`, not in a `Style`. Any mutation (property assignment or `BeginAnimation`) on a frozen object throws `InvalidOperationException: Cannot change a frozen object`. **The fix**: never define mutable transforms or effects in XAML for DataTemplate items. Create them entirely in code-behind on first use:

```csharp
private static TransformGroup EnsureTiltTransform(Button btn)
{
    if (btn.RenderTransform is TransformGroup tg && !tg.IsFrozen && tg.Children.Count >= 3)
        return tg;
    var newTg = new TransformGroup();
    newTg.Children.Add(new ScaleTransform(1, 1));
    newTg.Children.Add(new SkewTransform(0, 0));
    newTg.Children.Add(new TranslateTransform(0, 0));
    btn.RenderTransformOrigin = new Point(0.5, 0.5);
    btn.RenderTransform = newTg;
    return newTg;
}
```

### 8. `BeginAnimation` owns the property — no direct assignment afterwards
After `BeginAnimation` is called with `FillBehavior=HoldEnd` (the default), the animation clock owns that property. Any direct property assignment (`obj.Prop = value`) afterwards throws `InvalidOperationException`. Once you animate a property with `BeginAnimation`, all future mutations must also go through `BeginAnimation`. To stop the animation and restore direct control, call `BeginAnimation(prop, null)` first.

### 9. `DropShadowEffect x:Name` inside `ControlTemplate` cannot be a `Trigger.Setter` target
A `DropShadowEffect` with `x:Name="MyShadow"` inside a `ControlTemplate` throws `MC4111: Cannot find trigger target 'MyShadow'` at build time when referenced as `TargetName` in a `Trigger.Setter`. Effects cannot be trigger targets this way. Solutions:
- Create the `DropShadowEffect` in code-behind (avoids all freezing and targeting issues)
- Or use `Trigger.Setter` on the parent element's `Effect` property directly (replaces the whole effect object)

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

## Browser picker — drag-to-reorder

Browsers in `BrowserPickerWindow` can be dragged to reorder. Implemented with native WPF `DragDrop` — no third-party library.

- **Drag detection**: `PreviewMouseLeftButtonDown` records start point; `PreviewMouseMove` starts `DragDrop.DoDragDrop()` once the movement exceeds `SystemParameters.MinimumHorizontalDragDistance` / `MinimumVerticalDragDistance`. Short movements still fire as normal clicks.
- **Visual feedback**: target card opacity drops to 0.45 on `DragEnter`, restored on `DragLeave`/`Drop`.
- **Reorder logic**: `BrowserPickerViewModel.MoveItem()` calls `ObservableCollection.Move()` on both `Browsers` and `FilteredBrowsers`, reassigns `Browser.OrderIndex` for every item, then fires `SaveBrowsersAsync`. Order persists across sessions.
- **Key files**: `Windows/BrowserPickerWindow.xaml` (event bindings), `Windows/BrowserPickerWindow.xaml.cs` (drag handlers), `ViewModels/BrowserPickerViewModel.cs` (`MoveItem`).

---

## Browser picker — window design

`BrowserPickerWindow` is a glassmorphism-style floating modal (`AllowsTransparency="True"`, `WindowStyle="None"`, `Background="Transparent"`). It supports two modes and is fully configurable from Settings → Appearance → Picker Window Style.

**Layout layers (inside `InnerContainer` which clips to rounded corners):**
1. `BgGradient` — default dark gradient (`#0D1117` → `#161B2E`)
2. Radial accent `Ellipse` — subtle blue glow at center (opacity 0.08)
3. `BgImage` / `BgMedia` — optional custom static image or animated video (set in settings)
4. `DarkTintOverlay` — black semi-transparent overlay; opacity driven by `PickerDarkTintOpacity` setting
5. Top glass highlight line — 1px horizontal gradient for edge definition

**Modes (set in `AppSettings.PickerWindowStyle`):**
- `Modal` — 1000×640, centered, drop shadow, CornerRadius=22, margin=32
- `Fullscreen` — `WindowState=Maximized`, all margins/radius/shadow removed at runtime in `ApplyPickerSettings()`

**Card layout (set in `AppSettings.PickerCardLayout`):**
- `WrapGrid` — default `WrapPanel`, cards wrap across rows
- `SingleRow` — `StackPanel(Horizontal)` injected via `FrameworkElementFactory`; horizontal scroll enabled

**Background modes (set in `AppSettings.PickerBackgroundMode`):**
- `DarkGradient` — the default gradient layers above
- `CustomFile` — user picks a path; images show in `BgImage`, video/GIF files in `BgMedia` (loops via `BgMedia_MediaEnded`)

**Card size** is controlled by `AppSettings.PickerCardSize` (140–260 px). `BrowserPickerViewModel.CardWidth` / `CardHeight` expose these as bindable properties; cards bind to them with `Mode=OneWay`.

---

## Browser picker — card hover effect

Each browser card (`BrowserCardStyle` `Button`) has a premium multi-layer hover effect. **All transforms and effects are created entirely in code-behind** — never in XAML — to avoid WPF's DataTemplate Freezable-freezing (see gotcha #7).

**On `MouseMove` (`BrowserCard_Tilt`):**
- `EnsureTiltTransform(btn)` — creates a `TransformGroup` with 3 children: `ScaleTransform`, `SkewTransform`, `TranslateTransform`. On first call only; subsequent calls return the existing group.
- **Scale**: 1.0 → 1.08 (100ms, CubicEaseOut) — card pops toward viewer
- **Skew**: ±14° on X and Y axes driven by normalized cursor position — perspective-like tilt
- **Translate Y**: 0 → -5 px (100ms) — subtle physical lift off the surface
- `EnsureShadow(btn)` — creates `DropShadowEffect` in code (mutable). On hover: `ShadowDepth=18`, `BlurRadius=32`, `Opacity=0.50` — elevated shadow
- **Specular shine** (`Tag="ShineLayer"` `Border` found via `FindShineLayer()`): a `RadialGradientBrush` (white #41 → transparent) is created in code on first hover; its `Center`/`GradientOrigin` tracks the cursor position, simulating a light reflection moving across the card surface. Fades in at 120ms.

**On `MouseLeave` (`BrowserCard_ResetTilt`):**
- All three transforms animate back to identity with `ElasticEase { Oscillations=2, Springiness=5 }` over 480ms — the card bounces slightly past zero before settling, giving a physical "snap back" feel.
- Shadow resets to resting values (`ShadowDepth=4`, `BlurRadius=16`, `Opacity=0.15`).
- Shine fades out at 200ms.

**Key files**: `Windows/BrowserPickerWindow.xaml` (`ShineLayer` Border in DataTemplate, `BrowserCardStyle` ControlTemplate without effects), `Windows/BrowserPickerWindow.xaml.cs` (`EnsureTiltTransform`, `EnsureShadow`, `FindShineLayer`, `BrowserCard_Tilt`, `BrowserCard_ResetTilt`).

---

## Browser picker — settings model

Three new enums in `Core/Enums/AppEnums.cs`:
```csharp
public enum PickerWindowStyle  { Modal, Fullscreen }
public enum PickerCardLayout   { WrapGrid, SingleRow }
public enum PickerBackgroundMode { DarkGradient, CustomFile }
```

Six new properties in `Core/Models/AppSettings.cs`:
```csharp
public PickerWindowStyle     PickerWindowStyle           { get; set; } = PickerWindowStyle.Modal;
public PickerCardLayout      PickerCardLayout            { get; set; } = PickerCardLayout.WrapGrid;
public PickerBackgroundMode  PickerBackgroundMode        { get; set; } = PickerBackgroundMode.DarkGradient;
public string?               PickerBackgroundImagePath   { get; set; }
public double                PickerDarkTintOpacity       { get; set; } = 0.55;
public int                   PickerCardSize              { get; set; } = 190;
```

These are exposed in `AppearanceViewModel` and edited in `Views/AppearanceView.xaml` under "Picker Window Style".

---

## Known working state (as of 2026-06-14)

- **Build**: 0 errors, informational NU1510 / CS8604 warnings only (pre-existing, safe to ignore)
- **Tests**: 47/47 passing
- **Published EXE**: `publish/win-x64/BrowserPickerPro.exe` — ~147 MB, self-contained, win-x64
- **Dashboard**: opens, all pages navigate correctly
- **Browser picker**: opens on URL argument, shows detected browsers, Escape closes, drag-to-reorder works
- **Card hover effect**: scale + skew + lift + directional shadow + specular shine + ElasticEase spring-back
- **Staggered entrance**: browser cards animate in one by one (80ms base + 65ms × index delay)
- **Picker settings**: Modal/Fullscreen, WrapGrid/SingleRow, DarkGradient/CustomFile background, dark tint slider, card size slider — all saved and applied at picker open time
- **Custom backgrounds**: static images (BgImage) and video/GIF (BgMedia with loop) supported
- **Theme switching**: Light/Dark/System radio buttons work, brushes update live
- **Windows Setup page**: loads, Register/Unregister/Open Default Apps buttons work
- **Icon**: real icon embedded in EXE

---

*All source file headers must include: `// Browser Picker Pro — Created by Parlee Conseiller, Inc.`*
