// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: StartupRegistrarService.cs
// Description: Adds/removes the per-user "Run" registry entry that starts the resident
//              tray app at sign-in (HKCU only — no admin rights needed).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Ipc;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BrowserPickerPro.Infrastructure.Services;

public class StartupRegistrarService : IStartupRegistrar
{
    // Standard per-user auto-start key read by Explorer at sign-in.
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    // Name of our value under the Run key.
    private const string ValueName = "BrowserPickerPro";

    private readonly ILogger<StartupRegistrarService> _logger;

    public StartupRegistrarService(ILogger<StartupRegistrarService> logger) => _logger = logger;

    // The exact command line we register: the full app, started silently in the tray.
    private static string Command
    {
        get
        {
            // Environment.ProcessPath, not Assembly.Location (empty in single-file publish).
            var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, IpcProtocol.MainExecutableName);
            return $"\"{exe}\" {IpcProtocol.TraySwitch}";
        }
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        // Compare with the current command so a moved exe counts as "not enabled" and gets refreshed.
        return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
    }

    public void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            key.SetValue(ValueName, Command, RegistryValueKind.String);
            _logger.LogInformation("Start-with-Windows enabled: {Command}", Command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable start-with-Windows");
        }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            _logger.LogInformation("Start-with-Windows disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable start-with-Windows");
        }
    }
}
