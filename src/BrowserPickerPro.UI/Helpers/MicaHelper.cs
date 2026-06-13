// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BrowserPickerPro.UI.Helpers;

public static class MicaHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_MICA_EFFECT = 1029;
    private const int DWMSBT_MAINWINDOW = 2;  // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
    private const int DWMSBT_TABBEDWINDOW = 4;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    public static bool TryApplyMica(Window window, bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();

            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            int backdropType = DWMSBT_MAINWINDOW;
            var result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            if (result != 0)
            {
                // Fallback: Mica legacy effect for older Windows 11 builds
                int micaEffect = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref micaEffect, sizeof(int));
            }

            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryApplyAcrylic(Window window, bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();

            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            int backdropType = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void UpdateDarkMode(Window window, bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            int darkMode = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }
        catch { }
    }
}
