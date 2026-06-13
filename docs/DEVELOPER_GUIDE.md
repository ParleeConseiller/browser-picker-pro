# Developer Guide — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## Prerequisites

- Windows 10/11 (required for WPF and DWM API)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 17.8+ or JetBrains Rider 2024.1+

---

## Building

```powershell
# Clone
git clone https://github.com/parleeconseiller/browser-picker-pro.git
cd browser-picker-pro

# Restore + Build
dotnet restore
dotnet build

# Run with the dashboard
dotnet run --project src/BrowserPickerPro.UI

# Simulate browser-picker mode
dotnet run --project src/BrowserPickerPro.UI -- "https://example.com"
```

---

## Testing

```powershell
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Single project
dotnet test tests/BrowserPickerPro.UnitTests
```

---

## Project Dependencies

```
UI → Infrastructure → Core
UI → Infrastructure → BrowserDetection → Core
UI → Infrastructure → Rules → Core
UI → Infrastructure → Security → Core
```

The `Core` project has zero NuGet dependencies beyond `Microsoft.Extensions.Logging.Abstractions`.

---

## Adding a New Browser

Edit `BrowserDetectionService.cs` and add an entry to `KnownBrowsers`:

```csharp
new("My Browser", BrowserType.Unknown, @"MyBrowser\Application\mybrowser.exe", null),
```

Add the type to `BrowserType` enum if needed.

---

## Adding a New View

1. Create `Views/MyView.xaml` + `Views/MyView.xaml.cs` (Page)
2. Create `ViewModels/MyViewModel.cs` (ObservableObject)
3. Register in `App.xaml.cs`:
   ```csharp
   services.AddTransient<MyView>();
   services.AddTransient<MyViewModel>();
   ```
4. Register navigation in `MainWindow.xaml.cs`:
   ```csharp
   _navigation.RegisterPage(NavigationSection.MySection, () => _services.GetRequiredService<MyView>());
   ```
5. Add nav item to `MainWindow.xaml`

---

## Theme System

Colors are defined in `Themes/Dark.xaml` and `Themes/Light.xaml` as `Theme.*` color resources.

`Colors.xaml` exposes `Brush.*` brushes that reference `{DynamicResource Theme.*}`. When the theme changes, `ThemeService` swaps the merged dictionary and all brushes update automatically.

Use `{DynamicResource Brush.TextPrimary}` (not `StaticResource`) in XAML to support live theme switching.

---

## Logging

Inject `ILogger<T>` anywhere. Serilog is configured in `ServiceCollectionExtensions.AddBrowserPickerLogging()`.

```csharp
_logger.LogInformation("Browser launched: {Name}", browser.Name);
_logger.LogWarning("Security check failed for {Url}", url);
_logger.LogError(ex, "Unexpected error in {Operation}", nameof(LoadAsync));
```
