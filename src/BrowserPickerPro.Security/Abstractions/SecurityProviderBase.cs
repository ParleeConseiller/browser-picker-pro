// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using Microsoft.Extensions.Logging;

namespace BrowserPickerPro.Security.Abstractions;

public abstract class SecurityProviderBase : ISecurityProvider
{
    protected readonly ILogger Logger;
    protected readonly HttpClient HttpClient;

    public abstract string ProviderName { get; }
    public abstract bool IsConfigured { get; }
    public abstract bool IsEnabled { get; }

    protected SecurityProviderBase(ILogger logger, HttpClient httpClient)
    {
        Logger = logger;
        HttpClient = httpClient;
        HttpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<ProviderSecurityResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !IsConfigured)
            return new ProviderSecurityResult { ProviderName = ProviderName, Level = SecurityLevel.Unknown, IsAvailable = false };

        try
        {
            return await CheckUrlCoreAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new ProviderSecurityResult { ProviderName = ProviderName, Level = SecurityLevel.Unknown, IsAvailable = false, Message = "Timed out" };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Provider} check failed for {Url}", ProviderName, url);
            return new ProviderSecurityResult { ProviderName = ProviderName, Level = SecurityLevel.Unknown, IsAvailable = false, Message = ex.Message };
        }
    }

    protected abstract Task<ProviderSecurityResult> CheckUrlCoreAsync(string url, CancellationToken cancellationToken);
}
