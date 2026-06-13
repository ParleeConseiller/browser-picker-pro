// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrowserPickerPro.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigation;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private NavigationSection _currentSection = NavigationSection.Browsers;
    [ObservableProperty] private string _title = "Browser Picker Pro";

    public MainViewModel(NavigationService navigation, ISettingsService settingsService)
    {
        _navigation = navigation;
        _settingsService = settingsService;
        _navigation.Navigated += (_, s) => CurrentSection = s;
    }

    [RelayCommand]
    private void NavigateTo(NavigationSection section)
    {
        _navigation.NavigateTo(section);
    }

    public bool IsActive(NavigationSection section) => CurrentSection == section;
}
