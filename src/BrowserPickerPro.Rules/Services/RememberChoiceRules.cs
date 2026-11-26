// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: RememberChoiceRules.cs
// Description: Builds the domain rule created when the user ticks "Remember for this
//              domain" in the picker (the checkbox was a no-op before v2.0).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.Rules.Services;

public static class RememberChoiceRules
{
    /// <summary>Marker stored in <see cref="BrowserRule.Description"/> to recognise our rules.</summary>
    public const string Marker = "Remembered from the picker";

    /// <summary>
    /// Lower than the default rule priority (100): the engine evaluates ascending, so a
    /// remembered choice wins over the rule that made the picker appear (e.g. AskAlways).
    /// </summary>
    public const int Priority = 50;

    /// <summary>
    /// Returns <paramref name="rules"/> with a single remembered rule for
    /// <paramref name="domain"/> (any previous remembered rule for it is replaced).
    /// </summary>
    public static List<BrowserRule> Upsert(IEnumerable<BrowserRule> rules, string domain,
                                           string browserId, string? profileId)
    {
        var pattern = NormalizeDomain(domain);

        var result = rules
            .Where(r => !(r.Description == Marker &&
                          r.RuleType == RuleType.Domain &&
                          string.Equals(r.Pattern, pattern, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        result.Add(new BrowserRule
        {
            Name = $"Remember {pattern}",
            Pattern = pattern,
            RuleType = RuleType.Domain,
            Action = RuleAction.OpenInBrowser,
            TargetBrowserId = browserId,
            TargetProfileId = profileId,
            Priority = Priority,
            Description = Marker,
        });
        return result;
    }

    /// <summary>"WWW.Example.com" → "example.com" (DomainMatcher also matches subdomains).</summary>
    public static string NormalizeDomain(string domain)
    {
        var d = domain.Trim().ToLowerInvariant();
        return d.StartsWith("www.", StringComparison.Ordinal) ? d[4..] : d;
    }
}
