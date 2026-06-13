// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Rules.Matchers;
using BrowserPickerPro.Rules.Services;
using Microsoft.Extensions.Logging;

namespace BrowserPickerPro.Rules.Engine;

public class RulesEngine : IRulesEngine
{
    private readonly RulesPersistenceService _persistence;
    private readonly ILogger<RulesEngine> _logger;

    public RulesEngine(RulesPersistenceService persistence, ILogger<RulesEngine> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public BrowserRule? FindMatchingRule(UrlRequest request, IEnumerable<BrowserRule> rules)
    {
        var url = request.Url;
        if (string.IsNullOrWhiteSpace(url)) return null;

        var sorted = rules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.RuleType);

        foreach (var rule in sorted)
        {
            if (IsMatch(url, rule))
            {
                _logger.LogInformation("Rule matched: {RuleName} for URL {Url}", rule.Name, url);
                return rule;
            }
        }
        return null;
    }

    public bool IsMatch(string url, BrowserRule rule)
    {
        return rule.RuleType switch
        {
            RuleType.Domain => DomainMatcher.IsMatch(url, rule.Pattern),
            RuleType.Wildcard => WildcardMatcher.IsMatch(url, rule.Pattern),
            RuleType.Regex => RegexMatcher.IsMatch(url, rule.Pattern),
            RuleType.Exact => string.Equals(url, rule.Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public Task<IReadOnlyList<BrowserRule>> LoadRulesAsync(CancellationToken cancellationToken = default)
        => _persistence.LoadAsync(cancellationToken);

    public Task SaveRulesAsync(IEnumerable<BrowserRule> rules, CancellationToken cancellationToken = default)
        => _persistence.SaveAsync(rules, cancellationToken);
}
