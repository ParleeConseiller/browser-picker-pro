// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.UI.Helpers;
using System.Windows;

namespace BrowserPickerPro.UI.Services;

public class ThemeService
{
    private AppTheme _currentTheme = AppTheme.System;
    public event EventHandler<AppTheme>? ThemeChanged;

    public AppTheme CurrentTheme => _currentTheme;
    public bool IsDark => ThemeHelper.IsDarkMode(_currentTheme);

    public void Apply(AppTheme theme, ResourceDictionary appResources)
    {
        _currentTheme = theme;
        ThemeHelper.ApplyTheme(appResources, theme);
        ThemeChanged?.Invoke(this, theme);
    }
}
