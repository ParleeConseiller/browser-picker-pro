// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: App.xaml.cs
// Description: Application entry. v2.0 runs as a single resident tray instance:
//              - a second start forwards its URL to the resident one over the pipe and exits;
//              - the resident instance pre-warms the picker and serves every link instantly.
//              KeepRunningInTray=false keeps the legacy v1 one-shot behaviour.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-26
// Version: 2.1.0
using System.Diagnostics;
using System.IO;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Ipc;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Infrastructure.Extensions;
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.Services;
using BrowserPickerPro.UI.ViewModels;
using BrowserPickerPro.UI.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace BrowserPickerPro.UI;

public partial class App : Application
{
    // How long a forwarding instance waits for the resident one (it may still be starting).
    private const int ForwardTimeoutMs = 5000;

    private IServiceProvider? _serviceProvider;
    private Mutex? _residentMutex;
    private IpcServer? _ipcServer;
    private TrayHost? _tray;
    private PickerHost? _pickerHost;
    private MainWindow? _dashboard;
    private ILogger<App>? _logger;

    /// <summary>Parsed command line: a URL/file, --tray, --settings, or nothing.</summary>
    private sealed record StartupArgs(string? Url, bool Tray, bool Settings)
    {
        public static StartupArgs Parse(string[] args)
        {
            var tray = args.Any(a => string.Equals(a, IpcProtocol.TraySwitch, StringComparison.OrdinalIgnoreCase));
            var settings = args.Any(a => string.Equals(a, IpcProtocol.SettingsSwitch, StringComparison.OrdinalIgnoreCase));
            var url = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
            return new StartupArgs(url, tray, settings);
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var started = Stopwatch.GetTimestamp();
        var args = StartupArgs.Parse(e.Args);

        // 0) Never become the resident (or one-shot) instance while elevated. Links come
        //    from non-elevated apps; their launcher can't open an elevated process's pipe or
        //    mutex, so every link was silently dropped (2026-11-26: the app had been started
        //    from an admin shell). Relaunch at the normal integrity level and get out.
        //    An elevated *forwarder* is fine (high → medium works), hence the resident check.
        string? deElevationFailure = null;
        if (!IpcProtocol.IsResidentRunning() && Elevation.IsCurrentProcessElevated())
        {
            if (Environment.ProcessPath is { } exe && Elevation.TryRelaunchUnelevated(exe, e.Args, out deElevationFailure))
            {
                Shutdown();
                return;
            }
            // Logged below once the logger exists; we keep going rather than drop the link.
        }

        // 1) Single instance FIRST, before any expensive setup: if a resident instance
        //    exists, hand it our request and get out of the way.
        _residentMutex = new Mutex(initiallyOwned: true, IpcProtocol.MutexName, out var isFirstInstance);
        var oneShotFallback = false;
        if (!isFirstInstance)
        {
            if (ForwardToResident(args, started) || args.Url is not { Length: > 0 })
            {
                Shutdown();
                return;
            }
            // The resident instance exists but can't be reached (e.g. it runs elevated):
            // show a one-shot picker ourselves instead of doing nothing at all.
            _residentMutex.Dispose();
            _residentMutex = null;
            oneShotFallback = true;
        }

        // 2) Services, settings, theme.
        _serviceProvider = BuildServices();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        if (deElevationFailure != null)
            _logger.LogWarning("Running elevated: could not relaunch unelevated ({Reason}); links from normal apps may not reach this instance", deElevationFailure);
        if (oneShotFallback)
            _logger.LogWarning("Resident instance unreachable (elevated or hung?) — showing a one-shot picker");

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = await settingsService.LoadAsync();
        _serviceProvider.GetRequiredService<ThemeService>().Apply(settings.Theme, Resources);

        if (oneShotFallback || !settings.KeepRunningInTray)
        {
            // Legacy one-shot mode: nobody else may forward to us.
            _residentMutex?.Dispose();
            _residentMutex = null;
            await RunLegacyAsync(args, settings);
            return;
        }

        await RunResidentAsync(args, settings, started);
    }

