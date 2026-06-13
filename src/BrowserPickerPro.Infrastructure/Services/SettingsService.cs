// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrowserPickerPro.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public string AppDataPath { get; }

    private string SettingsFilePath => Path.Combine(AppDataPath, "settings.json");
    private string BrowsersFilePath => Path.Combine(AppDataPath, "browsers.json");

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserPickerPro");
        Directory.CreateDirectory(AppDataPath);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) return new AppSettings();
            var json = await File.ReadAllTextAsync(SettingsFilePath, cancellationToken);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task<IReadOnlyList<Browser>> LoadBrowsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(BrowsersFilePath)) return Array.Empty<Browser>();
            var json = await File.ReadAllTextAsync(BrowsersFilePath, cancellationToken);
            return JsonSerializer.Deserialize<List<Browser>>(json, JsonOptions) ?? new List<Browser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load browsers");
            return Array.Empty<Browser>();
        }
    }

    public async Task SaveBrowsersAsync(IEnumerable<Browser> browsers, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(browsers.ToList(), JsonOptions);
            await File.WriteAllTextAsync(BrowsersFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save browsers");
            throw;
        }
    }

    public async Task ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var export = new
        {
            ExportedAt = DateTime.UtcNow,
            Version = "1.0",
            Settings = await LoadAsync(cancellationToken),
            Browsers = await LoadBrowsersAsync(cancellationToken)
        };
        var json = JsonSerializer.Serialize(export, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    public async Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("Settings", out var settingsElem))
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(settingsElem.GetRawText(), JsonOptions);
            if (settings != null) await SaveAsync(settings, cancellationToken);
        }

        if (doc.RootElement.TryGetProperty("Browsers", out var browsersElem))
        {
            var browsers = JsonSerializer.Deserialize<List<Browser>>(browsersElem.GetRawText(), JsonOptions);
            if (browsers != null) await SaveBrowsersAsync(browsers, cancellationToken);
        }
    }
}
