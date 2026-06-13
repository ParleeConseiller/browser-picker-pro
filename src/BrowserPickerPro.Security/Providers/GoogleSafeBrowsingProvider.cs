// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Security.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BrowserPickerPro.Security.Providers;

public class GoogleSafeBrowsingProvider : SecurityProviderBase
{
    private readonly string? _apiKey;
    public override string ProviderName => "Google Safe Browsing";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);
    public override bool IsEnabled { get; }

    public GoogleSafeBrowsingProvider(string? apiKey, bool enabled, ILogger<GoogleSafeBrowsingProvider> logger, HttpClient httpClient)
        : base(logger, httpClient)
    {
        _apiKey = apiKey;
        IsEnabled = enabled;
    }

    protected override async Task<ProviderSecurityResult> CheckUrlCoreAsync(string url, CancellationToken cancellationToken)
    {
        var payload = new
        {
            client = new { clientId = "BrowserPickerPro", clientVersion = "1.0.0" },
            threatInfo = new
            {
                threatTypes = new[] { "MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION" },
                platformTypes = new[] { "WINDOWS" },
                threatEntryTypes = new[] { "URL" },
                threatEntries = new[] { new { url } }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var endpoint = $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={_apiKey}";
        var response = await HttpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new ProviderSecurityResult { ProviderName = ProviderName, Level = SecurityLevel.Unknown, IsAvailable = true };

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var hasThreat = doc.RootElement.TryGetProperty("matches", out var matches) && matches.GetArrayLength() > 0;

        return new ProviderSecurityResult
        {
            ProviderName = ProviderName,
            Level = hasThreat ? SecurityLevel.Dangerous : SecurityLevel.Safe,
            Score = hasThreat ? 0 : 100,
            Message = hasThreat ? "Threat detected by Google Safe Browsing" : "No threats detected",
            IsAvailable = true
        };
    }
}
