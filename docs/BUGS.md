<!--
  File: BUGS.md
  Description: Authoritative log of every gotcha hit in Browser Picker Pro — our own bugs
               ([Internal]) and surprising behaviour of dependencies/platform ([External]).
  Author: Jose-Jorge HERNANDEZ
  Company: Parlee Conseiller, Inc.
  Date: 2026-11-25
  Last edit date: 2026-11-25
  Version: 1.0.0
-->
# Bugs & gotchas — Browser Picker Pro

Format: `[Internal|External] **Title** — Symptom. Root cause. Fix (or workaround). File.`
Entries from v1.0.1/v1.1 were back-filled from `CHANGELOG.md` / `CLAUDE.md`.

## 2026-11-25 (v2.0)

- [Internal] **"Remember for this domain" was a no-op** — Symptom: ticking the box never changed anything. Root cause: `SelectBrowserAsync` had an empty `// Save domain rule` placeholder. Fix: `RememberChoiceRules.Upsert` saves a priority-50 domain rule. File: `src/BrowserPickerPro.UI/ViewModels/BrowserPickerViewModel.cs`, `src/BrowserPickerPro.Rules/Services/RememberChoiceRules.cs`.
- [Internal] **Browser switch injection through the URL** — Symptom: a link like `https://x/" --renderer-cmd-prefix="calc` could add switches to the browser's command line. Root cause: URL embedded as `"{url}"` without escaping. Fix: `UrlArgumentSanitizer` (quotes → `%22`, control chars removed, trailing `\` doubled, `-`/`/`-prefixed values rejected). File: `src/BrowserPickerPro.Core/Extensions/UrlArgumentSanitizer.cs`.
- [Internal] **Profile ids regenerated on every detection** — Symptom: rules targeting a profile stopped matching it. Root cause: detection creates profiles with `Guid.NewGuid()` and the merge replaced the list wholesale. Fix: `BrowserListMerger` keeps ids by `ProfileArgument`. File: `src/BrowserPickerPro.BrowserDetection/Services/BrowserListMerger.cs`.
- [Internal] **Zombie process after closing the dashboard** — Symptom: `BrowserPickerPro.exe` kept running invisibly. Root cause: `ShutdownMode="OnExplicitShutdown"` and nothing called `Shutdown()` when `MainWindow` closed. Fix: legacy mode shuts down on `Closed`; resident mode keeps running on purpose (tray). File: `src/BrowserPickerPro.UI/App.xaml.cs`.
- [Internal] **Startup deadlock (dev build)** — Symptom: resident app hung right after "Start-with-Windows enabled". Root cause: `IsRegisteredAsync().GetAwaiter().GetResult()` on the UI thread; the method's continuation needed that same thread. Fix: await it. File: `src/BrowserPickerPro.UI/App.xaml.cs`.
- [External] **"Cannot animate '(0).(1)' on an immutable object instance"** — Symptom: exception on every layout pass when a card was created already "visible". Root cause: WPF clones a frozen *Style-setter* value before animating it, but refuses a frozen *template-local* `RenderTransform` when the trigger fires during template instantiation. Fix: `TranslateTransform` moved into a Style setter; cards are always created hidden and shown by the stagger. File: `src/BrowserPickerPro.UI/Windows/BrowserPickerWindow.xaml`.
- [Internal] **1 GB log from a swallowed render-loop exception (dev build)** — Symptom: log file grew to Serilog's 1 GB default cap within minutes. Root cause: `DispatcherUnhandledException` set `Handled=true` for an exception that recurs on every frame. Fix: burst guard (>5 in 10 s → let the process die) and `fileSizeLimitBytes` 10 MB + roll. File: `src/BrowserPickerPro.UI/App.xaml.cs`, `src/BrowserPickerPro.Infrastructure/Extensions/ServiceCollectionExtensions.cs`.
- [External] **Background process can't bring its window to the front** — Symptom: `Activate()` from the resident app only flashes the taskbar. Root cause: Windows foreground lock. Workaround: the launcher (started by the user's click) calls `AllowSetForegroundWindow(ASFW_ANY)` before writing to the pipe. File: `src/BrowserPickerPro.Launcher/Program.cs`.
- [External] **H.NotifyIcon puts the whole process in Efficiency mode** — Symptom (avoided): `TaskbarIcon.ForceCreate()` defaults to `enablesEfficiencyMode: true` → EcoQoS / low priority for the entire app. Fix: `ForceCreate(enablesEfficiencyMode: false)`. File: `src/BrowserPickerPro.UI/Services/TrayHost.cs`.
- [External] **`Show()` of a layered WPF window costs ~55 ms** — Symptom: half of the reveal budget spent in `Show()` even though the window was pre-built. Workaround: keep it shown, parked off-screen as a tool window, and move it in. File: `src/BrowserPickerPro.UI/Windows/BrowserPickerWindow.xaml.cs`.
- [External] **NativeAOT publish: `'vswhere.exe' is not recognized` (MSB3073)** — Symptom: launcher publish fails at the link step. Root cause: the ILCompiler targets call `vswhere.exe` by name, and the VS Installer folder isn't on PATH. Workaround: `build.ps1` prepends `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer`. File: `scripts/build.ps1`.
- [External] **Windows PowerShell 5.1 has no 3-argument `Join-Path`** — Symptom: `build.ps1 -Publish` failed under `powershell.exe`. Fix: nested `Join-Path`. File: `scripts/build.ps1`.
- [External] **PowerShell passes `$null` as `""` to a `string` P/Invoke parameter** — Symptom: `FindWindowW($null, title)` never found the picker. Fix: `[NullString]::Value`. File: `scripts/measure-picker-latency.ps1`.
- [Internal] **CI targeted .NET 9 for a .NET 10 solution** — Symptom: none visible yet — runs passed only because the windows-latest image already ships a .NET 10 SDK; a runner image change would have broken CI. Fix: `setup-dotnet` uses `global-json-file: global.json`. File: `.github/workflows/*.yml`.

## 2026-06-12 → 06-14 (v1.0.1 / v1.1) — back-filled

- [External] **`<Run Style=…>` crash** — `Run` is a `FrameworkContentElement`; a `TargetType="TextBlock"` style throws at load. Fix: style on the parent `TextBlock`. File: `Views/SetupView.xaml`.
- [External] **`RenderTransform` on a `Window` is invalid** — Fix: animate content, `FadeIn()` for windows. File: `Helpers/AnimationHelper.cs`.
- [Internal] **Theme stayed dark** — theme dictionaries are nested inside `Theme.xaml`. Fix in `ThemeHelper.ApplyTheme`.
- [External] **Freezables in DataTemplate XAML are frozen** — mutable transforms/effects created in code instead. File: `Windows/BrowserPickerWindow.xaml.cs`.
