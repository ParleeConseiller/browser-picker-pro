// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using System.IO;
using BrowserPickerPro.Core.Interfaces;
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
    private IServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrowserPickerPro", "Logs");

        var services = new ServiceCollection();
        services.AddBrowserPickerLogging(logDir);
        services.AddBrowserPickerServices();

        services.AddSingleton<NavigationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<DialogService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<BrowserPickerViewModel>();
        services.AddTransient<BrowsersViewModel>();
        services.AddTransient<RulesViewModel>();
        services.AddTransient<SecurityViewModel>();
        services.AddTransient<AppearanceViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<ImportExportViewModel>();
        services.AddTransient<SetupWizardViewModel>();

        services.AddTransient<MainWindow>(sp => new MainWindow(
            sp, sp.GetRequiredService<NavigationService>(), sp.GetRequiredService<MainViewModel>()));
        services.AddTransient<BrowserPickerWindow>();

        services.AddTransient<Views.BrowsersView>();
        services.AddTransient<Views.RulesView>();
        services.AddTransient<Views.SecurityView>();
        services.AddTransient<Views.AppearanceView>();
        services.AddTransient<Views.ImportExportView>();
        services.AddTransient<Views.AboutView>();

        _serviceProvider = services.BuildServiceProvider();

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = await settingsService.LoadAsync();

        var themeService = _serviceProvider.GetRequiredService<ThemeService>();
        themeService.Apply(settings.Theme, Resources);

        if (e.Args.Length > 0)
        {
            await LaunchBrowserPickerAsync(e.Args[0], settings);
        }
        else
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            MainWindow = mainWindow;
        }
    }

    private async Task LaunchBrowserPickerAsync(string argument, AppSettings settings)
    {
        var request = UrlRequest.Parse(argument);
        var rulesEngine = _serviceProvider!.GetRequiredService<IRulesEngine>();
        var rules = await rulesEngine.LoadRulesAsync();

        var matchedRule = rulesEngine.FindMatchingRule(request, rules);

        if (matchedRule?.Action == Core.Enums.RuleAction.OpenInBrowser && matchedRule.TargetBrowserId != null)
        {
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            var browsers = await settingsService.LoadBrowsersAsync();
            var browser = browsers.FirstOrDefault(b => b.Id == matchedRule.TargetBrowserId);

            if (browser != null)
            {
                var launcher = _serviceProvider.GetRequiredService<IBrowserLauncher>();
                await launcher.LaunchAsync(browser, request.Url);
                Shutdown();
                return;
            }
        }

        var pickerWindow = _serviceProvider.GetRequiredService<BrowserPickerWindow>();
        var vm = _serviceProvider.GetRequiredService<BrowserPickerViewModel>();
        await vm.InitializeAsync(request, settings);
        pickerWindow.DataContext = vm;
        pickerWindow.Show();
        MainWindow = pickerWindow;
        vm.BrowserSelected += async (browser, profile) =>
        {
            if (browser != null)
            {
                var launcher = _serviceProvider.GetRequiredService<IBrowserLauncher>();
                await launcher.LaunchAsync(browser, request.Url, profile);
            }
            pickerWindow.Close();
            Shutdown();
        };
        vm.Cancelled += () =>
        {
            pickerWindow.Close();
            Shutdown();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
        base.OnExit(e);
    }
}
