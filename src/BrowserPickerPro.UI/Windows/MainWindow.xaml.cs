// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.Services;
using BrowserPickerPro.UI.ViewModels;
using BrowserPickerPro.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace BrowserPickerPro.UI.Windows;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly NavigationService _navigation;

    public MainWindow(IServiceProvider services, NavigationService navigation, MainViewModel viewModel)
    {
        InitializeComponent();
        _services = services;
        _navigation = navigation;
        DataContext = viewModel;

        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var isDark = ThemeHelper.IsSystemDarkMode();
        MicaHelper.TryApplyMica(this, isDark);

        _navigation.Initialize(ContentFrame);
        RegisterPages();
        _navigation.NavigateTo(NavigationSection.Browsers);
        AnimationHelper.FadeIn(this, 300);
    }

    private void RegisterPages()
    {
        _navigation.RegisterPage(NavigationSection.Browsers, () => _services.GetRequiredService<BrowsersView>());
        _navigation.RegisterPage(NavigationSection.Rules, () => _services.GetRequiredService<RulesView>());
        _navigation.RegisterPage(NavigationSection.Security, () => _services.GetRequiredService<SecurityView>());
        _navigation.RegisterPage(NavigationSection.Appearance, () => _services.GetRequiredService<AppearanceView>());
        _navigation.RegisterPage(NavigationSection.ImportExport, () => _services.GetRequiredService<ImportExportView>());
        _navigation.RegisterPage(NavigationSection.About, () => _services.GetRequiredService<AboutView>());
    }

    private void NavItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag)
        {
            if (Enum.TryParse<NavigationSection>(tag, out var section))
                _navigation.NavigateTo(section);
            else if (tag == "Setup")
                NavigateToSetup();
        }
    }

    private void NavigateToSetup()
    {
        var setupVm = _services.GetRequiredService<SetupWizardViewModel>();
        var setupView = new SetupView { DataContext = setupVm };
        _ = setupVm.InitializeAsync();
        ContentFrame.Navigate(setupView);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var icon = WindowState == WindowState.Maximized ? "" : "";
        if (MaximizeButton?.Content is System.Windows.Controls.TextBlock tb)
            tb.Text = icon;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 && _navigation.CurrentSection == NavigationSection.Browsers)
        {
            if (ContentFrame.Content is BrowsersView bv && bv.DataContext is BrowsersViewModel vm)
                vm.RefreshCommand.Execute(null);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
