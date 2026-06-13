// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using Microsoft.Win32;
using System.Windows;

namespace BrowserPickerPro.UI.Helpers;

public static class ThemeHelper
{
    public static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsDarkMode(AppTheme theme) => theme switch
    {
        AppTheme.Dark => true,
        AppTheme.Light => false,
        AppTheme.System => IsSystemDarkMode(),
        _ => false
    };

    public static void ApplyTheme(ResourceDictionary appResources, AppTheme theme)
    {
        var isDark = IsDarkMode(theme);
        var themeUri = new Uri(
            isDark ? "pack://application:,,,/Themes/Dark.xaml"
                   : "pack://application:,,,/Themes/Light.xaml");

        // Dark.xaml / Light.xaml live inside Theme.xaml's MergedDictionaries, not at the top level.
        var themeWrapper = appResources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.xaml") == true);

        var targetList = themeWrapper?.MergedDictionaries ?? appResources.MergedDictionaries;

        var existing = targetList
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Dark.xaml") == true ||
                                  d.Source?.OriginalString?.Contains("Light.xaml") == true);

        if (existing != null)
            targetList.Remove(existing);

        targetList.Insert(0, new ResourceDictionary { Source = themeUri });
    }
}
