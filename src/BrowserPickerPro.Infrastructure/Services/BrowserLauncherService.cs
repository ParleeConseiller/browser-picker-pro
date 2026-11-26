// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: BrowserLauncherService.cs
// Description: Starts a browser executable with a (sanitized) URL and optional profile switch.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 1.1.0
using BrowserPickerPro.Core.Extensions;
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

        // The URL comes from whatever app the user clicked in, so it is untrusted:
        // the sanitizer stops it from breaking out of its quotes into browser switches.
        var args = UrlArgumentSanitizer.BuildArguments(url, profile?.ProfileArgument);
        if (args is null)
        {
            _logger.LogWarning("Refused to launch {Browser}: URL rejected by sanitizer ({Url})", browser.DisplayName, url);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Launching {Browser} with args: {Args}", browser.DisplayName, args);

        var psi = new ProcessStartInfo
        {
            FileName = browser.ExecutablePath,
            Arguments = args,
            UseShellExecute = false,
        };

        // Dispose the Process handle right away; we never wait on the browser.
        using var _ = Process.Start(psi);
        return Task.CompletedTask;
    }

    public Task<bool> IsRunningAsync(Browser browser, CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileNameWithoutExtension(browser.ExecutablePath);
        var processes = Process.GetProcessesByName(processName);
        var running = processes.Length > 0;
        foreach (var p in processes) p.Dispose();
        return Task.FromResult(running);
    }
}
