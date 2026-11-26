// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: Program.cs
// Description: Entry point of BrowserPickerProLauncher.exe — the registered link handler.
//              Hot path: resident app found → hand it the foreground right and the URL over
//              the pipe → exit (a few ms). Cold path: start BrowserPickerPro.exe with the URL.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using System.Diagnostics;
using System.Runtime.InteropServices;
using BrowserPickerPro.Core.Ipc;

namespace BrowserPickerPro.Launcher;

internal static partial class Program
{
    // ASFW_ANY: let any process take the foreground. Needed because the resident app is a
    // background process and Windows' foreground lock would otherwise stop the picker from
    // coming to the front (it would only flash in the taskbar).
    private const int AsfwAny = -1;

    // How long to wait for the resident app to accept the message before cold-starting.
    private const int SendTimeoutMs = 3000;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllowSetForegroundWindow(int processId);

    private static int Main(string[] args)
    {
        // Taken first so the resident app can log real click-to-screen latency.
        var started = Stopwatch.GetTimestamp();

        // Windows passes the link as "%1" → exactly one argument (may be absent).
        var argument = args.Length > 0 ? args[0] : string.Empty;
        var command = argument.Length > 0 ? IpcProtocol.CommandOpen : IpcProtocol.CommandSettings;

        if (IpcProtocol.IsResidentRunning())
        {
            // We were started by the user's click, so we own the foreground right; pass it on.
            AllowSetForegroundWindow(AsfwAny);
            if (IpcProtocol.TrySend(new IpcMessage(command, started, argument), SendTimeoutMs))
                return 0;
        }

        return ColdStart(argument);
    }

    // No resident instance (first link after sign-in with start-with-Windows off, or the
    // app was exited): start the full app, which becomes resident and shows the picker.
    private static int ColdStart(string argument)
    {
        var mainExe = Path.Combine(AppContext.BaseDirectory, IpcProtocol.MainExecutableName);
        if (!File.Exists(mainExe)) return 2;

        var psi = new ProcessStartInfo(mainExe) { UseShellExecute = false };
        // ArgumentList quotes correctly, so the URL stays one argument whatever it contains.
        psi.ArgumentList.Add(argument.Length > 0 ? argument : IpcProtocol.SettingsSwitch);

        using var child = Process.Start(psi);
        if (child is null) return 3;

        // A child started by the foreground process may take the foreground; make it explicit.
        AllowSetForegroundWindow(child.Id);
        return 0;
    }
}
