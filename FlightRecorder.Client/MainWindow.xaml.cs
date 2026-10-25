using FlightRecorder.Client.Logic;
using FlightRecorder.Client.Logics;
using FlightRecorder.Client.SimConnectMSFS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    private readonly IStorageLogic storageLogic;
    private readonly ISettingsLogic settingsLogic;
    private readonly IDialogLogic dialogLogic;
    private readonly string currentVersion;

    private IntPtr Handle;
    private readonly Stopwatch telemetryStopwatch = Stopwatch.StartNew();
    private long lastTelemetrySendTime = 0;
    private bool isWebViewReady = false;
    private CoreWebView2Environment? webViewEnvironment;
    private ReplayHudWindow? replayHudWindow;
    private bool autoRecordBelowAltitude = true;
    private int autoRecordAltitudeThreshold = 6000;
    private bool isArmedForAutoRecord = true;
    private string currentLanguage = "en";

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
        AutoUpdateService autoUpdateService,
        IStorageLogic storageLogic,
        ISettingsLogic settingsLogic,
        IDialogLogic dialogLogic
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
        this.storageLogic = storageLogic;
        this.settingsLogic = settingsLogic;
        this.dialogLogic = dialogLogic;

        stateMachine.StateChanged += StateMachine_StateChanged;
        connector.AircraftPositionUpdated += Connector_AircraftPositionUpdated;
        connector.Closed += Connector_Closed;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        shortcutKeyLogic.ToggleReplayHudRequested += (s, e) =>
        {
            Dispatcher.Invoke(async () => await ToggleReplayHudAsync());
        };

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

        autoRecordBelowAltitude = await settingsLogic.GetAutoRecordBelowAltitudeAsync();
        autoRecordAltitudeThreshold = await settingsLogic.GetAutoRecordAltitudeThresholdAsync();
        currentLanguage = await settingsLogic.GetLanguageAsync();
        if (dialogLogic is DialogLogic dl)
        {
            dl.CurrentLanguage = currentLanguage;
        }

        _ = Task.Run(() => VpilotService.EnsurePluginInstalled(logger));
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            webViewEnvironment = env;
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
            JsonDocument? innerDoc = null;
            if (root.ValueKind == JsonValueKind.String)
            {
                var innerStr = root.GetString();
                if (string.IsNullOrWhiteSpace(innerStr)) return;
                innerDoc = JsonDocument.Parse(innerStr);
                root = innerDoc.RootElement;
            }

            try
            {
                if (!root.TryGetProperty("action", out var actionProp)) return;
                var action = actionProp.GetString();

            switch (action)
            {
                case "uiReady":
                    isWebViewReady = true;
                    SendFullStateToWeb();
                    SendRecordsToWeb();
                    try
                    {
                        var initList = SavedFlightsLogic.GetSavedFlights(storageLogic);
                        PostWebMessage(new
                        {
                            type = "savedFlightsList",
                            payload = initList
                        });
                    }
                    catch { }
                    _ = Task.Run(async () => await SendSettingsToWebAsync());
                    _ = Task.Run(async () => await CheckAndApplyUpdateAsync());
                    break;

                case "record":
                    await stateMachine.TransitAsync(StateMachine.Event.Record);
                    break;

                case "stop":
                    await stateMachine.TransitAsync(StateMachine.Event.Stop);
                    break;

                case "replay":
                    if (await VpilotService.DisconnectIfConnectedAsync(logger))
                    {
                        PostWebMessage(new
                        {
                            type = "toast",
                            payload = (currentLanguage?.ToLowerInvariant() ?? "") switch
                            {
                                var l when l.StartsWith("pt") => "vPilot desconectado da rede VATSIM para iniciar o replay.",
                                var l when l.StartsWith("es") => "vPilot desconectado de la red VATSIM para iniciar la repetición.",
                                var l when l.StartsWith("fr") => "vPilot déconnecté du réseau VATSIM pour démarrer la relecture.",
                                _ => "vPilot disconnected from VATSIM network to start replay."
                            }
                        });
                    }
                    await stateMachine.TransitAsync(StateMachine.Event.Replay);
                    break;

                case "pause":
                    await stateMachine.TransitAsync(StateMachine.Event.Pause);
                    break;

                case "resume":
                    if (await VpilotService.DisconnectIfConnectedAsync(logger))
                    {
                        PostWebMessage(new
                        {
                            type = "toast",
                            payload = (currentLanguage?.ToLowerInvariant() ?? "") switch
                            {
                                var l when l.StartsWith("pt") => "vPilot desconectado da rede VATSIM para iniciar o replay.",
                                var l when l.StartsWith("es") => "vPilot desconectado de la red VATSIM para iniciar la repetición.",
                                var l when l.StartsWith("fr") => "vPilot déconnecté du réseau VATSIM para démarrer la relecture.",
                                _ => "vPilot disconnected from VATSIM network to start replay."
                            }
                        });
                    }
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
                    if (await stateMachine.TransitAsync(StateMachine.Event.Save))
                    {
                        SendRecordsToWeb();
                        PostWebMessage(new
                        {
                            type = "toast",
                            payload = "Voo gravado e salvo com sucesso em SavedFlights!"
                        });
                        try
                        {
                            var updatedList = SavedFlightsLogic.GetSavedFlights(storageLogic);
                            PostWebMessage(new
                            {
                                type = "savedFlightsList",
                                payload = updatedList
                            });
                        }
                        catch { }
                    }
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

                case "getSavedFlights":
                    {
                        var list = SavedFlightsLogic.GetSavedFlights(storageLogic);
                        PostWebMessage(new
                        {
                            type = "savedFlightsList",
                            payload = list
                        });
                    }
                    break;

                case "loadSavedFlight":
                    if (root.TryGetProperty("path", out var loadPathProp))
                    {
                        var filePath = loadPathProp.GetString();
                        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        {
                            using var stream = File.OpenRead(filePath);
                            var data = await storageLogic.LoadAsync(stream);
                            if (data != null)
                            {
                                replayLogic.FromData(Path.GetFileName(filePath), data);
                                await stateMachine.TransitAsync(StateMachine.Event.LoadDirect);
                                SendRecordsToWeb();
                                PostWebMessage(new
                                {
                                    type = "flightLoaded",
                                    payload = new
                                    {
                                        fileName = Path.GetFileName(filePath),
                                        aircraftTitle = data.StartState?.AircraftTitle ?? viewModel.AircraftTitle ?? "Flight",
                                        frameCount = data.Records.Count
                                    }
                                });
                                PostWebMessage(new
                                {
                                    type = "toast",
                                    payload = $"Voo {data.StartState?.AircraftTitle ?? Path.GetFileName(filePath)} carregado com sucesso!"
                                });
                            }
                            else
                            {
                                PostWebMessage(new
                                {
                                    type = "toast",
                                    payload = "Erro ao carregar voo: arquivo inválido."
                                });
                            }
                        }
                    }
                    break;

                case "deleteSavedFlight":
                    if (root.TryGetProperty("path", out var delPathProp))
                    {
                        var delPath = delPathProp.GetString();
                        if (!string.IsNullOrEmpty(delPath))
                        {
                            SavedFlightsLogic.DeleteSavedFlight(delPath);
                            var list = SavedFlightsLogic.GetSavedFlights(storageLogic);
                            PostWebMessage(new
                            {
                                type = "savedFlightsList",
                                payload = list
                            });
                            PostWebMessage(new
                            {
                                type = "toast",
                                payload = "Voo excluído com sucesso."
                            });
                        }
                    }
                    break;

                case "openSavedFlightsFolder":
                    {
                        var dir = SavedFlightsLogic.GetSavedFlightsDirectory();
                        Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
                    }
                    break;

                case "importSavedFlight":
                    {
                        var dlg = new Microsoft.Win32.OpenFileDialog
                        {
                            Title = "Importar Arquivo de Voo",
                            Filter = "Flight Recorder Files (*.fltrec;*.flightrecorder)|*.fltrec;*.flightrecorder|All Files (*.*)|*.*"
                        };
                        if (dlg.ShowDialog() == true)
                        {
                            var destDir = SavedFlightsLogic.GetSavedFlightsDirectory();
                            var destFile = Path.Combine(destDir, Path.GetFileName(dlg.FileName));
                            if (!File.Exists(destFile) || dlg.FileName != destFile)
                            {
                                File.Copy(dlg.FileName, destFile, true);
                            }
                            using var stream = File.OpenRead(destFile);
                            var data = await storageLogic.LoadAsync(stream);
                            if (data != null)
                            {
                                await SavedFlightsLogic.SaveMetadataAsync(destFile, data);
                                replayLogic.FromData(Path.GetFileName(destFile), data);
                                await stateMachine.TransitAsync(StateMachine.Event.LoadDirect);
                                SendRecordsToWeb();
                                PostWebMessage(new
                                {
                                    type = "flightLoaded",
                                    payload = new
                                    {
                                        fileName = Path.GetFileName(destFile),
                                        aircraftTitle = data.StartState?.AircraftTitle ?? viewModel.AircraftTitle ?? "Flight",
                                        frameCount = data.Records.Count
                                    }
                                });
                                var updated = SavedFlightsLogic.GetSavedFlights(storageLogic);
                                PostWebMessage(new
                                {
                                    type = "savedFlightsList",
                                    payload = updated
                                });
                                PostWebMessage(new
                                {
                                    type = "toast",
                                    payload = "Voo importado e carregado com sucesso!"
                                });
                            }
                        }
                    }
                    break;

                case "getSettings":
                    await SendSettingsToWebAsync();
                    break;

                case "saveShortcutSettings":
                    if (root.TryGetProperty("enabled", out var enabledProp))
                    {
                        await settingsLogic.SetShortcutKeysEnabledAsync(enabledProp.GetBoolean());
                    }

                    if (root.TryGetProperty("shortcuts", out var shortcutsProp) && shortcutsProp.ValueKind == JsonValueKind.Array)
                    {
                        var dict = new Dictionary<Shortcuts, ShortcutKey>();
                        foreach (var item in shortcutsProp.EnumerateArray())
                        {
                            if (item.TryGetProperty("id", out var idProp) && Enum.TryParse<Shortcuts>(idProp.GetString(), out var shortcutEnum))
                            {
                                var ctrl = item.TryGetProperty("ctrl", out var c) && c.GetBoolean();
                                var alt = item.TryGetProperty("alt", out var a) && a.GetBoolean();
                                var shift = item.TryGetProperty("shift", out var s) && s.GetBoolean();
                                var key = item.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                                var vk = item.TryGetProperty("virtualKey", out var v) ? v.GetUInt32() : 0u;

                                dict[shortcutEnum] = new ShortcutKey(ctrl, alt, shift, key, vk);
                            }
                        }
                        await settingsLogic.SetShortcutKeysAsync(dict);
                    }

                    shortcutKeyLogic.Unregister(Handle);
                    await shortcutKeyLogic.RegisterAsync(Handle);
                    await SendSettingsToWebAsync();

                    PostWebMessage(new
                    {
                        type = "toast",
                        payload = "Atalhos atualizados e salvos com sucesso!"
                    });
                    break;

                case "restoreDefaultShortcuts":
                    await settingsLogic.SetShortcutKeysAsync(new Dictionary<Shortcuts, ShortcutKey>());
                    shortcutKeyLogic.Unregister(Handle);
                    await shortcutKeyLogic.RegisterAsync(Handle);
                    await SendSettingsToWebAsync();
                    PostWebMessage(new
                    {
                        type = "toast",
                        payload = "Atalhos restaurados para o padrão."
                    });
                    break;

                case "openReplayHud":
                    _ = Dispatcher.Invoke(async () => await OpenReplayHudAsync());
                    break;

                case "saveHudSettings":
                    if (root.TryGetProperty("autoHide", out var hudAutoHideProp))
                    {
                        var ah = hudAutoHideProp.GetBoolean();
                        await settingsLogic.SetHudAutoHideAsync(ah);
                    }
                    if (root.TryGetProperty("translucent", out var hudTranslucentProp))
                    {
                        var tr = hudTranslucentProp.GetBoolean();
                        await settingsLogic.SetHudTranslucentAsync(tr);
                    }
                    var currentAutoHide = await settingsLogic.GetHudAutoHideAsync();
                    var currentTranslucent = await settingsLogic.GetHudTranslucentAsync();
                    replayHudWindow?.SetSettings(currentAutoHide, currentTranslucent);
                    await SendSettingsToWebAsync();
                    break;

                case "saveAutoRecordAltitudeSettings":
                    if (root.TryGetProperty("enabled", out var autoRecProp))
                    {
                        autoRecordBelowAltitude = autoRecProp.GetBoolean();
                        await settingsLogic.SetAutoRecordBelowAltitudeAsync(autoRecordBelowAltitude);
                        if (autoRecordBelowAltitude)
                        {
                            isArmedForAutoRecord = true;
                        }
                    }
                    if (root.TryGetProperty("threshold", out var threshProp))
                    {
                        autoRecordAltitudeThreshold = Math.Clamp(threshProp.GetInt32(), 1000, 15000);
                        await settingsLogic.SetAutoRecordAltitudeThresholdAsync(autoRecordAltitudeThreshold);
                    }
                    await SendSettingsToWebAsync();
                    break;

                case "setLanguage":
                    if (root.TryGetProperty("language", out var langProp))
                    {
                        var lang = langProp.GetString() ?? "en";
                        currentLanguage = lang;
                        if (dialogLogic is DialogLogic dl)
                        {
                            dl.CurrentLanguage = lang;
                        }
                        await settingsLogic.SetLanguageAsync(lang);
                        PostWebMessage(new
                        {
                            type = "languageChanged",
                            payload = lang
                        });
                        await SendSettingsToWebAsync();
                    }
                    break;

                case "themeChanged":
                    if (root.TryGetProperty("effectiveTheme", out var effThemeProp))
                    {
                        var eff = effThemeProp.GetString();
                        var isLight = eff == "light";
                        Dispatcher.Invoke(() =>
                        {
                            var brush = isLight
                                ? (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf4, 0xf4, 0xf5))
                                : (System.Windows.Media.Brush)new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x09, 0x09, 0x0b));
                            Background = brush;
                            if (webView != null)
                            {
                                webView.DefaultBackgroundColor = isLight
                                    ? System.Drawing.Color.FromArgb(244, 244, 245)
                                    : System.Drawing.Color.FromArgb(9, 9, 11);
                            }
                            replayHudWindow?.ApplyTheme(isLight);
                        });
                    }
                    break;
                }
            }
            finally
            {
                innerDoc?.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing web message from WebView2.");
        }
    }

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private void PostWebMessage(object data)
    {
        if (replayHudWindow != null)
        {
            try
            {
                replayHudWindow.PostWebMessage(data);
            }
            catch { }
        }

        if (!isWebViewReady) return;

        Dispatcher.Invoke(() =>
        {
            try
            {
                if (webView?.CoreWebView2 == null) return;
                var json = JsonSerializer.Serialize(data, jsonOptions);
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
            payload = new
            {
                records = recordsList
            }
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
        if (e.To == StateMachine.State.Recording)
        {
            isArmedForAutoRecord = false;
        }
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

        if (autoRecordBelowAltitude)
        {
            var alt = e.Position.Altitude;
            if (alt >= autoRecordAltitudeThreshold + 500)
            {
                // Aircraft climbed above threshold + 500ft hysteresis -> re-arm auto-record
                isArmedForAutoRecord = true;
            }
            else if (isArmedForAutoRecord && alt < autoRecordAltitudeThreshold && !double.IsNaN(alt))
            {
                if (stateMachine.CurrentState == StateMachine.State.IdleEmpty ||
                    stateMachine.CurrentState == StateMachine.State.IdleSaved)
                {
                    isArmedForAutoRecord = false;
                    _ = Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            logger.LogInformation("Auto-record triggered: aircraft altitude is {Altitude:F0} ft (threshold: {Threshold} ft)", alt, autoRecordAltitudeThreshold);
                            if (await stateMachine.TransitAsync(StateMachine.Event.Record))
                            {
                                PostWebMessage(new
                                {
                                    type = "toast",
                                    payload = $"Gravação iniciada automaticamente (Altitude: {Math.Round(alt):N0} ft)"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error while starting auto-record.");
                        }
                    });
                }
            }
        }

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
        try
        {
            replayHudWindow?.Close();
        }
        catch { }

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

                if (autoUpdateService.IsDevelopmentEnvironment())
                {
                    logger.LogInformation("Running in development/source directory. Auto-restart skipped to allow local testing.");
                    PostWebMessage(new
                    {
                        type = "toast",
                        payload = $"[Modo Dev] Nova versão {result.LatestVersion} baixada. Reinício automático desativado."
                    });
                    return;
                }

                autoUpdateService.RecordUpdateAttempt(result.LatestVersion);

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

    private async Task SendSettingsToWebAsync()
    {
        try
        {
            var enabled = await settingsLogic.IsShortcutKeysEnabledAsync();
            var shortcuts = await shortcutKeyLogic.GetShortcutKeysAsync();
            var shortcutList = shortcuts.Select(kv => new
            {
                id = kv.Key.ToString(),
                name = GetShortcutDisplayName(kv.Key),
                desc = GetShortcutDescription(kv.Key),
                ctrl = kv.Value.Ctrl,
                alt = kv.Value.Alt,
                shift = kv.Value.Shift,
                key = kv.Value.Key,
                virtualKey = kv.Value.VirtualKey
            }).ToList();

            var stayOnTop = Dispatcher.Invoke(() => Topmost);
            var hudAutoHide = await settingsLogic.GetHudAutoHideAsync();
            var hudTranslucent = await settingsLogic.GetHudTranslucentAsync();
            autoRecordBelowAltitude = await settingsLogic.GetAutoRecordBelowAltitudeAsync();
            autoRecordAltitudeThreshold = await settingsLogic.GetAutoRecordAltitudeThresholdAsync();
            currentLanguage = await settingsLogic.GetLanguageAsync();
            if (dialogLogic is DialogLogic dl)
            {
                dl.CurrentLanguage = currentLanguage;
            }

            PostWebMessage(new
            {
                type = "settingsData",
                payload = new
                {
                    shortcutKeysEnabled = enabled,
                    shortcuts = shortcutList,
                    savedFlightsFolder = SavedFlightsLogic.GetSavedFlightsDirectory(),
                    stayOnTop = stayOnTop,
                    hudAutoHide = hudAutoHide,
                    hudTranslucent = hudTranslucent,
                    autoRecordBelowAltitude = autoRecordBelowAltitude,
                    autoRecordAltitudeThreshold = autoRecordAltitudeThreshold,
                    language = currentLanguage
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error sending settings to web.");
        }
    }

    private async Task OpenReplayHudAsync()
    {
        var autoHide = await settingsLogic.GetHudAutoHideAsync();
        var translucent = await settingsLogic.GetHudTranslucentAsync();

        if (replayHudWindow == null || !replayHudWindow.IsLoaded)
        {
            replayHudWindow = new ReplayHudWindow(stateMachine, replayLogic, viewModel, webViewEnvironment, this, autoHide, translucent);
            replayHudWindow.Closed += (s, args) => replayHudWindow = null;
            replayHudWindow.Show();
        }
        else
        {
            replayHudWindow.Activate();
            replayHudWindow.Topmost = true;
            replayHudWindow.SetSettings(autoHide, translucent);
            replayHudWindow.RevealNow();
        }
        replayHudWindow.SendStateUpdate();
    }

    private async Task ToggleReplayHudAsync()
    {
        if (replayHudWindow == null || !replayHudWindow.IsLoaded)
        {
            await OpenReplayHudAsync();
        }
        else
        {
            replayHudWindow.Close();
            replayHudWindow = null;
        }
    }

    private string GetShortcutDisplayName(Shortcuts shortcut)
    {
        string lang = currentLanguage?.ToLowerInvariant() ?? "en";
        if (lang.StartsWith("pt"))
        {
            return shortcut switch
            {
                Shortcuts.Record => "Gravar Voo (Record)",
                Shortcuts.StopRecording => "Parar Gravação (Stop)",
                Shortcuts.Replay => "Replay do Voo (Play)",
                Shortcuts.Pause => "Pausar Replay (Pause)",
                Shortcuts.Resume => "Continuar Replay (Resume)",
                Shortcuts.StopReplay => "Parar Replay (Stop Replay)",
                Shortcuts.SaveToDisk => "Salvar Voo Rápido (Quick Save)",
                Shortcuts.ToggleReplayHud => "Barra de Replay (HUD)",
                _ => shortcut.ToString()
            };
        }
        if (lang.StartsWith("es"))
        {
            return shortcut switch
            {
                Shortcuts.Record => "Grabar Vuelo (Record)",
                Shortcuts.StopRecording => "Detener Grabación (Stop)",
                Shortcuts.Replay => "Reproducir Vuelo (Play)",
                Shortcuts.Pause => "Pausar Repetición (Pause)",
                Shortcuts.Resume => "Reanudar Repetición (Resume)",
                Shortcuts.StopReplay => "Detener Repetición (Stop)",
                Shortcuts.SaveToDisk => "Guardado Rápido (Quick Save)",
                Shortcuts.ToggleReplayHud => "Barra de Repetición (HUD)",
                _ => shortcut.ToString()
            };
        }
        if (lang.StartsWith("fr"))
        {
            return shortcut switch
            {
                Shortcuts.Record => "Enregistrer le Vol (Record)",
                Shortcuts.StopRecording => "Arrêter l'Enregistrement (Stop)",
                Shortcuts.Replay => "Lecture du Vol (Play)",
                Shortcuts.Pause => "Mettre en Pause (Pause)",
                Shortcuts.Resume => "Reprendre la Lecture (Resume)",
                Shortcuts.StopReplay => "Arrêter la Relecture (Stop)",
                Shortcuts.SaveToDisk => "Sauvegarde Rapide (Quick Save)",
                Shortcuts.ToggleReplayHud => "Barre de Relecture (HUD)",
                _ => shortcut.ToString()
            };
        }
        return shortcut switch
        {
            Shortcuts.Record => "Record Flight (Record)",
            Shortcuts.StopRecording => "Stop Recording (Stop)",
            Shortcuts.Replay => "Replay Flight (Play)",
            Shortcuts.Pause => "Pause Replay (Pause)",
            Shortcuts.Resume => "Resume Replay (Resume)",
            Shortcuts.StopReplay => "Stop Replay (Stop Replay)",
            Shortcuts.SaveToDisk => "Quick Save Flight (Save)",
            Shortcuts.ToggleReplayHud => "Replay Bar (HUD)",
            _ => shortcut.ToString()
        };
    }

    private string GetShortcutDescription(Shortcuts shortcut)
    {
        string lang = currentLanguage?.ToLowerInvariant() ?? "en";
        if (lang.StartsWith("pt"))
        {
            return shortcut switch
            {
                Shortcuts.Record => "Inicia a gravação das posições do voo",
                Shortcuts.StopRecording => "Interrompe a gravação atual",
                Shortcuts.Replay => "Inicia a reprodução do voo gravado",
                Shortcuts.Pause => "Congela a reprodução do voo no simulador",
                Shortcuts.Resume => "Retoma a reprodução pausada",
                Shortcuts.StopReplay => "Encerra o replay e devolve os controles do simulador",
                Shortcuts.SaveToDisk => "Salva o voo gravado diretamente na pasta SavedFlights",
                Shortcuts.ToggleReplayHud => "Abre ou fecha o popup flutuante da barra de controle de replay",
                _ => ""
            };
        }
        if (lang.StartsWith("es"))
        {
            return shortcut switch
            {
                Shortcuts.Record => "Inicia la grabación de las posiciones de vuelo",
                Shortcuts.StopRecording => "Detiene la grabación activa",
                Shortcuts.Replay => "Inicia la reproducción del vuelo grabado",
                Shortcuts.Pause => "Congela la reproducción del vuelo en el simulador",
                Shortcuts.Resume => "Reanuda la reproducción pausada",
                Shortcuts.StopReplay => "Finaliza la repetición y devuelve los controles del simulador",
                Shortcuts.SaveToDisk => "Guarda la grabación directamente en la carpeta SavedFlights",
                Shortcuts.ToggleReplayHud => "Muestra u oculta el mini-reproductor flotante de repetición",
                _ => ""
            };
        }
        if (lang.StartsWith("fr"))
        {
            return shortcut switch
            {
                Shortcuts.Record => "Démarre l'enregistrement des positions de vol",
                Shortcuts.StopRecording => "Arrête l'enregistrement actif",
                Shortcuts.Replay => "Démarre la lecture du vol enregistré",
                Shortcuts.Pause => "Fige la lecture du vol dans le simulateur",
                Shortcuts.Resume => "Reprend la lecture en pause",
                Shortcuts.StopReplay => "Met fin à la relecture et restaure les commandes du simulateur",
                Shortcuts.SaveToDisk => "Enregistre le vol directement dans le dossier SavedFlights",
                Shortcuts.ToggleReplayHud => "Affiche ou masque le mini-lecteur flottant de relecture",
                _ => ""
            };
        }
        return shortcut switch
        {
            Shortcuts.Record => "Starts recording aircraft flight positions",
            Shortcuts.StopRecording => "Stops the active recording",
            Shortcuts.Replay => "Starts playback of the recorded flight",
            Shortcuts.Pause => "Freezes simulator replay playback",
            Shortcuts.Resume => "Resumes paused playback",
            Shortcuts.StopReplay => "Ends replay and restores simulator controls",
            Shortcuts.SaveToDisk => "Saves recording directly to SavedFlights folder",
            Shortcuts.ToggleReplayHud => "Shows or hides the floating replay mini-player",
            _ => ""
        };
    }
}

