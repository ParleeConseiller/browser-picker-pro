// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.Core.Interfaces;

public interface ISecurityProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    bool IsEnabled { get; }
    Task<ProviderSecurityResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default);
}
