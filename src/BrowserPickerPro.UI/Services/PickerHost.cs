// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: PickerHost.cs
// Description: Owns the single, long-lived picker window + view model of the resident
//              (tray) app. Handles each incoming link: rule match → launch directly,
//              otherwise prepare the warm window and reveal it; after the pick, hide again.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.UI.ViewModels;
using BrowserPickerPro.UI.Windows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BrowserPickerPro.UI.Services;

public sealed class PickerHost
{
    private readonly ISettingsService _settingsService;
    private readonly IRulesEngine _rulesEngine;
    private readonly IBrowserLauncher _launcher;
    private readonly BrowserPickerViewModel _vm;
    private readonly BrowserPickerWindow _window;
    private readonly ILogger<PickerHost> _logger;

    // Completes once the window is pre-warmed; links arriving earlier wait for it.
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private UrlRequest? _current;
    private bool _refreshPending;

    public PickerHost(ISettingsService settingsService, IRulesEngine rulesEngine, IBrowserLauncher launcher,
                      BrowserPickerViewModel vm, BrowserPickerWindow window, ILogger<PickerHost> logger)
    {
        _settingsService = settingsService;
        _rulesEngine = rulesEngine;
        _launcher = launcher;
        _vm = vm;
        _window = window;
        _logger = logger;
    }

    /// <summary>True once <see cref="InitializeAsync"/> ran (resident mode only).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Loads the saved browsers, pre-warms the window/video, then detects browsers in the background.</summary>
    public async Task InitializeAsync(AppSettings settings)
    {
        IsActive = true;
        _window.IsResident = true;
        _window.DataContext = _vm;
        _vm.BrowserSelected += OnBrowserSelected;
        _vm.Cancelled += OnCancelled;

        // Saved list only: milliseconds. The slow disk/registry scan runs after pre-warm.
        await _vm.LoadBrowsersAsync(detect: false);
        _window.ApplySettings(settings);
        await _window.PrewarmAsync();
        _ready.TrySetResult();
        _logger.LogInformation("Picker pre-warmed and resident");

        await RefreshBrowsersAsync();
    }

    /// <summary>Handles one link from the pipe (or the command line of the first instance).</summary>
    public async Task HandleUrlAsync(string argument, long launcherTimestamp, bool bypassRules = false)
    {
        await _ready.Task;
        var received = Stopwatch.GetTimestamp();

        var request = UrlRequest.Parse(argument);
        // Reading settings.json is ~1 ms and picks up changes made in the dashboard.
        var settings = await _settingsService.LoadAsync();

        if (!bypassRules && await TryLaunchByRuleAsync(request)) return;
        var decided = Stopwatch.GetTimestamp();

        _current = request;
        _window.ApplySettings(settings);
        _vm.PrepareForRequest(request, settings);
        var prepared = Stopwatch.GetTimestamp();
        _window.Reveal(launcherTimestamp);

        // Where the time goes, for tuning: pipe hop, settings+rules, view-model reset, Show().
        _logger.LogDebug(
            "Reveal timing: launcher→host {Hop:F1} ms, settings+rules {Rules:F1} ms, prepare {Prep:F1} ms, show {Show:F1} ms",
            launcherTimestamp > 0 ? Stopwatch.GetElapsedTime(launcherTimestamp, received).TotalMilliseconds : 0,
            Stopwatch.GetElapsedTime(received, decided).TotalMilliseconds,
            Stopwatch.GetElapsedTime(decided, prepared).TotalMilliseconds,
            Stopwatch.GetElapsedTime(prepared).TotalMilliseconds);
    }

    /// <summary>Re-detects installed browsers; deferred while the picker is on screen.</summary>
    public async Task RefreshBrowsersAsync()
    {
        if (!IsActive) return;
        // IsRevealed, not IsVisible: the resident window is always "visible" (parked off-screen).
        if (_window.IsRevealed) { _refreshPending = true; return; }
        try
        {
            await _vm.LoadBrowsersAsync(detect: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Browser refresh failed");
        }
    }

    /// <summary>Closes the window for good (application exit).</summary>
    public void Shutdown()
    {
        if (IsActive) _window.CloseForExit();
    }

    // An OpenInBrowser rule for this URL launches straight away — no window at all.
    private async Task<bool> TryLaunchByRuleAsync(UrlRequest request)
    {
        var rules = await _rulesEngine.LoadRulesAsync();
        var rule = _rulesEngine.FindMatchingRule(request, rules);
        if (rule?.Action != RuleAction.OpenInBrowser || rule.TargetBrowserId is null) return false;

        var browser = _vm.FindBrowser(rule.TargetBrowserId);
        if (browser is null) return false;

        var profile = rule.TargetProfileId is null ? null : browser.Profiles.FirstOrDefault(p => p.Id == rule.TargetProfileId);
        await LaunchSafeAsync(browser, request.Url, profile);
        return true;
    }

    private async void OnBrowserSelected(Browser? browser, BrowserProfile? profile)
    {
        // Launch first, hide second: the browser window should be on its way immediately.
        if (browser != null && _current != null) await LaunchSafeAsync(browser, _current.Url, profile);
        // The new browser window takes the focus; don't hand it back to the old app.
        HideAndCatchUp(restoreFocus: false);
    }

    private void OnCancelled() => HideAndCatchUp(restoreFocus: true);

    private void HideAndCatchUp(bool restoreFocus)
    {
        _window.Conceal(restoreFocus);
        if (_refreshPending)
        {
            _refreshPending = false;
            _ = RefreshBrowsersAsync();
        }
    }

    private async Task LaunchSafeAsync(Browser browser, string url, BrowserProfile? profile)
    {
        try
        {
            await _launcher.LaunchAsync(browser, url, profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch {Browser}", browser.DisplayName);
        }
    }
}
