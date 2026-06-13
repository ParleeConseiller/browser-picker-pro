// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.Core.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Browser>> LoadBrowsersAsync(CancellationToken cancellationToken = default);
    Task SaveBrowsersAsync(IEnumerable<Browser> browsers, CancellationToken cancellationToken = default);
    Task ExportAsync(string filePath, CancellationToken cancellationToken = default);
    Task ImportAsync(string filePath, CancellationToken cancellationToken = default);
    string AppDataPath { get; }
}