    // A resident app should survive one bad link, so isolated UI exceptions are swallowed.
    // But an exception thrown from layout/render repeats on EVERY frame: swallowing it forever
    // turned one bug into a 1 GB log during development. Burst → let the process die.
    private const int MaxUiErrorsPerWindow = 5;
    private static readonly TimeSpan UiErrorWindow = TimeSpan.FromSeconds(10);
    private readonly Queue<DateTime> _recentUiErrors = new();

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var now = DateTime.UtcNow;
        _recentUiErrors.Enqueue(now);
        while (_recentUiErrors.Count > 0 && now - _recentUiErrors.Peek() > UiErrorWindow) _recentUiErrors.Dequeue();

        if (_recentUiErrors.Count > MaxUiErrorsPerWindow)
        {
            _logger?.LogCritical(e.Exception, "Repeated UI exceptions — exiting instead of looping");
            return; // not handled → WPF terminates the process (Event Log 1026 has the details)
        }

        _logger?.LogError(e.Exception, "Unhandled UI exception");
        e.Handled = true;
    }

    private static IServiceProvider BuildServices()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrowserPickerPro", "Logs");

        var services = new ServiceCollection();
        services.AddBrowserPickerLogging(logDir);
        services.AddBrowserPickerServices();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<IpcServer>();
        services.AddSingleton<TrayHost>();
        services.AddSingleton<PickerHost>();

        services.AddTransient<MainViewModel>();
        // Singletons: the resident app keeps ONE warm picker window and its view model.
        services.AddSingleton<BrowserPickerViewModel>();
        services.AddSingleton<BrowserPickerWindow>();
        services.AddTransient<BrowsersViewModel>();
        services.AddTransient<RulesViewModel>();
        services.AddTransient<SecurityViewModel>();
        services.AddTransient<AppearanceViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<ImportExportViewModel>();
        services.AddTransient<SetupWizardViewModel>();

        services.AddTransient<MainWindow>(sp => new MainWindow(
            sp, sp.GetRequiredService<NavigationService>(), sp.GetRequiredService<MainViewModel>()));

        services.AddTransient<Views.BrowsersView>();
        services.AddTransient<Views.RulesView>();
        services.AddTransient<Views.SecurityView>();
        services.AddTransient<Views.AppearanceView>();
        services.AddTransient<Views.ImportExportView>();
        services.AddTransient<Views.AboutView>();

