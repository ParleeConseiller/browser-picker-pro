// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
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

    public string GetLaunchArgs(string url, BrowserProfile? profile = null)
    {
        if (profile != null && !string.IsNullOrWhiteSpace(profile.ProfileArgument))
            return $"{profile.ProfileArgument} \"{url}\"";
        return $"\"{url}\"";
    }
}
