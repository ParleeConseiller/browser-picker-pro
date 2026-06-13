// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BrowserPickerPro.Infrastructure.Services;

public class ProtocolRegistrarService : IProtocolRegistrar
{
    private readonly ILogger<ProtocolRegistrarService> _logger;
    private const string AppName = "BrowserPickerPro";
    private const string AppDisplayName = "Browser Picker Pro";
    private const string ProgId = "BrowserPickerProHTML";

    public ProtocolRegistrarService(ILogger<ProtocolRegistrarService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var exePath = Environment.ProcessPath
                    ?? Path.Combine(AppContext.BaseDirectory, "BrowserPickerPro.exe");
                RegisterProgId(exePath);
                RegisterCapabilities(exePath);
                _logger.LogInformation("Registered Browser Picker Pro as browser handler");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register protocol handler");
                return false;
            }
        }, cancellationToken);
    }

    private static void RegisterProgId(string exePath)
    {
        using var classesRoot = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes", true)!;

        using var progIdKey = classesRoot.CreateSubKey(ProgId);
        progIdKey.SetValue(null, AppDisplayName);
        progIdKey.SetValue("FriendlyTypeName", AppDisplayName);

        using var defaultIconKey = progIdKey.CreateSubKey("DefaultIcon");
        defaultIconKey.SetValue(null, $"\"{exePath}\",0");

        using var openWithKey = progIdKey.CreateSubKey(@"shell\open\command");
        openWithKey.SetValue(null, $"\"{exePath}\" \"%1\"");
    }

    private static void RegisterCapabilities(string exePath)
    {
        var capabilitiesPath = $@"SOFTWARE\{AppName}\Capabilities";

        using var appKey = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\{AppName}");
        appKey.SetValue("Description", AppDisplayName);
        appKey.SetValue("ApplicationIcon", $"\"{exePath}\",0");
        appKey.SetValue("ApplicationName", AppDisplayName);

        using var capKey = Registry.CurrentUser.CreateSubKey(capabilitiesPath);
        capKey.SetValue("ApplicationDescription", "Choose the right browser every time.");
        capKey.SetValue("ApplicationName", AppDisplayName);

        using var urlAssocKey = capKey.CreateSubKey("URLAssociations");
        urlAssocKey.SetValue("http", ProgId);
        urlAssocKey.SetValue("https", ProgId);
        urlAssocKey.SetValue("ftp", ProgId);

        using var fileAssocKey = capKey.CreateSubKey("FileAssociations");
        fileAssocKey.SetValue(".html", ProgId);
        fileAssocKey.SetValue(".htm", ProgId);
        fileAssocKey.SetValue(".url", ProgId);

        using var registeredApps = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\RegisteredApplications");
        registeredApps.SetValue(AppName, capabilitiesPath);
    }

    public async Task<bool> UnregisterAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"SOFTWARE\Classes\{ProgId}", false);
                Registry.CurrentUser.DeleteSubKeyTree($@"SOFTWARE\{AppName}", false);
                using var registeredApps = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\RegisteredApplications", true);
                registeredApps?.DeleteValue(AppName, false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister protocol handler");
                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> IsRegisteredAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Classes\{ProgId}");
            return key != null;
        }, cancellationToken);
    }
}
