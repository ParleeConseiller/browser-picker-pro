// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Security.Services;
using BrowserPickerPro.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace BrowserPickerPro.UI.ViewModels;

public partial class BrowserPickerViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IBrowserDetectionService _detectionService;
    private readonly SecurityOrchestrator _security;
    private readonly ILogger<BrowserPickerViewModel> _logger;

    [ObservableProperty] private string _displayUrl = string.Empty;
    [ObservableProperty] private string _domain = string.Empty;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isSecurityChecking;
    [ObservableProperty] private SecurityResult? _securityResult;
    [ObservableProperty] private bool _rememberChoice;
    [ObservableProperty] private BrowserItemViewModel? _selectedBrowser;
    [ObservableProperty] private bool _showSecurityPanel;
    [ObservableProperty] private string _searchText = string.Empty;

    public ObservableCollection<BrowserItemViewModel> Browsers { get; } = new();
    public ObservableCollection<BrowserItemViewModel> FilteredBrowsers { get; } = new();

    public AppSettings? Settings { get; private set; }
    public int CardWidth => Settings?.PickerCardSize ?? 190;
    public int CardHeight => (Settings?.PickerCardSize ?? 190) + 24;

    public event Action<Browser?, BrowserProfile?>? BrowserSelected;
    public event Action? Cancelled;

    private UrlRequest? _request;
    private AppSettings? _settings;

    public BrowserPickerViewModel(
        ISettingsService settingsService,
        IBrowserDetectionService detectionService,
        SecurityOrchestrator security,
        ILogger<BrowserPickerViewModel> logger)
    {
        _settingsService = settingsService;
        _detectionService = detectionService;
        _security = security;
        _logger = logger;
    }

    public async Task InitializeAsync(UrlRequest request, AppSettings settings)
    {
        _request = request;
        _settings = settings;
        Settings = settings;
        DisplayUrl = request.Url.Length > 80 ? request.Url[..77] + "..." : request.Url;
        Domain = request.Domain ?? string.Empty;

        IsLoading = true;

        var saved = await _settingsService.LoadBrowsersAsync();
        var detected = await _detectionService.DetectInstalledBrowsersAsync();

        var merged = MergeBrowsers(saved.ToList(), detected.ToList());
        var enabled = merged.Where(b => b.IsEnabled && b.IsInstalled).OrderBy(b => b.OrderIndex).ToList();

        Browsers.Clear();
        FilteredBrowsers.Clear();
        foreach (var browser in enabled)
        {
            var vm = new BrowserItemViewModel(browser);
            Browsers.Add(vm);
            FilteredBrowsers.Add(vm);
        }

        IsLoading = false;
        TriggerEntranceAnimations();

        if (settings.EnableSecurityCheck && (request.IsUrl || request.Domain != null))
            await RunSecurityCheckAsync(request.Url);
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredBrowsers.Clear();
        var query = value.Trim().ToLowerInvariant();
        foreach (var b in Browsers)
        {
            if (string.IsNullOrEmpty(query) || b.DisplayName.ToLowerInvariant().Contains(query))
                FilteredBrowsers.Add(b);
        }
    }

    [RelayCommand]
    private async Task SelectBrowserAsync(BrowserItemViewModel? vm)
    {
        if (vm == null) return;
        SelectedBrowser = vm;

        if (RememberChoice && _request?.Domain != null)
        {
            // Save domain rule
        }

        vm.Browser.LastUsed = DateTime.UtcNow;
        vm.Browser.UsageCount++;
        await _settingsService.SaveBrowsersAsync(Browsers.Select(b => b.Browser));

        BrowserSelected?.Invoke(vm.Browser, vm.SelectedProfile);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    private void TriggerEntranceAnimations()
    {
        for (int i = 0; i < FilteredBrowsers.Count; i++)
        {
            var index = i;
            var item = FilteredBrowsers[i];
            Task.Run(async () =>
            {
                await Task.Delay(80 + index * 65);
                System.Windows.Application.Current.Dispatcher.Invoke(() => item.IsEntranceVisible = true);
            });
        }
    }

    public void MoveItem(BrowserItemViewModel source, BrowserItemViewModel target)
    {
        var fromIndex = Browsers.IndexOf(source);
        var toIndex = Browsers.IndexOf(target);
        if (fromIndex < 0 || toIndex < 0) return;

        Browsers.Move(fromIndex, toIndex);

        var filtFrom = FilteredBrowsers.IndexOf(source);
        var filtTo = FilteredBrowsers.IndexOf(target);
        if (filtFrom >= 0 && filtTo >= 0)
            FilteredBrowsers.Move(filtFrom, filtTo);

        for (int i = 0; i < Browsers.Count; i++)
            Browsers[i].Browser.OrderIndex = i;

        _ = _settingsService.SaveBrowsersAsync(Browsers.Select(b => b.Browser));
    }

    private async Task RunSecurityCheckAsync(string url)
    {
        IsSecurityChecking = true;
        ShowSecurityPanel = true;
        try
        {
            SecurityResult = await _security.CheckUrlAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security check failed for {Url}", url);
        }
        finally
        {
            IsSecurityChecking = false;
        }
    }

    private static List<Browser> MergeBrowsers(List<Browser> saved, List<Browser> detected)
    {
        if (!saved.Any()) return detected;

        foreach (var d in detected)
        {
            var existing = saved.FirstOrDefault(s =>
                string.Equals(s.ExecutablePath, d.ExecutablePath, StringComparison.OrdinalIgnoreCase));
            if (existing == null) saved.Add(d);
            else
            {
                existing.Version = d.Version;
                existing.Profiles = d.Profiles;
            }
        }
        return saved;
    }
}

public partial class BrowserItemViewModel : ObservableObject
{
    public Browser Browser { get; }

    [ObservableProperty] private bool _isHovered;
    [ObservableProperty] private bool _isEntranceVisible;
    [ObservableProperty] private BrowserProfile? _selectedProfile;

    public string DisplayName => Browser.DisplayName.IsNullOrEmpty() ? Browser.Name : Browser.DisplayName;
    public string Version => Browser.Version;
    public string ShortVersion => Browser.Version.Split('.').FirstOrDefault() ?? Browser.Version;
    public bool HasProfiles => Browser.Profiles.Count > 1;
    public ImageSource? Icon => IconHelper.GetBrowserIcon(Browser.ExecutablePath);
    public string UsageLabel => Browser.UsageCount > 0 ? $"Used {Browser.UsageCount}×" : string.Empty;
    public bool HasUsage => Browser.UsageCount > 0;
    public List<BrowserProfile> Profiles => Browser.Profiles;

    public BrowserItemViewModel(Browser browser)
    {
        Browser = browser;
        SelectedProfile = browser.Profiles.FirstOrDefault();
    }
}

internal static class StringNullExtension
{
    public static bool IsNullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
}
