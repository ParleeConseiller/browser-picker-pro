// Browser Picker Pro — Created by Parlee Conseiller, Inc.
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
