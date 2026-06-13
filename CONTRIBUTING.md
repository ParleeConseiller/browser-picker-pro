# Contributing to Browser Picker Pro

Thank you for your interest in contributing!

*Created by Parlee Conseiller, Inc.*

---

## Development Setup

### Prerequisites
- Windows 10 22H2 or Windows 11 (for Mica effects)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 17.8+ or JetBrains Rider 2024+

### Clone and Build

```powershell
git clone https://github.com/parleeconseiller/browser-picker-pro.git
cd browser-picker-pro
.\scripts\build.ps1 -Test
```

### Run

```powershell
# Open dashboard
dotnet run --project src/BrowserPickerPro.UI

# Test browser picker
dotnet run --project src/BrowserPickerPro.UI -- "https://google.com"
```

---

## Code Style

- Follow existing C# conventions (nullable enabled, implicit usings)
- MVVM: ViewModels must not reference UI types
- No `TODO` comments in merged code — open an issue instead
- Keep each class focused on a single responsibility

## Pull Request Process

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Write tests for new functionality
4. Ensure `.\scripts\build.ps1 -Test` passes
5. Open a PR against `develop`

## Reporting Issues

Use [GitHub Issues](https://github.com/parleeconseiller/browser-picker-pro/issues) with:
- Windows version
- App version
- Steps to reproduce
- Expected vs. actual behavior
