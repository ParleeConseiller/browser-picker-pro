// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.Core.Interfaces;

public interface IBrowserLauncher
{
    Task LaunchAsync(Browser browser, string url, BrowserProfile? profile = null, CancellationToken cancellationToken = default);
    Task<bool> IsRunningAsync(Browser browser, CancellationToken cancellationToken = default);
}
