// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: Elevation.cs
// Description: Detects an elevated (administrator) process and relaunches the app at the
//              user's normal integrity level through the desktop's own Explorer instance.
//              The resident app must never run elevated: links come from non-elevated
//              processes, whose launcher cannot open an elevated process's pipe, so every
//              link was silently dropped (and browsers started from an elevated picker would
//              run as admin).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-26
// Last edit date: 2026-11-26
// Version: 2.0.0
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace BrowserPickerPro.UI.Helpers;

internal static class Elevation
{
    // ── Token check of the desktop shell (UAC-off guard) ─────────────────────
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenQuery = 0x0008;
    private const int TokenElevationClass = 20;

    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint processId);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr token, int infoClass, out int info, int length, out int returned);

    // ── Shell COM interfaces (only the slots we call are real; the rest keep vtable order) ──
    // Raymond Chen, "How can I launch an unelevated process from my elevated process?":
    // find the desktop's shell view, get its Shell.Application, and ShellExecute there — the
    // program then starts from Explorer, with Explorer's (non-elevated) token.
    private static readonly Guid ClsidShellWindows = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
    private static readonly Guid SidSTopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
    private static readonly Guid IidIShellBrowser = new("000214E2-0000-0000-C000-000000000046");
    private static readonly Guid IidIDispatch = new("00020400-0000-0000-C000-000000000046");
    private const int CsidlDesktop = 0;        // ShellWindows.FindWindowSW: the desktop
    private const int SwcDesktop = 8;          // SWC_DESKTOP
    private const int SwfoNeedDispatch = 1;    // SWFO_NEEDDISPATCH
    private const uint SvgioBackground = 0;    // IShellView.GetItemObject: the view background
    private const int SwShowNormal = 1;

    // IShellWindows through its dual vtable (FindWindowSW has an out parameter).
    [ComImport, Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    private interface IShellWindowsDual
    {
        int Count { get; }
        [return: MarshalAs(UnmanagedType.IDispatch)] object Item(object index);
        [return: MarshalAs(UnmanagedType.IUnknown)] object _NewEnum();
        void Register(object pid, int hwnd, int swClass, out int cookie);
        void RegisterPending(int threadId, ref object loc, ref object locRoot, int swClass, out int cookie);
        void Revoke(int cookie);
        void OnNavigate(int cookie, ref object loc);
        void OnActivated(int cookie, bool active);
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object FindWindowSW(ref object loc, ref object locRoot, int swClass, out int hwnd, int swfwOptions);
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryService(ref Guid service, ref Guid riid);
    }

    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        void GetWindow();                 // IOleWindow
        void ContextSensitiveHelp();      // IOleWindow
        void InsertMenusSB();
        void SetMenuSB();
        void RemoveMenusSB();
        void SetStatusTextSB();
        void EnableModelessSB();
        void TranslateAcceleratorSB();
        void BrowseObject();
        void GetViewStateStream();
        void GetControlWindow();
        void SendControlMsg();
        IShellView QueryActiveShellView();
    }

    [ComImport, Guid("000214E3-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellView
    {
        void GetWindow();                 // IOleWindow
        void ContextSensitiveHelp();      // IOleWindow
        void TranslateAccelerator();
        void EnableModeless();
        void UIActivate();
        void Refresh();
        void CreateViewWindow();
        void DestroyViewWindow();
        void GetCurrentInfo();
        void AddPropertySheetPages();
        void SaveViewState();
        void SelectItem();
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetItemObject(uint item, ref Guid riid);
    }

    /// <summary>True when this process runs with an elevated (administrator) token.</summary>
    public static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        // A filtered (non-elevated) admin token does not report the Administrators role.
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Asks the desktop's Explorer to start <paramref name="exePath"/> with
    /// <paramref name="arguments"/>, so it runs with the user's normal, non-elevated token.
    /// Returns false — without throwing — when there is no shell, the shell itself is
    /// elevated (UAC off: relaunching would loop forever), or the request fails.
    /// </summary>
    public static bool TryRelaunchUnelevated(string exePath, IEnumerable<string> arguments, out string? failure)
    {
        failure = null;
        if (IsDesktopShellElevated(out failure)) return false;
        if (failure != null) return false;

        try
        {
            var shellWindows = (IShellWindowsDual)Activator.CreateInstance(Type.GetTypeFromCLSID(ClsidShellWindows, throwOnError: true)!)!;
            object loc = CsidlDesktop, root = Type.Missing;
            var desktop = shellWindows.FindWindowSW(ref loc, ref root, SwcDesktop, out _, SwfoNeedDispatch);

            var service = SidSTopLevelBrowser;
            var iidBrowser = IidIShellBrowser;
            var browser = (IShellBrowser)((IServiceProvider)desktop).QueryService(ref service, ref iidBrowser);
            var view = browser.QueryActiveShellView();

            var iidDispatch = IidIDispatch;
            dynamic folderView = view.GetItemObject(SvgioBackground, ref iidDispatch);  // IShellFolderViewDual
            dynamic shell = folderView.Application;                                    // IShellDispatch2

            shell.ShellExecute(exePath, BuildArguments(arguments), Path.GetDirectoryName(exePath) ?? string.Empty,
                               string.Empty, SwShowNormal);
            return true;
        }
        catch (Exception ex)
        {
            failure = ex.Message;
            return false;
        }
    }

    // UAC disabled → Explorer is elevated too, and a relaunch through it would come back
    // elevated and relaunch again, forever. Detect that and stay put.
    private static bool IsDesktopShellElevated(out string? failure)
    {
        failure = null;
        var shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero) { failure = "no desktop shell window"; return false; }
        GetWindowThreadProcessId(shellWindow, out var shellPid);

        var process = OpenProcess(ProcessQueryLimitedInformation, false, shellPid);
        if (process == IntPtr.Zero) { failure = "cannot open the desktop shell process"; return false; }
        try
        {
            if (!OpenProcessToken(process, TokenQuery, out var token)) { failure = "cannot read the desktop shell token"; return false; }
            try
            {
                var elevated = GetTokenInformation(token, TokenElevationClass, out var value, sizeof(int), out _) && value != 0;
                if (elevated) failure = "the desktop shell is elevated too (UAC off)";
                return elevated;
            }
            finally
            {
                CloseHandle(token);
            }
        }
        finally
        {
            CloseHandle(process);
        }
    }

    // arg1 arg2 … with the standard Windows (CommandLineToArgvW) quoting rules, so a URL or
    // path containing spaces, quotes or backslashes arrives as exactly one argument.
    private static string BuildArguments(IEnumerable<string> arguments)
    {
        var sb = new StringBuilder();
        foreach (var argument in arguments)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append('"');
            var backslashes = 0;
            foreach (var c in argument)
            {
                if (c == '\\') { backslashes++; continue; }
                // Backslashes before a quote are doubled, and the quote itself escaped.
                if (c == '"') sb.Append('\\', backslashes * 2 + 1);
                else sb.Append('\\', backslashes);
                backslashes = 0;
                sb.Append(c);
            }
            // Trailing backslashes are doubled so they don't escape the closing quote.
            sb.Append('\\', backslashes * 2).Append('"');
        }
        return sb.ToString();
    }
}
