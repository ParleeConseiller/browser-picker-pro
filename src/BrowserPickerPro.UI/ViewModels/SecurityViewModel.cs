// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrowserPickerPro.UI.ViewModels;

public partial class SecurityViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private bool _enableSecurityCheck;
    [ObservableProperty] private bool _enableVirusTotal;
    [ObservableProperty] private string _virusTotalApiKey = string.Empty;
    [ObservableProperty] private bool _enableGoogleSafeBrowsing;
    [ObservableProperty] private string _googleApiKey = string.Empty;
    [ObservableProperty] private bool _blockDangerous;
    [ObservableProperty] private bool _warnSuspicious = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isTestingUrl;
    [ObservableProperty] private string _testUrl = string.Empty;

    public SecurityViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync();
        EnableSecurityCheck = settings.EnableSecurityCheck;
        EnableVirusTotal = settings.Security.EnableVirusTotal;
        VirusTotalApiKey = settings.Security.VirusTotalApiKey ?? string.Empty;
        EnableGoogleSafeBrowsing = settings.Security.EnableGoogleSafeBrowsing;
        GoogleApiKey = settings.Security.GoogleSafeBrowsingApiKey ?? string.Empty;
        BlockDangerous = settings.Security.BlockDangerous;
        WarnSuspicious = settings.Security.WarnSuspicious;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = await _settingsService.LoadAsync();
        settings.EnableSecurityCheck = EnableSecurityCheck;
        settings.Security.EnableVirusTotal = EnableVirusTotal;
        settings.Security.VirusTotalApiKey = string.IsNullOrWhiteSpace(VirusTotalApiKey) ? null : VirusTotalApiKey;
        settings.Security.EnableGoogleSafeBrowsing = EnableGoogleSafeBrowsing;
        settings.Security.GoogleSafeBrowsingApiKey = string.IsNullOrWhiteSpace(GoogleApiKey) ? null : GoogleApiKey;
        settings.Security.BlockDangerous = BlockDangerous;
        settings.Security.WarnSuspicious = WarnSuspicious;
        await _settingsService.SaveAsync(settings);
        StatusMessage = "Security settings saved";
    }
}
