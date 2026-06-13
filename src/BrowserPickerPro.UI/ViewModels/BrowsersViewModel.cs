// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace BrowserPickerPro.UI.ViewModels;

public partial class BrowsersViewModel : ObservableObject
{
    private readonly IBrowserDetectionService _detectionService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<BrowsersViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<BrowserEntryViewModel> Browsers { get; } = new();

    public BrowsersViewModel(
        IBrowserDetectionService detectionService,
        ISettingsService settingsService,
        ILogger<BrowsersViewModel> logger)
    {
        _detectionService = detectionService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Detecting browsers...";
        try
        {
            var saved = await _settingsService.LoadBrowsersAsync();
            var detected = await _detectionService.DetectInstalledBrowsersAsync();

            var merged = MergeBrowsers(saved.ToList(), detected.ToList());
            Browsers.Clear();
            foreach (var b in merged.OrderBy(x => x.OrderIndex))
                Browsers.Add(new BrowserEntryViewModel(b));

            StatusMessage = $"{Browsers.Count} browser{(Browsers.Count == 1 ? "" : "s")} found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load browsers");
            StatusMessage = "Error loading browsers";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing...";
        try
        {
            var detected = await _detectionService.DetectInstalledBrowsersAsync();
            var existing = Browsers.Select(b => b.Browser).ToList();
            var merged = MergeBrowsers(existing, detected.ToList());

            Browsers.Clear();
            foreach (var b in merged.OrderBy(x => x.OrderIndex))
                Browsers.Add(new BrowserEntryViewModel(b));

            await _settingsService.SaveBrowsersAsync(Browsers.Select(b => b.Browser));
            StatusMessage = $"{Browsers.Count} browser{(Browsers.Count == 1 ? "" : "s")} found";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settingsService.SaveBrowsersAsync(Browsers.Select(b => b.Browser));
        StatusMessage = "Saved";
    }

    private static List<Browser> MergeBrowsers(List<Browser> saved, List<Browser> detected)
    {
        foreach (var d in detected)
        {
            var existing = saved.FirstOrDefault(s =>
                string.Equals(s.ExecutablePath, d.ExecutablePath, StringComparison.OrdinalIgnoreCase));
            if (existing == null) saved.Add(d);
            else { existing.Version = d.Version; existing.Profiles = d.Profiles; }
        }
        return saved;
    }
}

public partial class BrowserEntryViewModel : ObservableObject
{
    public Browser Browser { get; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _displayName;

    public string Name => Browser.Name;
    public string Version => Browser.Version;
    public string ExecutablePath => Browser.ExecutablePath;
    public bool IsInstalled => Browser.IsInstalled;
    public int ProfileCount => Browser.Profiles.Count;
    public ImageSource? Icon => IconHelper.GetBrowserIcon(Browser.ExecutablePath);

    public BrowserEntryViewModel(Browser browser)
    {
        Browser = browser;
        _isEnabled = browser.IsEnabled;
        _displayName = browser.DisplayName;
    }

    partial void OnIsEnabledChanged(bool value) => Browser.IsEnabled = value;
    partial void OnDisplayNameChanged(string value) => Browser.DisplayName = value;
}
