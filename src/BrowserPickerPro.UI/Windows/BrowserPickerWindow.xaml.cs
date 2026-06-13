// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.UI.Helpers;
using BrowserPickerPro.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace BrowserPickerPro.UI.Windows;

public partial class BrowserPickerWindow : Window
{
    public BrowserPickerWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var isDark = Helpers.ThemeHelper.IsSystemDarkMode();
        MicaHelper.TryApplyAcrylic(this, isDark);
        AnimationHelper.FadeIn(this, 200);
        SearchBox.Focus();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is BrowserPickerViewModel vm)
                vm.CancelCommand.Execute(null);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrowserPickerViewModel vm)
            vm.CancelCommand.Execute(null);
        else
            Close();
    }
}
