// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: NativeMethods.cs
// Description: Win32 P/Invoke used by the resident picker: cursor position, the work area
//              and DPI of the monitor under the cursor, moving/parking a window in physical
//              pixels, tool-window style, and the foreground helpers.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using System.Runtime.InteropServices;

namespace BrowserPickerPro.UI.Helpers;

internal static class NativeMethods
{
    // MonitorFromPoint: pick the nearest monitor when the point is between screens.
    private const uint MonitorDefaultToNearest = 2;
    // GetDpiForMonitor: the effective DPI (what WPF uses for scaling).
    private const int MdtEffectiveDpi = 0;
    // SetWindowPos flags: move only — keep size, z-order and activation untouched.
    private const uint SwpNoSize = 0x0001, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010;
    /// <summary>ASFW_ANY for AllowSetForegroundWindow.</summary>
    public const int AsfwAny = -1;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int processId);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080; // no taskbar button, not listed in Alt+Tab
    private const long WsExAppWindow = 0x00040000;  // forces a taskbar button — must be cleared

    /// <summary>
    /// Makes the window a tool window: it never appears in Alt+Tab or the taskbar, which
    /// matters because the resident picker stays "shown" (parked off-screen) between links.
    /// </summary>
    public static void MakeToolWindow(IntPtr hwnd)
    {
        var style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        style = (style | WsExToolWindow) & ~WsExAppWindow;
        SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(style));
    }

    /// <summary>Cursor position in physical screen pixels.</summary>
    public static POINT CursorPosition()
    {
        GetCursorPos(out var p);
        return p;
    }

    /// <summary>Work area (taskbar excluded) and monitor rect, in physical pixels, plus DPI scale (1.0 = 96 dpi).</summary>
    public static (RECT Work, RECT Monitor, double Scale) MonitorUnder(POINT point)
    {
        var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);

        // shcore may fail on very old systems; fall back to 96 dpi.
        var scale = GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dpiX, out _) == 0 ? dpiX / 96.0 : 1.0;
        return (info.rcWork, info.rcMonitor, scale);
    }

    /// <summary>Moves a window (visible or not) to a physical-pixel position.</summary>
    public static void MoveWindow(IntPtr hwnd, int x, int y)
        => SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
}
