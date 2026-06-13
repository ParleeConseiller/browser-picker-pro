// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrowserPickerPro.Rules.Services;

public class RulesPersistenceService
{
    private readonly string _rulesFilePath;
    private readonly ILogger<RulesPersistenceService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RulesPersistenceService(ILogger<RulesPersistenceService> logger)
    {
        _logger = logger;
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BrowserPickerPro");
        Directory.CreateDirectory(appData);
        _rulesFilePath = Path.Combine(appData, "rules.json");
    }

    public async Task<IReadOnlyList<BrowserRule>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_rulesFilePath)) return Array.Empty<BrowserRule>();
            var json = await File.ReadAllTextAsync(_rulesFilePath, cancellationToken);
            return JsonSerializer.Deserialize<List<BrowserRule>>(json, JsonOptions) ?? new List<BrowserRule>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rules from {Path}", _rulesFilePath);
            return Array.Empty<BrowserRule>();
        }
    }

    public async Task SaveAsync(IEnumerable<BrowserRule> rules, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(rules.ToList(), JsonOptions);
            await File.WriteAllTextAsync(_rulesFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save rules to {Path}", _rulesFilePath);
            throw;
        }
    }
}
