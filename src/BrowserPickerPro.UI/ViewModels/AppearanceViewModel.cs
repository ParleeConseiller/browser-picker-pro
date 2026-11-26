// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: AppearanceViewModel.cs
// Description: Settings → Appearance: theme, picker window style/background, opening
//              animation (with live preview), and tray/start-with-Windows behaviour.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 2.0.0
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BrowserPickerPro.UI.ViewModels;

/// <summary>One choice in the "Opening animation" list.</summary>
public sealed record AnimationOption(PickerOpenAnimation Value, string Label, string Description);

public partial class AppearanceViewModel : ObservableObject
{
    // URL shown in the picker when previewing the opening animation (rules are bypassed).
    private const string PreviewUrl = "https://example.com/preview";

    private readonly ISettingsService _settingsService;
    private readonly ThemeService _themeService;
    private readonly IStartupRegistrar _startup;
    private readonly PickerHost _pickerHost;

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
    [ObservableProperty] private AnimationOption? _selectedAnimation;

    // Tray / startup
    [ObservableProperty] private bool _keepRunningInTray = true;
    [ObservableProperty] private bool _startWithWindows = true;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool IsCustomFileMode => PickerBackgroundMode == PickerBackgroundMode.CustomFile;

    /// <summary>Preview needs the resident picker (not available in legacy mode).</summary>
    public bool CanPreview => _pickerHost.IsActive;

    public IEnumerable<AppTheme> Themes => Enum.GetValues<AppTheme>();

    public IReadOnlyList<AnimationOption> Animations { get; } = new[]
    {
        new AnimationOption(PickerOpenAnimation.CircleReveal, "Circle reveal", "A glowing ring expands from the cursor."),
        new AnimationOption(PickerOpenAnimation.ZoomBlur,     "Zoom & focus",  "Zooms in while a blur sharpens into focus."),
        new AnimationOption(PickerOpenAnimation.SpringRise,   "Spring rise",   "Rises from below and settles with a bounce."),
        new AnimationOption(PickerOpenAnimation.PowerOn,      "CRT power-on",  "An old TV switching on: a bright line opens up."),
        new AnimationOption(PickerOpenAnimation.Random,       "Surprise me",   "A different animation every time."),
        new AnimationOption(PickerOpenAnimation.None,         "None",          "Appears instantly."),
    };

    public AppearanceViewModel(ISettingsService settingsService, ThemeService themeService,
                               IStartupRegistrar startup, PickerHost pickerHost)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _startup = startup;
        _pickerHost = pickerHost;
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
        SelectedAnimation = Animations.FirstOrDefault(a => a.Value == settings.PickerOpenAnimation) ?? Animations[0];

        KeepRunningInTray = settings.KeepRunningInTray;
        StartWithWindows = settings.StartWithWindows;
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
            Filter = "Images & video|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.webm;*.wmv;*.mov;*.mkv|All files|*.*"
        };
        if (dialog.ShowDialog() == true)
            PickerBackgroundImagePath = dialog.FileName;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await PersistAsync();
        StatusMessage = "Appearance settings saved";
    }

    /// <summary>Saves, then opens the real picker so the animation and background can be judged.</summary>
    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (!_pickerHost.IsActive) return;
        await PersistAsync();
        StatusMessage = "Previewing — press Esc to close the picker";
        await _pickerHost.HandleUrlAsync(PreviewUrl, launcherTimestamp: 0, bypassRules: true);
    }

    private async Task PersistAsync()
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
        settings.PickerOpenAnimation = SelectedAnimation?.Value ?? PickerOpenAnimation.CircleReveal;

        settings.KeepRunningInTray = KeepRunningInTray;
        settings.StartWithWindows = StartWithWindows;
        // Start-with-Windows only makes sense for the resident app.
        if (StartWithWindows && KeepRunningInTray) _startup.Enable(); else _startup.Disable();

        await _settingsService.SaveAsync(settings);
    }
}
