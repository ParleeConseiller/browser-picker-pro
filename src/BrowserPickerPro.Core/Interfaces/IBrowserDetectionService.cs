// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.Core.Interfaces;

public interface IBrowserDetectionService
{
    Task<IReadOnlyList<Browser>> DetectInstalledBrowsersAsync(CancellationToken cancellationToken = default);
    Task<Browser?> GetBrowserByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetBrowserVersionAsync(string executablePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrowserProfile>> GetProfilesAsync(Browser browser, CancellationToken cancellationToken = default);
}
