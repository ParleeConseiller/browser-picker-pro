// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: TrayHost.cs
// Description: Notification-area (tray) icon of the resident app and its menu:
//              Settings, Refresh browsers, Start with Windows, Exit.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Interfaces;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BrowserPickerPro.UI.Services;

public sealed class TrayHost : IDisposable
{
    // Embedded WPF resource (BrowserPickerPro.UI.csproj <Resource Include="Assets\Icons\app.ico"/>).
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/Icons/app.ico", UriKind.Absolute);
    private const string ToolTip = "Browser Picker Pro — ready";

    private readonly ISettingsService _settingsService;
    private readonly IStartupRegistrar _startup;
    private TaskbarIcon? _icon;
    private MenuItem? _startWithWindowsItem;

    public TrayHost(ISettingsService settingsService, IStartupRegistrar startup)
    {
        _settingsService = settingsService;
        _startup = startup;
    }

    /// <summary>Raised when the user asks for the dashboard (menu or double-click).</summary>
    public event Action? SettingsRequested;
    public event Action? RefreshRequested;
    public event Action? ExitRequested;

    public void Show(bool startWithWindows)
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item("Settings…", () => SettingsRequested?.Invoke()));
        menu.Items.Add(Item("Refresh browsers", () => RefreshRequested?.Invoke()));
        menu.Items.Add(new Separator());

        _startWithWindowsItem = Item("Start with Windows", ToggleStartWithWindows);
        _startWithWindowsItem.IsCheckable = true;
        _startWithWindowsItem.IsChecked = startWithWindows;
        menu.Items.Add(_startWithWindowsItem);

        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Exit", () => ExitRequested?.Invoke()));

        _icon = new TaskbarIcon
        {
            ToolTipText = ToolTip,
            IconSource = BitmapFrame.Create(IconUri),
            ContextMenu = menu,
            // Double-click opens the dashboard; no need to delay single clicks.
            NoLeftClickDelay = true,
            DoubleClickCommand = new RelayCommand(() => SettingsRequested?.Invoke()),
        };

        // enablesEfficiencyMode:false — the default puts the WHOLE process in Windows 11
        // "Efficiency mode" (EcoQoS, low priority), which would make the picker open slowly.
        _icon.ForceCreate(enablesEfficiencyMode: false);
    }

    // Menu toggle writes the setting and the Run key together.
    private async void ToggleStartWithWindows()
    {
        var enabled = _startWithWindowsItem?.IsChecked == true;
        if (enabled) _startup.Enable(); else _startup.Disable();

        var settings = await _settingsService.LoadAsync();
        settings.StartWithWindows = enabled;
        await _settingsService.SaveAsync(settings);
    }

    private static MenuItem Item(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    public void Dispose()
    {
        _icon?.Dispose();
        _icon = null;
    }
}
