<!--
  File: module.md
  Description: What the BrowserPickerPro.Launcher module is, how it works and how to build it.
  Author: Jose-Jorge HERNANDEZ
  Company: Parlee Conseiller, Inc.
  Date: 2026-11-25
  Last edit date: 2026-11-25
  Version: 1.0.0
-->
# Module: BrowserPickerPro.Launcher

`BrowserPickerProLauncher.exe` is the program Windows runs for every http/https/ftp link and
.html/.htm/.url file once Browser Picker Pro is registered. Its only job is to get the link to
the resident tray app as fast as possible.

## Flow
1. Records `Stopwatch.GetTimestamp()` (machine-wide QPC ticks) for latency logging.
2. If the mutex `Local\BrowserPickerPro.Resident.<user>` exists → `AllowSetForegroundWindow(ASFW_ANY)`
   and send `OPEN\t<ticks>\t<url>` (or `SETTINGS` with no argument) over the per-user pipe
   `BrowserPickerPro.Picker.<user>.<session>`; exit on `OK`.
3. Otherwise (or if the send fails within 3 s) start `BrowserPickerPro.exe "<url>"` from the
   same folder (cold path), or `--settings` when there is no URL.

Exit codes: 0 = handed off / started, 2 = `BrowserPickerPro.exe` missing, 3 = start failed.

## Design constraints
- **NativeAOT** (`PublishAot`), `WinExe` (no console flash), invariant globalization: ~1.9 MB,
  starts in a few milliseconds — no .NET runtime boot, no single-file extraction.
- **No project references.** It compiles `../BrowserPickerPro.Core/Ipc/IpcProtocol.cs` in as a
  linked file, so the protocol has one source of truth and the binary stays tiny.
  `IpcProtocol.cs` must therefore stay AOT/trim-safe (no reflection, no JSON).

## Build
```powershell
.\scripts\build.ps1 -Publish   # publishes the app AND this launcher into publish\win-x64
```
Needs the Visual Studio "Desktop development with C++" workload (MSVC `link.exe`). The ILCompiler
calls `vswhere.exe` by name; `build.ps1` adds the VS Installer folder to PATH.

## Docs
Root-level details live in `/ARCHITECTURE.md` (Runtime model), `/docs/BUGS.md` and
`/technical_memory/technical_memory.ipynb`. Module guides: `docs/operations_guide.html`,
`docs/executive_overview.html`.
