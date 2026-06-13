// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;

namespace BrowserPickerPro.Core.Models;

public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string AccentColor { get; set; } = "#0078D4";
    public bool EnableSecurityCheck { get; set; } = false;
    public bool ShowBrowserVersion { get; set; } = true;
    public bool ShowRecentUsage { get; set; } = true;
    public bool RememberLastBrowser { get; set; } = false;
    public string? LastUsedBrowserId { get; set; }
    public bool AlwaysOnTop { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool EnableAnimations { get; set; } = true;
    public bool EnableMicaEffect { get; set; } = true;
    public bool CloseAfterSelection { get; set; } = true;
    public bool ShowUrlInPicker { get; set; } = true;
    public int PickerWindowColumns { get; set; } = 3;
    public SecuritySettings Security { get; set; } = new();
    public string ConfigVersion { get; set; } = "1.0";
}

public class SecuritySettings
{
    public bool EnableVirusTotal { get; set; }
    public string? VirusTotalApiKey { get; set; }
    public bool EnableGoogleSafeBrowsing { get; set; }
    public string? GoogleSafeBrowsingApiKey { get; set; }
    public bool EnableNortonSafeWeb { get; set; }
    public bool EnableIpQualityScore { get; set; }
    public string? IpQualityScoreApiKey { get; set; }
    public bool BlockDangerous { get; set; } = false;
    public bool WarnSuspicious { get; set; } = true;
}
