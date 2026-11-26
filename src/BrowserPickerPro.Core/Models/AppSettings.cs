// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: AppSettings.cs
// Description: User settings persisted to %APPDATA%\BrowserPickerPro\settings.json
//              (theme, picker appearance, tray/startup behaviour, security providers).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 1.1.0
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

    // Picker window appearance
    public PickerWindowStyle PickerWindowStyle { get; set; } = PickerWindowStyle.Modal;
    public PickerCardLayout PickerCardLayout { get; set; } = PickerCardLayout.WrapGrid;
    public PickerBackgroundMode PickerBackgroundMode { get; set; } = PickerBackgroundMode.DarkGradient;
    public string? PickerBackgroundImagePath { get; set; }
    public double PickerDarkTintOpacity { get; set; } = 0.55;
    public int PickerCardSize { get; set; } = 190;

    /// <summary>Entrance animation of the picker window.</summary>
    public PickerOpenAnimation PickerOpenAnimation { get; set; } = PickerOpenAnimation.CircleReveal;

    // Resident (tray) mode — v2.0
    /// <summary>
    /// Stay resident in the tray after a pick so the next picker opens instantly.
    /// false = legacy v1 behaviour (one process per link, exit after the pick).
    /// Takes effect on the next start.
    /// </summary>
    public bool KeepRunningInTray { get; set; } = true;

    /// <summary>Start the resident app (with --tray) when the user signs in to Windows.</summary>
    public bool StartWithWindows { get; set; } = true;
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
