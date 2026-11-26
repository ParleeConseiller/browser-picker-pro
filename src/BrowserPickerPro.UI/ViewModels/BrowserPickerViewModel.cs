// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: BrowserPickerViewModel.cs
// Description: View model of the picker window. Since v2.0 it is long-lived: browsers are
//              loaded once (LoadBrowsersAsync) and every link only calls PrepareForRequest,
//              which resets the per-link state and replays the card entrance.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 2.0.0
using BrowserPickerPro.BrowserDetection.Services;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Rules.Services;
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
    // Card entrance stagger: the first card waits for the window reveal to get going,
    // then each following card comes in a little later.
    private const int EntranceBaseDelayMs = 140;
    private const int EntranceStepDelayMs = 65;
    // URLs longer than this are shortened in the URL bar.
    private const int MaxDisplayUrlLength = 80;

    private readonly ISettingsService _settingsService;
    private readonly IBrowserDetectionService _detectionService;
    private readonly IRulesEngine _rulesEngine;
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

    /// <summary>Raised when the user picks a browser (after usage stats / remember rule are saved).</summary>
    public event Action<Browser?, BrowserProfile?>? BrowserSelected;
    /// <summary>Raised after the post-pick bookkeeping is saved (legacy mode exits on this).</summary>
    public event Action? SelectionFinished;
    public event Action? Cancelled;

    private UrlRequest? _request;
    // Bumped on every new link so stale async work (security check, stagger) is dropped.
    private int _requestVersion;

    public BrowserPickerViewModel(
        ISettingsService settingsService,
        IBrowserDetectionService detectionService,
        IRulesEngine rulesEngine,
        SecurityOrchestrator security,
        ILogger<BrowserPickerViewModel> logger)
    {
        _settingsService = settingsService;
        _detectionService = detectionService;
        _rulesEngine = rulesEngine;
        _security = security;
        _logger = logger;
    }

    /// <summary>One-shot (legacy v1) initialisation: load + detect browsers, then prepare.</summary>
    public async Task InitializeAsync(UrlRequest request, AppSettings settings)
    {
        await LoadBrowsersAsync(detect: true);
        PrepareForRequest(request, settings);
    }

    /// <summary>
    /// (Re)loads the browser list. <paramref name="detect"/>=false uses only the saved list
    /// (milliseconds); true also scans disk + registry (slow) and saves the merged result,
    /// which also keeps profile ids stable for remembered rules.
    /// </summary>
    public async Task LoadBrowsersAsync(bool detect)
    {
        IsLoading = Browsers.Count == 0;

        var list = (await _settingsService.LoadBrowsersAsync()).ToList();
        if (detect || list.Count == 0)
        {
            // Detection does blocking filesystem/registry work: keep it off the UI thread.
            var detected = await Task.Run(() => _detectionService.DetectInstalledBrowsersAsync());
            list = BrowserListMerger.Merge(list, detected.ToList());
            await _settingsService.SaveBrowsersAsync(list);
        }

        var enabled = list.Where(b => b.IsEnabled && b.IsInstalled).OrderBy(b => b.OrderIndex).ToList();

        Browsers.Clear();
        // Cards start hidden (IsEntranceVisible=false); PrepareForRequest's stagger shows them.
        // Never create them already "visible": the entrance trigger would then fire while the
        // card template is still being instantiated, which WPF rejects. Refreshes only happen
        // while the picker is hidden (PickerHost defers them), so nothing visible disappears.
        foreach (var browser in enabled)
            Browsers.Add(new BrowserItemViewModel(browser));
        RebuildFilter();
        IsLoading = false;
    }

    /// <summary>Looks up a loaded browser by id (used by rule matching in the resident host).</summary>
    public Browser? FindBrowser(string id) => Browsers.FirstOrDefault(b => b.Browser.Id == id)?.Browser;

    /// <summary>Resets all per-link state for a new URL and replays the card entrance.</summary>
    public void PrepareForRequest(UrlRequest request, AppSettings settings)
    {
        var version = ++_requestVersion;
        _request = request;
        Settings = settings;
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(CardWidth));
        OnPropertyChanged(nameof(CardHeight));

        DisplayUrl = request.Url.Length > MaxDisplayUrlLength ? request.Url[..(MaxDisplayUrlLength - 3)] + "..." : request.Url;
        Domain = request.Domain ?? string.Empty;
        RememberChoice = false;
        SelectedBrowser = null;
        SecurityResult = null;
        IsSecurityChecking = false;
        ShowSecurityPanel = false;

        // Setting SearchText only rebuilds the filter when it actually changes.
        if (SearchText.Length > 0) SearchText = string.Empty;
        else RebuildFilter();

        foreach (var b in Browsers) b.IsEntranceVisible = false;
        _ = StaggerEntranceAsync(version);

        if (settings.EnableSecurityCheck && (request.IsUrl || request.Domain != null))
            _ = RunSecurityCheckAsync(request.Url, version);
    }

    partial void OnSearchTextChanged(string value) => RebuildFilter();

    private void RebuildFilter()
    {
        var query = SearchText.Trim();
        var matches = Browsers
            .Where(b => query.Length == 0 || b.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Unchanged list → leave the collection alone. Clear()+Add() would make WPF throw
        // away and rebuild every card on every link, which is the slowest part of a reveal.
        if (matches.SequenceEqual(FilteredBrowsers)) return;

        FilteredBrowsers.Clear();
        foreach (var b in matches) FilteredBrowsers.Add(b);
    }

    [RelayCommand]
    private async Task SelectBrowserAsync(BrowserItemViewModel? vm)
    {
        if (vm == null) return;
        SelectedBrowser = vm;

        // Fire the launch FIRST — the browser should start while we do bookkeeping.
        BrowserSelected?.Invoke(vm.Browser, vm.SelectedProfile);

        if (RememberChoice && !string.IsNullOrEmpty(_request?.Domain))
            await RememberDomainAsync(_request.Domain, vm);

        vm.Browser.LastUsed = DateTime.UtcNow;
        vm.Browser.UsageCount++;
        vm.RefreshUsage();
        try
        {
            await _settingsService.SaveBrowsersAsync(Browsers.Select(b => b.Browser));
        }
        finally
        {
            SelectionFinished?.Invoke();
        }
    }

    // "Remember for this domain": store/replace a domain rule targeting this browser+profile.
    private async Task RememberDomainAsync(string domain, BrowserItemViewModel vm)
    {
        try
        {
            var rules = await _rulesEngine.LoadRulesAsync();
            var updated = RememberChoiceRules.Upsert(rules, domain, vm.Browser.Id, vm.SelectedProfile?.Id);
            await _rulesEngine.SaveRulesAsync(updated);
            _logger.LogInformation("Remembered {Browser} for {Domain}", vm.DisplayName, domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save remembered rule for {Domain}", domain);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    // Runs on the UI thread (await resumes on the dispatcher), so no Dispatcher.Invoke needed.
    private async Task StaggerEntranceAsync(int version)
    {
        var items = FilteredBrowsers.ToList();
        for (int i = 0; i < items.Count; i++)
        {
            await Task.Delay(i == 0 ? EntranceBaseDelayMs : EntranceStepDelayMs);
            if (version != _requestVersion) return; // a newer link replaced this one
            items[i].IsEntranceVisible = true;
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

    private async Task RunSecurityCheckAsync(string url, int version)
    {
        IsSecurityChecking = true;
        ShowSecurityPanel = true;
        try
        {
            var result = await _security.CheckUrlAsync(url);
            if (version == _requestVersion) SecurityResult = result; // ignore stale results
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security check failed for {Url}", url);
        }
        finally
        {
            if (version == _requestVersion) IsSecurityChecking = false;
        }
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

    /// <summary>The view model is reused across links, so the usage badge must be refreshed.</summary>
    public void RefreshUsage()
    {
        OnPropertyChanged(nameof(UsageLabel));
        OnPropertyChanged(nameof(HasUsage));
    }
}

internal static class StringNullExtension
{
    public static bool IsNullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
}
