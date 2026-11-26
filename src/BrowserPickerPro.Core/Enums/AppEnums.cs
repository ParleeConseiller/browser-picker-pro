// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: AppEnums.cs
// Description: All application-wide enumerations (theme, rules, security, browsers,
//              navigation and picker-window appearance options).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-12
// Last edit date: 2026-11-25
// Version: 1.1.0
namespace BrowserPickerPro.Core.Enums;

public enum AppTheme { System, Light, Dark }

public enum RuleType { Domain, Wildcard, Regex, Exact }

public enum RuleAction { OpenInBrowser, Block, AskAlways }

public enum SecurityLevel { Unknown, Safe, Suspicious, Dangerous }

public enum BrowserType { Chrome, Firefox, Edge, Brave, Vivaldi, Opera, OperaGX, Waterfox, LibreWolf, Zen, Floorp, Arc, Chromium, Unknown }

public enum NavigationSection { Browsers, Rules, Security, Profiles, Appearance, ImportExport, About }

public enum PickerWindowStyle { Modal, Fullscreen }
public enum PickerCardLayout { WrapGrid, SingleRow }
public enum PickerBackgroundMode { DarkGradient, CustomFile }

/// <summary>
/// How the picker window makes its entrance (Settings → Appearance → Opening animation).
/// Stored by name in settings.json, so values may be appended but never renamed.
/// </summary>
public enum PickerOpenAnimation
{
    /// <summary>Appears instantly (also used when animations are off or the OS asks for reduced motion).</summary>
    None,
    /// <summary>A glowing ring expands from the cursor, revealing the window inside it.</summary>
    CircleReveal,
    /// <summary>Zooms in from slightly oversized while a heavy blur sharpens into focus.</summary>
    ZoomBlur,
    /// <summary>Rises from below and settles with a soft spring overshoot.</summary>
    SpringRise,
    /// <summary>Old CRT TV switching on: a bright line opens horizontally, then vertically.</summary>
    PowerOn,
    /// <summary>Picks one of the animations above at random on every open.</summary>
    Random
}
