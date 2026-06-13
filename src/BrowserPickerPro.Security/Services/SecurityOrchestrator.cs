// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using Microsoft.Extensions.Logging;

namespace BrowserPickerPro.Security.Services;

public class SecurityOrchestrator
{
    private readonly IEnumerable<ISecurityProvider> _providers;
    private readonly ILogger<SecurityOrchestrator> _logger;

    public SecurityOrchestrator(IEnumerable<ISecurityProvider> providers, ILogger<SecurityOrchestrator> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<SecurityResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = new SecurityResult { Url = url };
        var activeProviders = _providers.Where(p => p.IsEnabled && p.IsConfigured).ToList();

        if (!activeProviders.Any())
        {
            result.OverallLevel = SecurityLevel.Unknown;
            result.IsComplete = true;
            return result;
        }

        var tasks = activeProviders.Select(p => p.CheckUrlAsync(url, cancellationToken));
        var results = await Task.WhenAll(tasks);
        result.ProviderResults.AddRange(results);

        result.OverallLevel = DetermineOverallLevel(result.ProviderResults);
        result.ReputationScore = CalculateReputationScore(result.ProviderResults);
        result.IsComplete = true;

        _logger.LogInformation("Security check for {Url}: {Level} (score: {Score})", url, result.OverallLevel, result.ReputationScore);
        return result;
    }

    private static SecurityLevel DetermineOverallLevel(List<ProviderSecurityResult> results)
    {
        var available = results.Where(r => r.IsAvailable).ToList();
        if (!available.Any()) return SecurityLevel.Unknown;

        if (available.Any(r => r.Level == SecurityLevel.Dangerous)) return SecurityLevel.Dangerous;
        if (available.Any(r => r.Level == SecurityLevel.Suspicious)) return SecurityLevel.Suspicious;
        if (available.All(r => r.Level == SecurityLevel.Safe)) return SecurityLevel.Safe;
        return SecurityLevel.Unknown;
    }

    private static int CalculateReputationScore(List<ProviderSecurityResult> results)
    {
        var scored = results.Where(r => r.IsAvailable && r.Score.HasValue).ToList();
        if (!scored.Any()) return -1;
        return (int)scored.Average(r => r.Score!.Value);
    }
}
