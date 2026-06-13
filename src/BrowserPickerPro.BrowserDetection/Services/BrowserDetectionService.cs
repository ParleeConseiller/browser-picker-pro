// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BrowserPickerPro.BrowserDetection.Services;

public class BrowserDetectionService : IBrowserDetectionService
{
    private readonly ILogger<BrowserDetectionService> _logger;

    private static readonly IReadOnlyList<BrowserDefinition> KnownBrowsers = new List<BrowserDefinition>
    {
        new("Google Chrome",   BrowserType.Chrome,   @"Google\Chrome\Application\chrome.exe",    @"SOFTWARE\Google\Chrome\BLBeacon"),
        new("Microsoft Edge",  BrowserType.Edge,     @"Microsoft\Edge\Application\msedge.exe",   @"SOFTWARE\Microsoft\Edge\BLBeacon"),
        new("Mozilla Firefox", BrowserType.Firefox,  @"Mozilla Firefox\firefox.exe",             @"SOFTWARE\Mozilla\Mozilla Firefox"),
        new("Brave",           BrowserType.Brave,    @"BraveSoftware\Brave-Browser\Application\brave.exe",  @"SOFTWARE\BraveSoftware\Brave-Browser\BLBeacon"),
        new("Vivaldi",         BrowserType.Vivaldi,  @"Vivaldi\Application\vivaldi.exe",         @"SOFTWARE\Vivaldi"),
        new("Opera",           BrowserType.Opera,    @"Opera Software\Opera Stable\opera.exe",   @"SOFTWARE\Opera Software"),
        new("Opera GX",        BrowserType.OperaGX,  @"Opera Software\Opera GX Stable\opera.exe", null),
        new("Waterfox",        BrowserType.Waterfox, @"Waterfox\waterfox.exe",                   null),
        new("LibreWolf",       BrowserType.LibreWolf,@"LibreWolf\librewolf.exe",                 null),
        new("Zen Browser",     BrowserType.Zen,      @"Zen Browser\zen.exe",                     null),
        new("Floorp",          BrowserType.Floorp,   @"Floorp\floorp.exe",                       null),
        new("Arc",             BrowserType.Arc,      @"Arc\app\Arc.exe",                         null),
        new("Chromium",        BrowserType.Chromium, @"Chromium\Application\chrome.exe",         null),
    };

