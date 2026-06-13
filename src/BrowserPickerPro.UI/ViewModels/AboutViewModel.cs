// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;

namespace BrowserPickerPro.UI.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppName => "Browser Picker Pro";
    public string Tagline => "Choose the right browser every time.";
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    public string Company => "Parlee Conseiller, Inc.";
    public string Developer => "Jose-Jorge HERNANDEZ";
    public string Copyright => $"© 2025 Parlee Conseiller, Inc. All rights reserved.";
    public string Framework => $".NET {Environment.Version}";
    public string Platform => Environment.OSVersion.VersionString;

    [RelayCommand]
    private void OpenGitHub() => Process.Start(new ProcessStartInfo("https://github.com/parleeconseiller/browser-picker-pro") { UseShellExecute = true });

    [RelayCommand]
    private void OpenLicense() => Process.Start(new ProcessStartInfo("https://opensource.org/licenses/MIT") { UseShellExecute = true });
}
