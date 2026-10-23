using FlightRecorder.Client.Logics;
using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace FlightRecorder.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml with Flux UI WebView2 host
/// </summary>
public partial class MainWindow : BaseWindow
{
    private readonly ILogger<MainWindow> logger;
    private readonly IConnector connector;
    private readonly ICrashLogic crashLogic;
    private readonly DrawingLogic drawingLogic;
    private readonly CsvExportLogic csvExportLogic;
    private readonly KmlExportLogic kmlExportLogic;
    private readonly ShortcutKeyLogic shortcutKeyLogic;
    private readonly WindowFactory windowFactory;
    private readonly IRecorderLogic recorderLogic;
    private readonly AircraftProfileManager profileManager;
    private readonly HubHopSyncService hubHopSyncService;
    private readonly AutoUpdateService autoUpdateService;
    private readonly string currentVersion;

    private IntPtr Handle;
    private readonly Stopwatch telemetryStopwatch = Stopwatch.StartNew();
    private long lastTelemetrySendTime = 0;
    private bool isWebViewReady = false;

    public MainWindow(
        ILogger<MainWindow> logger,
        IConnector connector,
        ICrashLogic crashLogic,
        DrawingLogic drawingLogic,
        CsvExportLogic csvExportLogic,
        KmlExportLogic kmlExportLogic,
        VersionLogic versionLogic,
        ShortcutKeyLogic shortcutKeyLogic,
        Orchestrator orchestrator,
        WindowFactory windowFactory,
        AircraftProfileManager profileManager,
        HubHopSyncService hubHopSyncService,
        AutoUpdateService autoUpdateService
    ) : base(orchestrator.ThreadLogic, orchestrator.StateMachine, orchestrator.ViewModel, orchestrator.ReplayLogic)
    {
        InitializeComponent();

        this.logger = logger;
        this.connector = connector;
        this.crashLogic = crashLogic;
        this.drawingLogic = drawingLogic;
        this.csvExportLogic = csvExportLogic;
        this.kmlExportLogic = kmlExportLogic;
        this.shortcutKeyLogic = shortcutKeyLogic;
        this.windowFactory = windowFactory;
        this.recorderLogic = orchestrator.RecorderLogic;
        this.profileManager = profileManager;
        this.hubHopSyncService = hubHopSyncService;
        this.autoUpdateService = autoUpdateService;

        stateMachine.StateChanged += StateMachine_StateChanged;
        connector.AircraftPositionUpdated += Connector_AircraftPositionUpdated;
        connector.Closed += Connector_Closed;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;

        DataContext = viewModel;
        currentVersion = versionLogic.GetVersion();
        Title = $"Flight Recorder {currentVersion}";
    }

