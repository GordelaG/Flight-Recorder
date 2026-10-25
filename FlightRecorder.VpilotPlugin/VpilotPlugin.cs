using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using RossCarlson.Vatsim.Vpilot.Plugins;
using RossCarlson.Vatsim.Vpilot.Plugins.Exceptions;

namespace FlightRecorder.VpilotPlugin
{
    public class VpilotPlugin : IPlugin
    {
        public const string PipeName = "FlightRecorder_vPilot";
        private IBroker? _broker;
        private volatile bool _isConnected;
        private volatile bool _isRunning;
        private Thread? _serverThread;
        private NamedPipeServerStream? _currentServer;

        public string Name => "Flight Recorder vPilot Bridge";

        public void Initialize(IBroker broker)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Log("Initialize called.");

            _broker.NetworkConnected += (s, e) =>
            {
                _isConnected = true;
                _broker?.PostDebugMessage("[Flight Recorder] Network connected: " + e?.Callsign);
                Log("[Event] NetworkConnected (Callsign: " + e?.Callsign + ")");
            };

            _broker.NetworkDisconnected += (s, e) =>
            {
                _isConnected = false;
                _broker?.PostDebugMessage("[Flight Recorder] Network disconnected.");
                Log("[Event] NetworkDisconnected");
            };

            _broker.SessionEnded += (s, e) =>
            {
                Log("[Event] SessionEnded");
                Stop();
            };

            _isRunning = true;
            _serverThread = new Thread(RunPipeServer)
            {
                IsBackground = true,
                Name = "FlightRecorder_vPilot_Bridge"
            };
            _serverThread.Start();

            _broker.PostDebugMessage("[Flight Recorder] Plugin initialized and pipe server started.");
            Log("Plugin initialized and background pipe thread started.");
        }

        private void RunPipeServer()
        {
            Log("RunPipeServer started.");
            var enc = new UTF8Encoding(false);

            while (_isRunning)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None,
                        4096,
                        4096);

                    _currentServer = server;
                    server.WaitForConnection();

                    if (!_isRunning)
                    {
                        break;
                    }

                    using (var reader = new StreamReader(server, enc, false, 1024, leaveOpen: true))
                    using (var writer = new StreamWriter(server, enc, 1024, leaveOpen: true) { AutoFlush = true })
                    {
                        var command = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            var cmd = command.Trim().ToUpperInvariant();
                            Log($"Received command: '{cmd}'");
                            switch (cmd)
                            {
                                case "PING":
                                    writer.WriteLine("PONG");
                                    break;

                                case "STATUS":
                                    writer.WriteLine(_isConnected ? "CONNECTED" : "DISCONNECTED");
                                    break;

                                case "DISCONNECT":
                                    try
                                    {
                                        if (_broker != null)
                                        {
                                            Log("Calling _broker.RequestDisconnect()...");
                                            _broker.PostDebugMessage("[Flight Recorder] Disconnect requested from Flight Recorder.");
                                            _broker.RequestDisconnect();
                                            _isConnected = false;
                                            writer.WriteLine("DISCONNECTED_OK");
                                            Log("RequestDisconnect succeeded, sent DISCONNECTED_OK.");
                                        }
                                        else
                                        {
                                            writer.WriteLine("NO_BROKER");
                                            Log("No broker available.");
                                        }
                                    }
                                    catch (NotConnectedException)
                                    {
                                        _isConnected = false;
                                        writer.WriteLine("ALREADY_DISCONNECTED");
                                        Log("Caught NotConnectedException, sent ALREADY_DISCONNECTED.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log($"Error disconnecting: {ex.Message}");
                                        _broker?.PostDebugMessage($"[Flight Recorder] Error disconnecting: {ex.Message}");
                                        writer.WriteLine($"ERROR: {ex.Message}");
                                    }
                                    break;

                                default:
                                    writer.WriteLine("UNKNOWN_COMMAND");
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Log($"Pipe loop exception: {ex.Message}");
                        Thread.Sleep(200);
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
                    _currentServer = null;
                }
            }
            Log("RunPipeServer exited.");
        }

        private void Stop()
        {
            _isRunning = false;
            try
            {
                _currentServer?.Close();
            }
            catch { }
        }

        private static void Log(string message)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var logPath = Path.Combine(localAppData, "vPilot", "flightrecorder_plugin.log");
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
