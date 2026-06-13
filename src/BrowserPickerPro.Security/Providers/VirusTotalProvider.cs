// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Security.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BrowserPickerPro.Security.Providers;

public class VirusTotalProvider : SecurityProviderBase
{
    private readonly string? _apiKey;
    public override string ProviderName => "VirusTotal";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);
    public override bool IsEnabled { get; }

    public VirusTotalProvider(string? apiKey, bool enabled, ILogger<VirusTotalProvider> logger, HttpClient httpClient)
        : base(logger, httpClient)
    {
        _apiKey = apiKey;
        IsEnabled = enabled;
        if (IsConfigured)
            HttpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);
    }

    protected override async Task<ProviderSecurityResult> CheckUrlCoreAsync(string url, CancellationToken cancellationToken)
    {
        var urlId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var response = await HttpClient.GetAsync($"https://www.virustotal.com/api/v3/urls/{urlId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new ProviderSecurityResult { ProviderName = ProviderName, Level = SecurityLevel.Unknown, IsAvailable = true };

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var stats = doc.RootElement
            .GetProperty("data").GetProperty("attributes")
            .GetProperty("last_analysis_stats");

        var malicious = stats.TryGetProperty("malicious", out var m) ? m.GetInt32() : 0;
        var suspicious = stats.TryGetProperty("suspicious", out var s) ? s.GetInt32() : 0;
        var total = stats.TryGetProperty("harmless", out var h) ? h.GetInt32() + malicious + suspicious : malicious + suspicious;

        var level = malicious > 2 ? SecurityLevel.Dangerous
            : malicious > 0 || suspicious > 3 ? SecurityLevel.Suspicious
            : SecurityLevel.Safe;

        return new ProviderSecurityResult
        {
            ProviderName = ProviderName,
            Level = level,
            Score = total > 0 ? (int)((1.0 - (double)(malicious + suspicious) / total) * 100) : 100,
            Message = $"{malicious} malicious, {suspicious} suspicious detections",
            IsAvailable = true
        };
    }
}
