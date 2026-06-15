# Installation — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## Option A: Download Release (Recommended)

1. Go to [Releases](https://github.com/parleeconseiller/browser-picker-pro/releases)
2. Download `BrowserPickerPro-x.x.x-win-x64.zip`
3. Extract to a permanent location (e.g., `C:\Program Files\BrowserPickerPro\`)
4. Run `BrowserPickerPro.exe`
5. Follow the **Windows Setup** wizard

No installer required. The application is a single self-contained executable.

---

## Option B: Build from Source

### Requirements
- Windows 10 21H1+ or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git

### Steps

```powershell
git clone https://github.com/parleeconseiller/browser-picker-pro.git
cd browser-picker-pro
.\scripts\build.ps1 -Test -Publish
```

Output: `.\publish\win-x64\BrowserPickerPro.exe`

---

## Registering as Default Browser

After running the app:

1. Navigate to **Windows Setup** tab
2. Click **Register as Browser** — this writes registry entries to HKCU
3. Click **Open Default Apps** — Windows Settings opens
4. In Default Apps, search for **"Browser Picker Pro"**
5. Click on it and set as default for:
   - HTTP
   - HTTPS
   - HTML file (.html)
   - HTM file (.htm)

Windows 11 requires this manual step by design. It cannot be automated.

---

## Uninstalling

1. Open Browser Picker Pro → **Windows Setup** → **Unregister**
2. Delete the application executable/folder
3. (Optional) Delete `%AppData%\BrowserPickerPro\` to remove all settings

---

## System Requirements

| Requirement | Minimum |
|---|---|
| OS | Windows 10 21H1 |
| OS (Mica effect) | Windows 11 22000+ |
| Architecture | x64 |
| RAM | ~50 MB |
| Disk | ~147 MB (self-contained) |
| .NET Runtime | Bundled (self-contained) |
