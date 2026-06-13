// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;

namespace BrowserPickerPro.Core.Models;

public class BrowserRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public RuleType RuleType { get; set; } = RuleType.Domain;
    public RuleAction Action { get; set; } = RuleAction.OpenInBrowser;
    public string? TargetBrowserId { get; set; }
    public string? TargetProfileId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
}
