using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RossCarlson.Vatsim.Vpilot.Plugins;
using RossCarlson.Vatsim.Vpilot.Plugins.Exceptions;

namespace FlightRecorder.VpilotPlugin
{
    public class VpilotPlugin : IPlugin
    {
        public const string PipeName = "FlightRecorder_vPilot";
        private IBroker? _broker;
        private volatile bool _isConnected;
        private CancellationTokenSource? _cts;
        private Task? _pipeServerTask;

        public string Name => "Flight Recorder vPilot Bridge";

        public void Initialize(IBroker broker)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));

            _broker.NetworkConnected += (s, e) =>
            {
                _isConnected = true;
                _broker?.PostDebugMessage("[Flight Recorder] Network connected event received.");
            };

            _broker.NetworkDisconnected += (s, e) =>
            {
                _isConnected = false;
                _broker?.PostDebugMessage("[Flight Recorder] Network disconnected event received.");
            };

            _broker.SessionEnded += (s, e) =>
            {
                Stop();
            };

            _cts = new CancellationTokenSource();
            _pipeServerTask = Task.Run(() => RunPipeServerAsync(_cts.Token));
            _broker.PostDebugMessage("[Flight Recorder] Plugin initialized and pipe server started.");
        }

        private async Task RunPipeServerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

#if NETSTANDARD2_0
                    await server.WaitForConnectionAsync().ConfigureAwait(false);
#else
                    await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
#endif

                    using var reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);
                    using var writer = new StreamWriter(server, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                    var command = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        var cmd = command.Trim().ToUpperInvariant();
                        switch (cmd)
                        {
                            case "PING":
                                await writer.WriteLineAsync("PONG").ConfigureAwait(false);
                                break;

                            case "STATUS":
                                await writer.WriteLineAsync(_isConnected ? "CONNECTED" : "DISCONNECTED").ConfigureAwait(false);
                                break;

                            case "DISCONNECT":
                                try
                                {
                                    if (_broker != null)
                                    {
                                        _broker.PostDebugMessage("[Flight Recorder] Disconnect requested from Flight Recorder.");
                                        _broker.RequestDisconnect();
                                        _isConnected = false;
                                        await writer.WriteLineAsync("DISCONNECTED_OK").ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await writer.WriteLineAsync("NO_BROKER").ConfigureAwait(false);
                                    }
                                }
                                catch (NotConnectedException)
                                {
                                    _isConnected = false;
                                    await writer.WriteLineAsync("ALREADY_DISCONNECTED").ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    _broker?.PostDebugMessage($"[Flight Recorder] Error disconnecting: {ex.Message}");
                                    await writer.WriteLineAsync($"ERROR: {ex.Message}").ConfigureAwait(false);
                                }
                                break;

                            default:
                                await writer.WriteLineAsync("UNKNOWN_COMMAND").ConfigureAwait(false);
                                break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _broker?.PostDebugMessage($"[Flight Recorder] Pipe server error: {ex.Message}");
                    try
                    {
                        await Task.Delay(500, ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        break;
                    }
                }
                finally
                {
                    try
                    {
                        if (server != null)
                        {
                            if (server.IsConnected)
                            {
                                server.Disconnect();
                            }
                            server.Dispose();
                        }
                    }
                    catch { }
                }
            }
        }

        private void Stop()
        {
            try
            {
                _cts?.Cancel();
            }
            catch { }
        }
    }
}
