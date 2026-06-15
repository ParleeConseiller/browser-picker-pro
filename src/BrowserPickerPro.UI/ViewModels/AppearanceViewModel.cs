// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BrowserPickerPro.UI.ViewModels;

public partial class AppearanceViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ThemeService _themeService;

    // Theme
    [ObservableProperty] private AppTheme _selectedTheme;
    [ObservableProperty] private bool _enableAnimations;
    [ObservableProperty] private bool _enableMicaEffect;
    [ObservableProperty] private bool _showBrowserVersion;
    [ObservableProperty] private bool _showRecentUsage;

    // Picker window
    [ObservableProperty] private PickerWindowStyle _pickerWindowStyle;
    [ObservableProperty] private PickerCardLayout _pickerCardLayout;
    [ObservableProperty] private PickerBackgroundMode _pickerBackgroundMode;
    [ObservableProperty] private string _pickerBackgroundImagePath = string.Empty;
    [ObservableProperty] private int _pickerDarkTintPercent = 55;
    [ObservableProperty] private int _pickerCardSize = 190;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool IsCustomFileMode => PickerBackgroundMode == PickerBackgroundMode.CustomFile;

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

        PickerWindowStyle = settings.PickerWindowStyle;
        PickerCardLayout = settings.PickerCardLayout;
        PickerBackgroundMode = settings.PickerBackgroundMode;
        PickerBackgroundImagePath = settings.PickerBackgroundImagePath ?? string.Empty;
        PickerDarkTintPercent = (int)Math.Round(settings.PickerDarkTintOpacity * 100);
        PickerCardSize = settings.PickerCardSize;
    }

    partial void OnSelectedThemeChanged(AppTheme value)
        => _themeService.Apply(value, System.Windows.Application.Current.Resources);

    partial void OnPickerBackgroundModeChanged(PickerBackgroundMode value)
        => OnPropertyChanged(nameof(IsCustomFileMode));

    [RelayCommand]
    private void BrowseBackground()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select background image or video",
            Filter = "Images & video|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.webm|All files|*.*"
        };
        if (dialog.ShowDialog() == true)
            PickerBackgroundImagePath = dialog.FileName;
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

        settings.PickerWindowStyle = PickerWindowStyle;
        settings.PickerCardLayout = PickerCardLayout;
        settings.PickerBackgroundMode = PickerBackgroundMode;
        settings.PickerBackgroundImagePath = string.IsNullOrWhiteSpace(PickerBackgroundImagePath) ? null : PickerBackgroundImagePath;
        settings.PickerDarkTintOpacity = PickerDarkTintPercent / 100.0;
        settings.PickerCardSize = PickerCardSize;

        await _settingsService.SaveAsync(settings);
        StatusMessage = "Appearance settings saved";
    }
}
