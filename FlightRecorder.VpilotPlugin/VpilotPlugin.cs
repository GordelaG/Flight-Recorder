using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
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
        private volatile bool _disconnectRequestedByFlightRecorder;
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
                _disconnectRequestedByFlightRecorder = false;
                _broker?.PostDebugMessage("[Flight Recorder] Network connected: " + e?.Callsign);
                Log("[Event] NetworkConnected (Callsign: " + e?.Callsign + ")");
            };

            _broker.NetworkDisconnected += (s, e) =>
            {
                _isConnected = false;
                _broker?.PostDebugMessage("[Flight Recorder] Network disconnected.");
                Log("[Event] NetworkDisconnected");

                if (_disconnectRequestedByFlightRecorder)
                {
                    _disconnectRequestedByFlightRecorder = false;
                    ScheduleForcibleDisconnectAppearance();
                }
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
                                            _disconnectRequestedByFlightRecorder = true;
                                            _broker.PostDebugMessage("[Flight Recorder] Disconnect requested from Flight Recorder.");
                                            _broker.RequestDisconnect();
                                            _isConnected = false;
                                            writer.WriteLine("DISCONNECTED_OK");
                                            Log("RequestDisconnect succeeded, sent DISCONNECTED_OK.");
                                            ScheduleForcibleDisconnectAppearance();
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
                                        _disconnectRequestedByFlightRecorder = false;
                                        writer.WriteLine("ALREADY_DISCONNECTED");
                                        Log("Caught NotConnectedException, sent ALREADY_DISCONNECTED.");
                                    }
                                    catch (Exception ex)
                                    {
                                        _disconnectRequestedByFlightRecorder = false;
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

        private static void ScheduleForcibleDisconnectAppearance()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // First attempt after 60ms (allows vPilot's own event handler to write "Disconnected from network." first)
                    Thread.Sleep(60);
                    bool replaced = ApplyForcibleDisconnectionAppearance();
                    if (!replaced)
                    {
                        // Second attempt after another 200ms in case UI processing was slow
                        Thread.Sleep(200);
                        ApplyForcibleDisconnectionAppearance();
                    }
                }
                catch (Exception ex)
                {
                    Log("ScheduleForcibleDisconnectAppearance error: " + ex.Message);
                }
            });
        }

        private static bool ApplyForcibleDisconnectionAppearance()
        {
            bool anyReplaced = false;
            try
            {
                Type? appType = Type.GetType("System.Windows.Forms.Application, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                                ?? Assembly.Load("System.Windows.Forms")?.GetType("System.Windows.Forms.Application");

                if (appType == null)
                {
                    Log("System.Windows.Forms.Application type not found.");
                    return false;
                }

                var openFormsProp = appType.GetProperty("OpenForms", BindingFlags.Public | BindingFlags.Static);
                if (openFormsProp == null) return false;

                var openForms = openFormsProp.GetValue(null, null) as IEnumerable;
                if (openForms == null) return false;

                foreach (var form in openForms)
                {
                    if (form == null) continue;
                    var formType = form.GetType();
                    var isDisposedProp = formType.GetProperty("IsDisposed");
                    if (isDisposedProp != null && (bool)isDisposedProp.GetValue(form, null)!)
                    {
                        continue;
                    }

                    var beginInvokeMethod = formType.GetMethod("BeginInvoke", new[] { typeof(Delegate) });
                    if (beginInvokeMethod == null) continue;

                    beginInvokeMethod.Invoke(form, new object[] { (Action)(() =>
                    {
                        try
                        {
                            var rtbList = new List<object>();
                            CollectRichTextBoxes(form, rtbList);

                            // Brand orange-red color (#EE4203)
                            object? msgColor = null;
                            var colorType = Type.GetType("System.Drawing.Color, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
                                            ?? Assembly.Load("System.Drawing")?.GetType("System.Drawing.Color");
                            var fromArgb = colorType?.GetMethod("FromArgb", new[] { typeof(int), typeof(int), typeof(int) });
                            if (fromArgb != null)
                            {
                                msgColor = fromArgb.Invoke(null, new object[] { 238, 66, 3 });
                            }
                            if (msgColor == null)
                            {
                                msgColor = colorType?.GetProperty("OrangeRed", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
                            }

                            const string replacementText = "[Flight Recorder] Replay started - Disconnected from network.";

                            foreach (var rtb in rtbList)
                            {
                                var rtbType = rtb.GetType();
                                string? text = rtbType.GetProperty("Text")?.GetValue(rtb, null) as string;
                                if (text == null || text.Length == 0) continue;

                                // If already replaced or appended recently, do not duplicate
                                if (text.Contains(replacementText) && text.LastIndexOf(replacementText, StringComparison.Ordinal) >= text.Length - 250)
                                {
                                    anyReplaced = true;
                                    continue;
                                }

                                int idx = text.LastIndexOf("Disconnected from network.", StringComparison.Ordinal);
                                int targetLen = "Disconnected from network.".Length;
                                if (idx < 0)
                                {
                                    idx = text.LastIndexOf("Forcibly disconnected from network.", StringComparison.Ordinal);
                                    targetLen = "Forcibly disconnected from network.".Length;
                                }
                                if (idx < 0)
                                {
                                    idx = text.LastIndexOf("[Flight Recorder] Replay iniciado - Desconectado da rede.", StringComparison.Ordinal);
                                    targetLen = "[Flight Recorder] Replay iniciado - Desconectado da rede.".Length;
                                }

                                if (idx >= 0 && idx >= text.Length - 250)
                                {
                                    // Select target text
                                    rtbType.GetMethod("Select", new[] { typeof(int), typeof(int) })?
                                           .Invoke(rtb, new object[] { idx, targetLen });

                                    // Replace text
                                    rtbType.GetProperty("SelectedText")?.SetValue(rtb, replacementText, null);

                                    // Select newly inserted text and apply custom color
                                    rtbType.GetMethod("Select", new[] { typeof(int), typeof(int) })?
                                           .Invoke(rtb, new object[] { idx, replacementText.Length });
                                    if (msgColor != null)
                                    {
                                        rtbType.GetProperty("SelectionColor")?.SetValue(rtb, msgColor, null);
                                    }

                                    // Move caret to end
                                    int newLength = (int)(rtbType.GetProperty("TextLength")?.GetValue(rtb, null) ?? 0);
                                    rtbType.GetProperty("SelectionStart")?.SetValue(rtb, newLength, null);
                                    rtbType.GetMethod("ScrollToCaret", Type.EmptyTypes)?.Invoke(rtb, null);

                                    anyReplaced = true;
                                    Log("Successfully replaced disconnect message with '" + replacementText + "' in RichTextBox.");
                                }
                                else
                                {
                                    // If disconnect message wasn't found in text, append it directly as a new line
                                    int curLength = (int)(rtbType.GetProperty("TextLength")?.GetValue(rtb, null) ?? 0);
                                    rtbType.GetProperty("SelectionStart")?.SetValue(rtb, curLength, null);

                                    string timeStamp = DateTime.UtcNow.ToString("HH:mm:ss");
                                    string fullLine = $"[{timeStamp}] {replacementText}{Environment.NewLine}";

                                    rtbType.GetProperty("SelectedText")?.SetValue(rtb, fullLine, null);
                                    rtbType.GetMethod("Select", new[] { typeof(int), typeof(int) })?
                                           .Invoke(rtb, new object[] { curLength, fullLine.Length });
                                    if (msgColor != null)
                                    {
                                        rtbType.GetProperty("SelectionColor")?.SetValue(rtb, msgColor, null);
                                    }

                                    int finalLength = (int)(rtbType.GetProperty("TextLength")?.GetValue(rtb, null) ?? 0);
                                    rtbType.GetProperty("SelectionStart")?.SetValue(rtb, finalLength, null);
                                    rtbType.GetMethod("ScrollToCaret", Type.EmptyTypes)?.Invoke(rtb, null);

                                    anyReplaced = true;
                                    Log("Successfully appended '" + replacementText + "' to RichTextBox.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("Error during UI replacement: " + ex.Message);
                        }
                    })});
                }
            }
            catch (Exception ex)
            {
                Log("ApplyForcibleDisconnectionAppearance error: " + ex.Message);
            }

            return anyReplaced;
        }

        private static void CollectRichTextBoxes(object control, List<object> result)
        {
            if (control == null) return;
            var cType = control.GetType();
            if (cType.Name == "RichTextBox" || (cType.BaseType != null && cType.BaseType.Name == "RichTextBox"))
            {
                result.Add(control);
            }

            var controlsProp = cType.GetProperty("Controls");
            if (controlsProp != null)
            {
                var children = controlsProp.GetValue(control, null) as IEnumerable;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child != null)
                        {
                            CollectRichTextBoxes(child, result);
                        }
                    }
                }
            }
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
