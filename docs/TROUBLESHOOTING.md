# Troubleshooting — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## The picker doesn't appear when I click a link

**Cause:** Browser Picker Pro is not registered as the default browser handler.

**Fix:**
1. Open Browser Picker Pro (no arguments)
2. Go to **Windows Setup**
3. Click **Register as Browser**
4. In Windows Settings → Default Apps, search for "Browser Picker Pro" and set it as default for HTTP and HTTPS

---

## A browser isn't showing in the picker

**Possible causes:**
1. The browser is disabled — go to **Browsers** and toggle it on
2. The browser isn't detected — click **Refresh** to re-scan
3. The executable path changed after an update — click Refresh

---

## The Mica/glass effect isn't showing

**Cause:** Mica requires Windows 11 build 22000+ with "Transparency effects" enabled.

**Fix:**
1. Ensure you're on Windows 11
2. Go to Settings → Personalization → Colors → turn on **Transparency effects**
3. Alternatively, disable Mica in Browser Picker Pro's Appearance settings

---

## Rules aren't being applied

**Check:**
1. Is the rule enabled? (Toggle in Rules list must be ON)
2. Is the priority correct? Lower number = higher priority
3. Test your pattern: in the Rules editor, the pattern type must match the format
4. Domain rules don't support paths — use Wildcard or Regex for paths

---

## Security check always shows "Unknown"

**Fix:**
1. Verify your API key is correct
2. Check your internet connection
3. Free tier limits may be exceeded — check the provider dashboard
4. The provider may be temporarily unavailable — this is non-blocking

---

## Application crashes on startup

1. Check logs in `%LocalAppData%\BrowserPickerPro\Logs\`
2. Delete `%AppData%\BrowserPickerPro\settings.json` to reset to defaults
3. The application is self-contained — no separate .NET runtime installation is needed
4. File a bug report with the log contents

---

## Logs

Logs are written to:
```
%LocalAppData%\BrowserPickerPro\Logs\browserPickerPro-YYYYMMDD.log
```

Rolling daily, kept for 7 days.
