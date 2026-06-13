// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BrowserPickerPro.Infrastructure.Services;

public class BrowserLauncherService : IBrowserLauncher
{
    private readonly ILogger<BrowserLauncherService> _logger;

    public BrowserLauncherService(ILogger<BrowserLauncherService> logger)
    {
        _logger = logger;
    }

    public Task LaunchAsync(Browser browser, string url, BrowserProfile? profile = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(browser.ExecutablePath))
            throw new FileNotFoundException($"Browser executable not found: {browser.ExecutablePath}");

        var args = BuildArguments(url, profile);
        _logger.LogInformation("Launching {Browser} with args: {Args}", browser.DisplayName, args);

        var psi = new ProcessStartInfo
        {
            FileName = browser.ExecutablePath,
            Arguments = args,
            UseShellExecute = false,
        };

        Process.Start(psi);
        return Task.CompletedTask;
    }

    public Task<bool> IsRunningAsync(Browser browser, CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileNameWithoutExtension(browser.ExecutablePath);
        var running = Process.GetProcessesByName(processName).Length > 0;
        return Task.FromResult(running);
    }

    private static string BuildArguments(string url, BrowserProfile? profile)
    {
        if (profile != null && !string.IsNullOrWhiteSpace(profile.ProfileArgument))
            return $"{profile.ProfileArgument} \"{url}\"";
        return $"\"{url}\"";
    }
}
