// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: IpcProtocol.cs
// Description: Wire protocol between the tiny link launcher (BrowserPickerProLauncher.exe)
//              and the resident tray app: names of the single-instance mutex and the
//              per-user named pipe, the one-line message format, and a client sender.
//              This file is compiled into BOTH Core and the NativeAOT launcher, so it
//              must only use BCL types that are trim/AOT safe (no reflection, no JSON).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.1.0
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace BrowserPickerPro.Core.Ipc;

/// <summary>A single request sent from a launcher to the resident app.</summary>
/// <param name="Command">One of the <c>IpcProtocol.Command*</c> constants.</param>
/// <param name="Timestamp">
/// <see cref="Stopwatch.GetTimestamp"/> taken when the launcher started. QPC ticks are
/// machine-wide, so the resident app can compute true "click → picker on screen" latency.
/// 0 means "unknown".
/// </param>
/// <param name="Argument">The URL / file path Windows handed to the launcher (may be empty).</param>
public sealed record IpcMessage(string Command, long Timestamp, string Argument);

public static class IpcProtocol
{
    /// <summary>Show the picker for <see cref="IpcMessage.Argument"/>.</summary>
    public const string CommandOpen = "OPEN";

    /// <summary>Open (or focus) the configuration dashboard.</summary>
    public const string CommandSettings = "SETTINGS";

    /// <summary>Reply the server writes once it has accepted a message.</summary>
    public const string ReplyOk = "OK";

    /// <summary>File name of the full WPF application next to the launcher.</summary>
    public const string MainExecutableName = "BrowserPickerPro.exe";

    /// <summary>File name of the NativeAOT launcher registered as the link handler.</summary>
    public const string LauncherExecutableName = "BrowserPickerProLauncher.exe";

    /// <summary>Command-line switch: start resident in the tray without showing any window.</summary>
    public const string TraySwitch = "--tray";

    /// <summary>Command-line switch: open the configuration dashboard.</summary>
    public const string SettingsSwitch = "--settings";

    // Field separator of the one-line wire format. URLs never contain a raw TAB.
    private const char Separator = '\t';

    /// <summary>
    /// Name of the mutex held by the resident instance. "Local\" scopes it to the
    /// current logon session, so two users on one machine each get their own resident app.
    /// </summary>
    public static string MutexName => $@"Local\BrowserPickerPro.Resident.{SafeUserName()}";

    /// <summary>
    /// Pipe names are machine-global (no Local\ scope), so the session id is part of
    /// the name. The pipe itself is also created with <see cref="PipeOptions.CurrentUserOnly"/>.
    /// </summary>
    public static string PipeName
    {
        get
        {
            using var self = Process.GetCurrentProcess();
            return $"BrowserPickerPro.Picker.{SafeUserName()}.{self.SessionId}";
        }
    }

    /// <summary>Serializes a message to its single-line wire form.</summary>
    public static string Format(IpcMessage message)
        => string.Concat(message.Command, Separator.ToString(), message.Timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                         Separator.ToString(), StripLineBreaks(message.Argument));

    /// <summary>Parses a wire line; returns null for anything malformed or unknown.</summary>
    public static IpcMessage? Parse(string? line)
    {
        if (string.IsNullOrEmpty(line)) return null;

        // Split into at most 3 parts so a TAB inside the argument can't shift fields.
        var parts = line.Split(Separator, 3);
        if (parts.Length != 3) return null;

        var command = parts[0];
        if (command != CommandOpen && command != CommandSettings) return null;

        if (!long.TryParse(parts[1], System.Globalization.NumberStyles.Integer,
                           System.Globalization.CultureInfo.InvariantCulture, out var timestamp))
            return null;

        return new IpcMessage(command, timestamp, parts[2]);
    }

    /// <summary>True when a resident instance currently owns the single-instance mutex.</summary>
    public static bool IsResidentRunning()
    {
        // TryOpenExisting never creates the mutex, so probing has no side effects.
        if (!Mutex.TryOpenExisting(MutexName, out var mutex)) return false;
        mutex.Dispose();
        return true;
    }

    /// <summary>
    /// Sends one message to the resident app and waits for its "OK".
    /// Returns false on any failure so the caller can fall back to a cold start.
    /// </summary>
    public static bool TrySend(IpcMessage message, int timeoutMs)
        => TrySend(message, timeoutMs, PipeName);

    /// <summary>Same as above with an explicit pipe name (tests use a private one so they
    /// never reach a real resident instance running on the developer's machine).</summary>
    public static bool TrySend(IpcMessage message, int timeoutMs, string pipeName)
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly);

            // Connect() also retries while the server is busy with another client.
            client.Connect(timeoutMs);

            // UTF-8 without BOM: the server reads exactly one line.
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            using var writer = new StreamWriter(client, utf8, bufferSize: 1024, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(client, utf8, detectEncodingFromByteOrderMarks: false, bufferSize: 64, leaveOpen: true);

            writer.WriteLine(Format(message));
            return reader.ReadLine() == ReplyOk;
        }
        catch (Exception)
        {
            // TimeoutException (no server), IOException (server went away), etc.
            return false;
        }
    }

    // Removes CR/LF so an argument can never inject a second protocol line.
    private static string StripLineBreaks(string value)
        => value.Replace("\r", string.Empty).Replace("\n", string.Empty);

    // Kernel object names can't contain '\'; user names normally don't, but be safe.
    private static string SafeUserName()
        => Environment.UserName.Replace('\\', '_').Replace('/', '_');
}
