// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BrowserPickerPro.UI.ViewModels;

public partial class ImportExportViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly ILogger<ImportExportViewModel> _logger;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public ImportExportViewModel(ISettingsService settingsService, DialogService dialogService, ILogger<ImportExportViewModel> logger)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = _dialogService.ShowSaveFileDialog(
            "Export Configuration", "JSON Files (*.json)|*.json", ".json");
        if (path == null) return;

        IsBusy = true;
        try
        {
            await _settingsService.ExportAsync(path);
            StatusMessage = $"Exported to {path}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            StatusMessage = "Export failed: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = _dialogService.ShowOpenFileDialog("Import Configuration", "JSON Files (*.json)|*.json");
        if (path == null) return;

        if (!_dialogService.ShowConfirmation("Import", "Import will overwrite your current settings. Continue?"))
            return;

        IsBusy = true;
        try
        {
            await _settingsService.ImportAsync(path);
            StatusMessage = "Configuration imported successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed");
            StatusMessage = "Import failed: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAppDataFolder()
    {
        System.Diagnostics.Process.Start("explorer.exe", _settingsService.AppDataPath);
    }
}