    public BrowserDetectionService(ILogger<BrowserDetectionService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<Browser>> DetectInstalledBrowsersAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => DetectBrowsersSync(), cancellationToken);
    }

    private List<Browser> DetectBrowsersSync()
    {
        var browsers = new List<Browser>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in KnownBrowsers)
        {
            var exePath = FindExecutable(definition);
            if (exePath == null || seen.Contains(exePath)) continue;

            seen.Add(exePath);
            var version = GetFileVersion(exePath);
            var browser = new Browser
            {
                Name = definition.Name,
                DisplayName = definition.Name,
                ExecutablePath = exePath,
                Version = version ?? string.Empty,
                BrowserType = definition.Type,
                IsEnabled = true,
                OrderIndex = browsers.Count,
            };
            browser.Profiles = DetectProfilesSync(browser);
            browsers.Add(browser);
            _logger.LogInformation("Detected browser: {Name} v{Version} at {Path}", browser.Name, browser.Version, browser.ExecutablePath);
        }

        DetectFromRegisteredHandlers(browsers, seen);
        return browsers;
    }

    private string? FindExecutable(BrowserDefinition def)
    {
        var programDirs = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        };

        foreach (var dir in programDirs)
        {
            var candidate = Path.Combine(dir, def.RelativePath);
            if (File.Exists(candidate)) return candidate;
        }

        if (def.RegistryPath != null)
        {
            var path = TryRegistryPath(def.RegistryPath);
            if (path != null) return path;
        }

        return TryOpenWithRegistry(def.Name);
    }

    private string? TryRegistryPath(string regPath)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(regPath);
            var version = key?.GetValue("version")?.ToString();
            if (version == null) return null;

            var installPath = key?.GetValue("InstallLocation")?.ToString();
            return installPath;
        }
        catch
        {
            return null;
        }
    }

    private string? TryOpenWithRegistry(string browserName)
    {
        try
        {
            var basePaths = new[]
            {
                @"SOFTWARE\Clients\StartMenuInternet",
                @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet"
            };

            foreach (var basePath in basePaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(basePath);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    var name = subKey?.GetValue(null)?.ToString() ?? string.Empty;
                    if (!name.Contains(browserName, StringComparison.OrdinalIgnoreCase)) continue;

                    using var commandKey = subKey?.OpenSubKey(@"shell\open\command");
                    var command = commandKey?.GetValue(null)?.ToString();
                    if (command == null) continue;

                    var match = Regex.Match(command, "\"([^\"]+)\"");
                    if (match.Success && File.Exists(match.Groups[1].Value))
                        return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Registry lookup failed for {Browser}", browserName);
        }
        return null;
    }

    private void DetectFromRegisteredHandlers(List<Browser> browsers, HashSet<string> seen)
    {
        try
        {
            var bases = new[]
            {
                @"SOFTWARE\Clients\StartMenuInternet",
                @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet"
            };

            foreach (var basePath in bases)
            {
                using var key = Registry.LocalMachine.OpenSubKey(basePath);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    using var commandKey = subKey?.OpenSubKey(@"shell\open\command");
                    var command = commandKey?.GetValue(null)?.ToString();
                    if (command == null) continue;

                    var match = Regex.Match(command, "\"([^\"]+)\"");
                    if (!match.Success) continue;

                    var exePath = match.Groups[1].Value;
                    if (!File.Exists(exePath) || seen.Contains(exePath)) continue;

                    seen.Add(exePath);
                    var name = subKey?.GetValue(null)?.ToString() ?? Path.GetFileNameWithoutExtension(exePath);
                    var browser = new Browser
                    {
                        Name = name,
                        DisplayName = name,
                        ExecutablePath = exePath,
                        Version = GetFileVersion(exePath) ?? string.Empty,
                        BrowserType = BrowserType.Unknown,
                        IsEnabled = true,
                        OrderIndex = browsers.Count,
                    };
                    browsers.Add(browser);
                    _logger.LogInformation("Detected browser from registry: {Name}", name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting browsers from registry handlers");
        }
    }

    private List<BrowserProfile> DetectProfilesSync(Browser browser)
    {
        return browser.BrowserType switch
        {
            BrowserType.Chrome => DetectChromiumProfiles(browser, "Google\\Chrome\\User Data"),
            BrowserType.Edge => DetectChromiumProfiles(browser, "Microsoft\\Edge\\User Data"),
            BrowserType.Brave => DetectChromiumProfiles(browser, "BraveSoftware\\Brave-Browser\\User Data"),
            BrowserType.Vivaldi => DetectChromiumProfiles(browser, "Vivaldi\\User Data"),
            BrowserType.Firefox => DetectFirefoxProfiles(browser),
            _ => new List<BrowserProfile>()
        };
    }

    private List<BrowserProfile> DetectChromiumProfiles(Browser browser, string relPath)
    {
        var profiles = new List<BrowserProfile>();
        try
        {
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), relPath);
            if (!Directory.Exists(userDataDir)) return profiles;

            foreach (var dir in Directory.GetDirectories(userDataDir))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName != "Default" && !dirName.StartsWith("Profile")) continue;

                var prefsFile = Path.Combine(dir, "Preferences");
                string profileName = dirName == "Default" ? "Default" : dirName;

                if (File.Exists(prefsFile))
                {
                    try
                    {
                        var prefs = System.Text.Json.JsonDocument.Parse(File.ReadAllText(prefsFile));
                        if (prefs.RootElement.TryGetProperty("profile", out var profileElem) &&
                            profileElem.TryGetProperty("name", out var nameElem))
                        {
                            profileName = nameElem.GetString() ?? profileName;
                        }
                    }
                    catch { }
                }

                profiles.Add(new BrowserProfile
                {
                    BrowserId = browser.Id,
                    Name = profileName,
                    ProfileArgument = $"--profile-directory=\"{dirName}\""
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error detecting Chromium profiles for {Browser}", browser.Name);
        }
        return profiles;
    }

    private List<BrowserProfile> DetectFirefoxProfiles(Browser browser)
    {
        var profiles = new List<BrowserProfile>();
        try
        {
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var profilesIni = Path.Combine(appDataRoaming, "Mozilla", "Firefox", "profiles.ini");
            if (!File.Exists(profilesIni)) return profiles;

            var lines = File.ReadAllLines(profilesIni);
            string? currentName = null;
            string? currentPath = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("[Profile"))
                {
                    if (currentName != null && currentPath != null)
                        profiles.Add(new BrowserProfile { BrowserId = browser.Id, Name = currentName, ProfileArgument = $"-P \"{currentName}\"" });
                    currentName = null;
                    currentPath = null;
                }
                else if (line.StartsWith("Name=")) currentName = line[5..];
                else if (line.StartsWith("Path=")) currentPath = line[5..];
            }
            if (currentName != null && currentPath != null)
                profiles.Add(new BrowserProfile { BrowserId = browser.Id, Name = currentName, ProfileArgument = $"-P \"{currentName}\"" });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error detecting Firefox profiles");
        }
        return profiles;
    }

    private static string? GetFileVersion(string exePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            return info.ProductVersion ?? info.FileVersion;
        }
        catch { return null; }
    }

    public Task<Browser?> GetBrowserByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult<Browser?>(null);

    public Task<string?> GetBrowserVersionAsync(string executablePath, CancellationToken cancellationToken = default)
        => Task.FromResult(GetFileVersion(executablePath));

    public Task<IReadOnlyList<BrowserProfile>> GetProfilesAsync(Browser browser, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BrowserProfile>>(DetectProfilesSync(browser));

    private record BrowserDefinition(string Name, BrowserType Type, string RelativePath, string? RegistryPath);
}
