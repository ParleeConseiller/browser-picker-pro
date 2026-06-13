// Browser Picker Pro — Created by Parlee Conseiller, Inc.
namespace BrowserPickerPro.Core.Models;

public class BrowserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BrowserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProfileArgument { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public bool IsEnabled { get; set; } = true;
}
