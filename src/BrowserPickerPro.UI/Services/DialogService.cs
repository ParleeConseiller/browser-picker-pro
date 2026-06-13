// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using Microsoft.Win32;
using System.Windows;

namespace BrowserPickerPro.UI.Services;

public class DialogService
{
    public string? ShowSaveFileDialog(string title, string filter, string defaultExt)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            DefaultExt = defaultExt
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowOpenFileDialog(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool ShowConfirmation(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public void ShowInfo(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
