// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: IStartupRegistrar.cs
// Description: Abstraction over "start Browser Picker Pro with Windows" (per-user Run key),
//              so the resident tray app can be warm before the first link click.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
namespace BrowserPickerPro.Core.Interfaces;

public interface IStartupRegistrar
{
    /// <summary>True when the Run entry exists and points at the current executable.</summary>
    bool IsEnabled();

    /// <summary>Creates or refreshes the Run entry (starts the app with <c>--tray</c>).</summary>
    void Enable();

    /// <summary>Removes the Run entry; no-op when it doesn't exist.</summary>
    void Disable();
}
