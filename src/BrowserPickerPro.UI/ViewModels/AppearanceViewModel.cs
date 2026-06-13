// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrowserPickerPro.UI.ViewModels;

public partial class AppearanceViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ThemeService _themeService;

    [ObservableProperty] private AppTheme _selectedTheme;
    [ObservableProperty] private bool _enableAnimations;
    [ObservableProperty] private bool _enableMicaEffect;
    [ObservableProperty] private bool _showBrowserVersion;
    [ObservableProperty] private bool _showRecentUsage;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public IEnumerable<AppTheme> Themes => Enum.GetValues<AppTheme>();

    public AppearanceViewModel(ISettingsService settingsService, ThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync();
        SelectedTheme = settings.Theme;
        EnableAnimations = settings.EnableAnimations;
        EnableMicaEffect = settings.EnableMicaEffect;
        ShowBrowserVersion = settings.ShowBrowserVersion;
        ShowRecentUsage = settings.ShowRecentUsage;
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _themeService.Apply(value, System.Windows.Application.Current.Resources);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = await _settingsService.LoadAsync();
        settings.Theme = SelectedTheme;
        settings.EnableAnimations = EnableAnimations;
        settings.EnableMicaEffect = EnableMicaEffect;
        settings.ShowBrowserVersion = ShowBrowserVersion;
        settings.ShowRecentUsage = ShowRecentUsage;
        await _settingsService.SaveAsync(settings);
        StatusMessage = "Appearance settings saved";
    }
}
