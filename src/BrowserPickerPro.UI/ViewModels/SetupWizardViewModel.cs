// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BrowserPickerPro.UI.ViewModels;

public partial class SetupWizardViewModel : ObservableObject
{
    private readonly IProtocolRegistrar _registrar;
    private readonly ILogger<SetupWizardViewModel> _logger;

    [ObservableProperty] private bool _isRegistered;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SetupWizardViewModel(IProtocolRegistrar registrar, ILogger<SetupWizardViewModel> logger)
    {
        _registrar = registrar;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        IsRegistered = await _registrar.IsRegisteredAsync();
        StatusMessage = IsRegistered
            ? "Browser Picker Pro is registered as a browser handler."
            : "Not yet registered as the default browser handler.";
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        IsBusy = true;
        try
        {
            var success = await _registrar.RegisterAsync();
            IsRegistered = success;
            StatusMessage = success
                ? "Registered! Now open Windows Settings → Default Apps → Browser and select 'Browser Picker Pro'."
                : "Registration failed. Please try running as Administrator.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            StatusMessage = "Registration failed: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenDefaultApps()
    {
        Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task UnregisterAsync()
    {
        IsBusy = true;
        try
        {
            await _registrar.UnregisterAsync();
            IsRegistered = false;
            StatusMessage = "Unregistered successfully.";
        }
        finally { IsBusy = false; }
    }
}