        return services.BuildServiceProvider();
    }

    // Second instance (main exe started directly while resident): forward and exit.
    // Returns false when the resident instance could not be reached.
    private static bool ForwardToResident(StartupArgs args, long started)
    {
        // We were started by the user, so we may pass the foreground right on.
        NativeMethods.AllowSetForegroundWindow(NativeMethods.AsfwAny);
        var message = args.Url is { Length: > 0 }
            ? new IpcMessage(IpcProtocol.CommandOpen, started, args.Url)
            : new IpcMessage(IpcProtocol.CommandSettings, started, string.Empty);
        // A bare "--tray" start (e.g. Run key while already running) has nothing to forward.
        if (args.Tray && args.Url is null) return true;
        return IpcProtocol.TrySend(message, ForwardTimeoutMs);
    }

    // ── Resident (tray) mode ─────────────────────────────────────────────────

    private async Task RunResidentAsync(StartupArgs args, AppSettings settings, long started)
    {
        var sp = _serviceProvider!;

        // Listen before the slow parts so early launchers don't time out (they queue).
        _ipcServer = sp.GetRequiredService<IpcServer>();
        _ipcServer.MessageReceived += message => Dispatcher.InvokeAsync(() => OnIpcMessage(message));
        _ipcServer.Start();

        _tray = sp.GetRequiredService<TrayHost>();
        _tray.SettingsRequested += ShowDashboard;
        _tray.RefreshRequested += () => _ = _pickerHost?.RefreshBrowsersAsync();
        _tray.ExitRequested += ExitApplication;
        _tray.Show(settings.StartWithWindows);

        await KeepRegistrationsCurrentAsync(sp, settings);

        _pickerHost = sp.GetRequiredService<PickerHost>();
        var init = _pickerHost.InitializeAsync(settings);

        if (args.Url is { Length: > 0 })
            await _pickerHost.HandleUrlAsync(args.Url, started);   // waits for pre-warm internally
        else if (!args.Tray)
            ShowDashboard();                                       // user opened the app itself

        await init;
    }

    private void OnIpcMessage(IpcMessage message)
    {
        if (message.Command == IpcProtocol.CommandSettings) { ShowDashboard(); return; }
        _ = _pickerHost?.HandleUrlAsync(message.Argument, message.Timestamp);
    }

    // Moving the exe, or upgrading from v1 (handler = main exe), must not leave stale paths.
    // Must be awaited, never .GetResult(): IsRegisteredAsync resumes on the UI thread, so
    // blocking that thread on it deadlocks the whole app (it did, in the first 2.0 test run).
    private async Task KeepRegistrationsCurrentAsync(IServiceProvider sp, AppSettings settings)
    {
        try
        {
            var startup = sp.GetRequiredService<IStartupRegistrar>();
            if (settings.StartWithWindows && !startup.IsEnabled()) startup.Enable();

            var protocol = sp.GetRequiredService<IProtocolRegistrar>();
            // Only refresh an existing registration — registering is the user's choice (Setup page).
            if (await protocol.IsRegisteredAsync())
                await protocol.RegisterAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not refresh registrations");
        }
    }

    /// <summary>Opens (or focuses) the configuration dashboard.</summary>
    public void ShowDashboard()
    {
        if (_dashboard != null)
        {
            if (_dashboard.WindowState == WindowState.Minimized) _dashboard.WindowState = WindowState.Normal;
            _dashboard.Activate();
            return;
        }

        _dashboard = _serviceProvider!.GetRequiredService<MainWindow>();
        _dashboard.Closed += (_, _) =>
        {
            _dashboard = null;
            // Browsers may have been added/disabled/reordered: refresh the warm picker.
            _ = _pickerHost?.RefreshBrowsersAsync();
        };
        _dashboard.Show();
        _dashboard.Activate();
    }

    private void ExitApplication()
    {
        _dashboard?.Close();
        _pickerHost?.Shutdown();
        Shutdown();
    }

    // ── Legacy one-shot mode (KeepRunningInTray = false) ─────────────────────

    private async Task RunLegacyAsync(StartupArgs args, AppSettings settings)
    {
        if (args.Url is { Length: > 0 })
        {
            await LaunchBrowserPickerAsync(args.Url, settings);
            return;
        }

        var mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
        // ShutdownMode is OnExplicitShutdown, so closing the dashboard must end the process
        // (v1 left an invisible zombie process behind here).
        mainWindow.Closed += (_, _) => Shutdown();
        mainWindow.Show();
        MainWindow = mainWindow;
    }

    private async Task LaunchBrowserPickerAsync(string argument, AppSettings settings)
    {
        var sp = _serviceProvider!;
        var request = UrlRequest.Parse(argument);
        var rulesEngine = sp.GetRequiredService<IRulesEngine>();
        var rules = await rulesEngine.LoadRulesAsync();
        var matchedRule = rulesEngine.FindMatchingRule(request, rules);
        var launcher = sp.GetRequiredService<IBrowserLauncher>();

        if (matchedRule?.Action == Core.Enums.RuleAction.OpenInBrowser && matchedRule.TargetBrowserId != null)
        {
            var browsers = await sp.GetRequiredService<ISettingsService>().LoadBrowsersAsync();
            var browser = browsers.FirstOrDefault(b => b.Id == matchedRule.TargetBrowserId);
            if (browser != null)
            {
                var profile = browser.Profiles.FirstOrDefault(p => p.Id == matchedRule.TargetProfileId);
                await launcher.LaunchAsync(browser, request.Url, profile);
                Shutdown();
                return;
            }
        }

        var pickerWindow = sp.GetRequiredService<BrowserPickerWindow>();
        var vm = sp.GetRequiredService<BrowserPickerViewModel>();
        await vm.InitializeAsync(request, settings);
        pickerWindow.DataContext = vm;
        pickerWindow.ApplySettings(settings);
        MainWindow = pickerWindow;

        vm.BrowserSelected += async (browser, profile) =>
        {
            if (browser != null) await launcher.LaunchAsync(browser, request.Url, profile);
        };
        // Exit only after the usage stats / remembered rule are written.
        vm.SelectionFinished += () => { pickerWindow.Close(); Shutdown(); };
        vm.Cancelled += () => { pickerWindow.Close(); Shutdown(); };

        pickerWindow.Reveal();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _ipcServer?.Dispose();
        _residentMutex?.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        base.OnExit(e);
    }
}
