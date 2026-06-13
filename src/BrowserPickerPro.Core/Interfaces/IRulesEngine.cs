// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.Core.Interfaces;

public interface IRulesEngine
{
    BrowserRule? FindMatchingRule(UrlRequest request, IEnumerable<BrowserRule> rules);
    bool IsMatch(string url, BrowserRule rule);
    Task<IReadOnlyList<BrowserRule>> LoadRulesAsync(CancellationToken cancellationToken = default);
    Task SaveRulesAsync(IEnumerable<BrowserRule> rules, CancellationToken cancellationToken = default);
}