    protected override async Task Window_LoadedAsync(object sender, RoutedEventArgs e)
    {
        await base.Window_LoadedAsync(sender, e);

        // Initialize WebView2
        await InitializeWebViewAsync();

        await crashLogic.LoadDataAsync(stateMachine, replayLogic);

        // Create an event handle for the WPF window to listen for SimConnect events
        Handle = new WindowInteropHelper(this).Handle;
        var handleSource = HwndSource.FromHwnd(Handle);
        handleSource?.AddHook(HandleHook);

        InitializeConnector();
        await shortcutKeyLogic.RegisterAsync(Handle);
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            var wwwrootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.flightrecorder",
                wwwrootDir,
                CoreWebView2HostResourceAccessKind.Allow
            );

            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            webView.WebMessageReceived += WebView_WebMessageReceived;
            webView.Source = new Uri("https://app.flightrecorder/index.html");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize WebView2 control.");
            MessageBox.Show("Failed to initialize WebView2. Please make sure WebView2 Runtime is installed.", "Flight Recorder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var rawJson = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("action", out var actionProp)) return;
            var action = actionProp.GetString();

            switch (action)
            {
                case "uiReady":
                    isWebViewReady = true;
                    SendFullStateToWeb();
                    SendRecordsToWeb();
                    _ = Task.Run(async () => await CheckAndApplyUpdateAsync());
                    break;

                case "record":
                    await stateMachine.TransitAsync(StateMachine.Event.Record);
                    break;

                case "stop":
                    await stateMachine.TransitAsync(StateMachine.Event.Stop);
                    break;

                case "replay":
                    await stateMachine.TransitAsync(StateMachine.Event.Replay);
                    break;

                case "pause":
                    await stateMachine.TransitAsync(StateMachine.Event.Pause);
                    break;

                case "resume":
                    await stateMachine.TransitAsync(StateMachine.Event.Resume);
                    break;

                case "stopReplay":
                    await stateMachine.TransitAsync(StateMachine.Event.RequestStopping);
                    break;

                case "seek":
                    if (root.TryGetProperty("frame", out var frameProp))
                    {
                        var frame = frameProp.GetInt32();
                        replayLogic.Seek(frame);
                    }
                    break;

                case "save":
                    await stateMachine.TransitAsync(StateMachine.Event.Save);
                    break;

                case "load":
                    if (await stateMachine.TransitAsync(StateMachine.Event.Load))
                    {
                        SendRecordsToWeb();
                    }
                    break;

                case "replayAI":
                    ButtonReplayAI_Click(this, new RoutedEventArgs());
                    break;

                case "trimStart":
                    if (await stateMachine.TransitAsync(StateMachine.Event.TrimStart))
                    {
                        SendRecordsToWeb();
                    }
                    break;

                case "trimEnd":
                    if (await stateMachine.TransitAsync(StateMachine.Event.TrimEnd))
                    {
                        SendRecordsToWeb();
                    }
                    break;

                case "setSpeed":
                    if (root.TryGetProperty("rate", out var rateProp))
                    {
                        replayLogic.ChangeRate(rateProp.GetDouble());
                    }
                    break;

                case "setRepeat":
                    if (root.TryGetProperty("repeat", out var repeatProp))
                    {
                        replayLogic.SetRepeat(repeatProp.GetBoolean());
                    }
                    break;

                case "export":
                    if (root.TryGetProperty("format", out var formatProp))
                    {
                        var format = formatProp.GetString();
                        await ExportAsync(format == "kml" ? kmlExportLogic : csvExportLogic);
                    }
                    break;

                case "setTopmost":
                    if (root.TryGetProperty("topmost", out var topmostProp))
                    {
                        Topmost = topmostProp.GetBoolean();
                    }
                    break;

                case "openProfilesFolder":
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var profilesDir = Path.Combine(baseDir, "aircraft_profiles");
                        if (!Directory.Exists(profilesDir)) Directory.CreateDirectory(profilesDir);
                        Process.Start(new ProcessStartInfo("explorer.exe", profilesDir) { UseShellExecute = true });
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing web message from WebView2.");
        }
    }

