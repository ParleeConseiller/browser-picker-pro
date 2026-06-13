// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;

namespace BrowserPickerPro.Core.Models;

public class SecurityResult
{
    public string Url { get; set; } = string.Empty;
    public SecurityLevel OverallLevel { get; set; } = SecurityLevel.Unknown;
    public int ReputationScore { get; set; } = -1;
    public List<ProviderSecurityResult> ProviderResults { get; set; } = new();
    public bool IsComplete { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class ProviderSecurityResult
{
    public string ProviderName { get; set; } = string.Empty;
    public SecurityLevel Level { get; set; } = SecurityLevel.Unknown;
    public string? Message { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int? Score { get; set; }
}
