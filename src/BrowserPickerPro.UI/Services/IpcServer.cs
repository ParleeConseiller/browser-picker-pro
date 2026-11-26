// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: IpcServer.cs
// Description: Named-pipe server of the resident app. Accepts one-line messages from the
//              launcher (or from a second BrowserPickerPro.exe), replies "OK" immediately,
//              then hands the message to the UI thread. Current-user-only pipe.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Ipc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace BrowserPickerPro.UI.Services;

public sealed class IpcServer : IDisposable
{
    // A client that connects but never writes must not stall the server forever.
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(2);
    // Longest line we accept: URLs are capped far below this by every browser.
    private const int MaxLineLength = 64 * 1024;

    private readonly ILogger<IpcServer> _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public IpcServer(ILogger<IpcServer> logger) => _logger = logger;

    /// <summary>Raised on a thread-pool thread for every valid message.</summary>
    public event Action<IpcMessage>? MessageReceived;

    /// <summary>Starts listening. Call as early as possible so early launchers don't time out.</summary>
    public void Start() => _loop = Task.Run(() => ListenLoopAsync(_cts.Token));

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // One instance at a time is enough: handling a message takes microseconds and
                // NamedPipeClientStream.Connect() retries while the pipe is busy.
                await using var server = new NamedPipeServerStream(
                    IpcProtocol.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

                using var reader = new StreamReader(server, utf8, false, 1024, leaveOpen: true);
                await using var writer = new StreamWriter(server, utf8, 64, leaveOpen: true) { AutoFlush = true };

                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readCts.CancelAfter(ReadTimeout);
                var line = await reader.ReadLineAsync(readCts.Token).ConfigureAwait(false);

                var message = line is { Length: <= MaxLineLength } ? IpcProtocol.Parse(line) : null;
                if (message is null)
                {
                    _logger.LogWarning("Ignored malformed IPC message");
                    continue;
                }

                // Reply first so the launcher can exit while we start drawing.
                await writer.WriteLineAsync(IpcProtocol.ReplyOk).ConfigureAwait(false);
                MessageReceived?.Invoke(message);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A broken client must never kill the server loop.
                _logger.LogWarning(ex, "IPC connection failed");
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _loop?.Wait(TimeSpan.FromSeconds(1)); } catch { /* shutting down */ }
        _cts.Dispose();
    }
}