    private void PostWebMessage(object data)
    {
        if (!isWebViewReady) return;

        Dispatcher.Invoke(() =>
        {
            try
            {
                if (webView?.CoreWebView2 == null) return;
                var json = JsonSerializer.Serialize(data);
                webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "Failed to send web message.");
            }
        });
    }

    private void SendFullStateToWeb()
    {
        PostWebMessage(new
        {
            type = "stateUpdate",
            payload = new
            {
                machineState = viewModel.State.ToString(),
                simConnectState = viewModel.SimConnectState.ToString(),
                currentFrame = viewModel.CurrentFrame,
                frameCount = viewModel.FrameCount,
                aircraftTitle = viewModel.AircraftTitle,
                fileName = viewModel.FileName,
                activeProfileName = viewModel.ActiveProfileName,
                activeProfileVendor = viewModel.ActiveProfileVendor,
                activeProfileVarCount = viewModel.ActiveProfileVarCount,
                topmost = Topmost
            }
        });
    }

    private void SendRecordsToWeb()
    {
        var recordsList = replayLogic.Records.Select(r => new
        {
            time = r.milliseconds,
            altitude = r.position.Altitude,
            isOnGround = r.position.IsOnGround != 0
        }).ToList();

        PostWebMessage(new
        {
            type = "recordsUpdate",
            payload = recordsList
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.CurrentFrame):
                PostWebMessage(new
                {
                    type = "stateUpdate",
                    payload = new { currentFrame = viewModel.CurrentFrame }
                });
                break;

            case nameof(MainViewModel.FrameCount):
                PostWebMessage(new
                {
                    type = "stateUpdate",
                    payload = new { frameCount = viewModel.FrameCount }
                });
                break;

            case nameof(MainViewModel.State):
                PostWebMessage(new
                {
                    type = "stateUpdate",
                    payload = new { machineState = viewModel.State.ToString() }
                });
                break;

            case nameof(MainViewModel.SimConnectState):
                PostWebMessage(new
                {
                    type = "stateUpdate",
                    payload = new { simConnectState = viewModel.SimConnectState.ToString() }
                });
                break;

            case nameof(MainViewModel.FileName):
            case nameof(MainViewModel.AircraftTitle):
                PostWebMessage(new
                {
                    type = "stateUpdate",
                    payload = new
                    {
                        fileName = viewModel.FileName,
                        aircraftTitle = viewModel.AircraftTitle
                    }
                });
                break;

            case nameof(MainViewModel.ActiveProfileName):
            case nameof(MainViewModel.ActiveProfileVarCount):
                PostWebMessage(new
                {
                    type = "stateUpdate",
                    payload = new
                    {
                        activeProfileName = viewModel.ActiveProfileName,
                        activeProfileVendor = viewModel.ActiveProfileVendor,
                        activeProfileVarCount = viewModel.ActiveProfileVarCount
                    }
                });
                break;
        }
    }

    private void StateMachine_StateChanged(object? sender, StateChangedEventArgs e)
    {
        SendFullStateToWeb();
        if (e.By == StateMachine.Event.Stop || e.By == StateMachine.Event.Load || e.By == StateMachine.Event.TrimStart || e.By == StateMachine.Event.TrimEnd)
        {
            SendRecordsToWeb();
        }
    }

    private void Connector_AircraftPositionUpdated(object? sender, AircraftPositionUpdatedEventArgs e)
    {
        recorderLogic.NotifyPosition(e.Position);
        replayLogic.NotifyPosition(e.Position);

        Dispatcher.Invoke(() =>
        {
            viewModel.AircraftPosition = AircraftPosition.FromStruct(e.Position);
        });

        // Throttle telemetry messages sent to Web (~30 FPS)
        if (telemetryStopwatch.ElapsedMilliseconds - lastTelemetrySendTime > 33)
        {
            lastTelemetrySendTime = telemetryStopwatch.ElapsedMilliseconds;
            PostWebMessage(new
            {
                type = "telemetryUpdate",
                payload = new
                {
                    position = new
                    {
                        latitude = e.Position.Latitude,
                        longitude = e.Position.Longitude,
                        altitude = e.Position.Altitude,
                        pitch = e.Position.Pitch,
                        bank = e.Position.Bank,
                        trueHeading = e.Position.TrueHeading,
                        magneticHeading = e.Position.MagneticHeading,
                        gearHandlePosition = e.Position.GearHandlePosition,
                        isOnGround = e.Position.IsOnGround != 0,
                        touchdownNormalVelocity = e.Position.TouchdownNormalVelocity,
                        throttleLeverPosition1 = e.Position.ThrottleLeverPosition1,
                        flapsHandleIndex = e.Position.FlapsHandleIndex
                    }
                }
            });
        }
    }

    private void Connector_Closed(object? sender, EventArgs e)
    {
        logger.LogDebug("Start reconnecting...");
        InitializeConnector();
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        if (stateMachine.CurrentState != StateMachine.State.End
            && stateMachine.CurrentState != StateMachine.State.Start)
        {
            e.Cancel = true;
            if (await stateMachine.TransitAsync(StateMachine.Event.Exit))
            {
                if (stateMachine.CurrentState == StateMachine.State.End)
                {
                    shortcutKeyLogic.Unregister(Handle);
                    Application.Current?.Shutdown();
                }
            }
        }
    }

    private IntPtr HandleHook(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
    {
        try
        {
            isHandled =
                connector.HandleWindowsEvent(message) ||
                shortcutKeyLogic.HandleWindowsEvent(message, wParam, lParam);
            return IntPtr.Zero;
        }
        catch (BadImageFormatException)
        {
            return IntPtr.Zero;
        }
    }

    private void InitializeConnector()
    {
        Dispatcher.Invoke(async () =>
        {
            while (true)
            {
                try
                {
                    connector.Initialize(Handle);
                    break;
                }
                catch (BadImageFormatException ex)
                {
                    logger.LogError(ex, "Cannot initialize SimConnect!");
                    MessageBox.Show("Cannot initialize SimConnect!", "Flight Recorder", MessageBoxButton.OK, MessageBoxImage.Error);
                    viewModel.SimConnectState = SimConnectState.Failed;
                    break;
                }
                catch (COMException ex)
                {
                    logger.LogTrace(ex, "SimConnect error.");
                    viewModel.SimConnectState = SimConnectState.NotConnected;
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        });
    }

    protected override void Draw()
    {
        SendRecordsToWeb();
    }

    private void ButtonReplayAI_Click(object sender, RoutedEventArgs e)
    {
        var window = CreateAIWindow();
        window.Owner = this;
        window.ShowInTaskbar = false;
        window.ShowWithData(viewModel.SimState?.AircraftTitle, viewModel.FileName, replayLogic.ToData(currentVersion));
    }

    private ServiceProvider GetServiceProvider() => (Application.Current as App)?.ServiceProvider ??
        throw new InvalidOperationException("ServiceProvider is not initialized!");

    private AIWindow CreateAIWindow() => windowFactory.CreateScopedWindow<AIWindow>(GetServiceProvider());

    private async Task ExportAsync(IExportLogic exportLogic)
    {
        var dialog = new SaveFileDialog
        {
            FileName = exportLogic.GetFileName(),
            Filter = exportLogic.GetFileFilter(),
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await exportLogic.ExportAsync(dialog.FileName, replayLogic.Records.Select(o =>
                {
                    var result = AircraftPosition.FromStruct(o.position);
                    result.Milliseconds = o.milliseconds;
                    return result;
                }));

                PostWebMessage(new
                {
                    type = "toast",
                    payload = $"Successfully exported to {Path.GetFileName(dialog.FileName)}"
                });

                logger.LogDebug("Save file into {fileName}", dialog.FileName);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Cannot export file.");
                MessageBox.Show("Flight Recorder cannot write the file to disk.\nPlease make sure the folder is accessible, and the file is not locked.", "Flight Recorder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public void RestoreWindow()
    {
        WindowState = WindowState.Normal;
        Activate();
    }

    private async Task CheckAndApplyUpdateAsync()
    {
        try
        {
            logger.LogInformation("Checking for new releases on GitHub...");
            var result = await autoUpdateService.CheckForUpdatesAsync(currentVersion);
            if (result.IsUpdateAvailable && !string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                logger.LogInformation("Automatic update detected: {NewVersion} (Current: {CurrentVersion})", result.LatestVersion, currentVersion);

                PostWebMessage(new
                {
                    type = "updateStatus",
                    payload = new
                    {
                        status = "downloading",
                        version = result.LatestVersion,
                        progress = 0
                    }
                });

                var progress = new Progress<int>(pct =>
                {
                    PostWebMessage(new
                    {
                        type = "updateProgress",
                        payload = new
                        {
                            version = result.LatestVersion,
                            progress = pct
                        }
                    });
                });

                var installerPath = await autoUpdateService.DownloadInstallerAsync(result.DownloadUrl, progress);

                PostWebMessage(new
                {
                    type = "updateStatus",
                    payload = new
                    {
                        status = "ready",
                        version = result.LatestVersion,
                        progress = 100
                    }
                });

                // Short delay so the user sees the confirmation
                await Task.Delay(2000);

                await Dispatcher.InvokeAsync(() =>
                {
                    autoUpdateService.ApplyUpdateAndRestart(installerPath);
                    Application.Current.Shutdown();
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Background auto-update routine encountered an error.");
        }
    }
}
