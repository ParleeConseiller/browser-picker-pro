// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: Browser.cs
// Description: A browser known to the app (detected or user-added) with its profiles,
//              usage statistics and picker ordering.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 1.1.0
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Extensions;
using System.Text.Json.Serialization;

namespace BrowserPickerPro.Core.Models;

public class Browser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public BrowserType BrowserType { get; set; } = BrowserType.Unknown;
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? CustomIconPath { get; set; }
    public List<BrowserProfile> Profiles { get; set; } = new();
    public DateTime LastUsed { get; set; }
    public int UsageCount { get; set; }
    public int OrderIndex { get; set; }
    public string? UserDataDir { get; set; }

    [JsonIgnore]
    public bool IsInstalled => File.Exists(ExecutablePath);

    /// <summary>
    /// Command-line arguments that open <paramref name="url"/> in this browser, with the
    /// URL sanitized against argument injection. Empty string when the URL is rejected.
    /// </summary>
    public string GetLaunchArgs(string url, BrowserProfile? profile = null)
        => UrlArgumentSanitizer.BuildArguments(url, profile?.ProfileArgument) ?? string.Empty;
}
