using FlightRecorder.Client.Logics;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FlightRecorder.Client;

public partial class ReplayHudWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private readonly IStateMachine stateMachine;
    private readonly IReplayLogic replayLogic;
    private readonly MainViewModel viewModel;
    private readonly CoreWebView2Environment? environment;
    private readonly MainWindow mainWindow;
    private IntPtr windowHandle = IntPtr.Zero;
    private bool isLoaded = false;

    private bool autoHideEnabled;
    private bool translucentEnabled;
    private bool isCurrentlyVisible = true;
    private double TargetVisibleOpacity => translucentEnabled ? 0.70 : 1.0;

    private DispatcherTimer? animTimer;
    private DispatcherTimer? hoverMonitorTimer;
    private DateTime lastHoverTime = DateTime.MinValue;
    private DateTime shownTime = DateTime.UtcNow;

    public ReplayHudWindow(
        IStateMachine stateMachine,
        IReplayLogic replayLogic,
        MainViewModel viewModel,
        CoreWebView2Environment? environment,
        MainWindow mainWindow,
        bool autoHide = false,
        bool translucent = false)
    {
        // Tell WebView2 environment to initialize with transparent background
        Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");

        InitializeComponent();

        this.stateMachine = stateMachine;
        this.replayLogic = replayLogic;
        this.viewModel = viewModel;
        this.environment = environment;
        this.mainWindow = mainWindow;
        this.autoHideEnabled = autoHide;
        this.translucentEnabled = translucent;

        Opacity = TargetVisibleOpacity;
        shownTime = DateTime.UtcNow;
        lastHoverTime = DateTime.UtcNow;

        // Position horizontally centered flush at top of the primary screen
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = Math.Max(0, (screenWidth - Width) / 2);
        Top = 0;

        Loaded += ReplayHudWindow_Loaded;
        Closed += (s, e) =>
        {
            hoverMonitorTimer?.Stop();
            hoverMonitorTimer = null;
            animTimer?.Stop();
            animTimer = null;
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            windowHandle = new WindowInteropHelper(this).Handle;
        }
        catch { }
    }

    private void StartOpacityAnimation(double target)
    {
        animTimer?.Stop();
        animTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16) // 60 FPS
        };
        animTimer.Tick += (s, e) =>
        {
            var diff = target - Opacity;
            if (Math.Abs(diff) <= 0.02)
            {
                Opacity = target;
                animTimer.Stop();
                animTimer = null;
            }
            else
            {
                Opacity += diff * 0.25;
            }
        };
        animTimer.Start();
    }

    private void SetHudVisible(bool visible)
    {
        if (isCurrentlyVisible == visible) return;
        isCurrentlyVisible = visible;

        StartOpacityAnimation(visible ? TargetVisibleOpacity : 0.0);

        PostWebMessage(new
        {
            type = "hudVisibilityUpdate",
            payload = new { visible = visible }
        });
    }

    private bool CheckMouseInside()
    {
        if (!GetCursorPos(out var pt)) return false;
        try
        {
            Point topLeft = PointToScreen(new Point(0, 0));
            double w = ActualWidth > 0 ? ActualWidth : Width;
            double h = ActualHeight > 0 ? ActualHeight : Height;
            Point bottomRight = PointToScreen(new Point(w, h));

            if (pt.X >= topLeft.X - 14 && pt.X <= bottomRight.X + 14 &&
                pt.Y >= topLeft.Y - 12 && pt.Y <= bottomRight.Y + 16)
            {
                return true;
            }

            IntPtr under = WindowFromPoint(pt);
            if (under == windowHandle || (windowHandle != IntPtr.Zero && IsChild(windowHandle, under)))
            {
                return true;
            }

            return false;
        }
        catch
        {
            return (pt.X >= Left - 14 && pt.X <= Left + Width + 14 &&
                    pt.Y >= Top - 12 && pt.Y <= Top + Height + 16);
        }
    }

    private void SetupHoverMonitor()
    {
        hoverMonitorTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(50) // 20 checks/sec
        };
        hoverMonitorTimer.Tick += HoverMonitorTimer_Tick;
        hoverMonitorTimer.Start();
    }

    private void HoverMonitorTimer_Tick(object? sender, EventArgs e)
    {
        if (!autoHideEnabled)
        {
            if (!isCurrentlyVisible) SetHudVisible(true);
            return;
        }

        // If mouse button is held down (user dragging, clicking buttons, or scrubbing slider), keep visible
        bool isMouseDown = (GetAsyncKeyState(0x01) & 0x8000) != 0; // VK_LBUTTON
        if (isMouseDown)
        {
            lastHoverTime = DateTime.UtcNow;
            SetHudVisible(true);
            return;
        }

        bool isInside = CheckMouseInside();

        if (isInside)
        {
            lastHoverTime = DateTime.UtcNow;
            SetHudVisible(true);
        }
        else
        {
            var elapsedSinceShown = (DateTime.UtcNow - shownTime).TotalMilliseconds;
            var elapsedSinceHover = (DateTime.UtcNow - lastHoverTime).TotalMilliseconds;

            if (elapsedSinceShown > 2000 && elapsedSinceHover > 600)
            {
                SetHudVisible(false);
            }
        }
    }

    private async void ReplayHudWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            hudWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            if (environment != null)
            {
                await hudWebView.EnsureCoreWebView2Async(environment);
            }
            else
            {
                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlightRecorder", "WebView2");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await hudWebView.EnsureCoreWebView2Async(env);
            }

            hudWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            var wwwrootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            hudWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.flightrecorder",
                wwwrootDir,
                CoreWebView2HostResourceAccessKind.Allow
            );

            hudWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            hudWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            hudWebView.WebMessageReceived += HudWebView_WebMessageReceived;
            hudWebView.Source = new Uri("https://app.flightrecorder/hud.html");
            isLoaded = true;

            SetupHoverMonitor();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao inicializar mini player: {ex.Message}", "Flight Recorder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void HudWebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var rawJson = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                using var innerDoc = JsonDocument.Parse(root.GetString()!);
                root = innerDoc.RootElement.Clone();
            }

            if (!root.TryGetProperty("action", out var actionProp)) return;
            var action = actionProp.GetString();

            switch (action)
            {
                case "hudReady":
                    SendStateUpdate();
                    PostWebMessage(new
                    {
                        type = "hudConfigUpdate",
                        payload = new { autoHide = autoHideEnabled, translucent = translucentEnabled }
                    });
                    SetHudVisible(true);
                    break;

                case "mouseEnter":
                    lastHoverTime = DateTime.UtcNow;
                    SetHudVisible(true);
                    break;

                case "dragWindow":
                    Dispatcher.Invoke(() =>
                    {
                        var helper = new WindowInteropHelper(this);
                        ReleaseCapture();
                        SendMessage(helper.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                    });
                    break;

                case "close":
                    Close();
                    break;

                case "play":
                    if (await VpilotService.DisconnectIfConnectedAsync())
                    {
                        PostWebMessage(new
                        {
                            type = "toast",
                            payload = "vPilot desconectado da rede VATSIM para iniciar o replay."
                        });
                    }
                    await stateMachine.TransitAsync(StateMachine.Event.Replay);
                    break;

                case "pause":
                    await stateMachine.TransitAsync(StateMachine.Event.Pause);
                    break;

                case "resume":
                    await stateMachine.TransitAsync(StateMachine.Event.Resume);
                    break;

                case "stop":
                    await stateMachine.TransitAsync(StateMachine.Event.RequestStopping);
                    break;

                case "seek":
                    if (root.TryGetProperty("frame", out var frameProp))
                    {
                        var frame = frameProp.GetInt32();
                        replayLogic.Seek(frame);
                    }
                    break;

                case "step":
                    if (root.TryGetProperty("delta", out var deltaProp))
                    {
                        var delta = deltaProp.GetInt32();
                        var target = Math.Max(0, Math.Min(viewModel.FrameCount - 1, viewModel.CurrentFrame + delta));
                        replayLogic.Seek(target);
                    }
                    break;
            }
        }
        catch { }
    }

    public void RevealNow()
    {
        shownTime = DateTime.UtcNow;
        lastHoverTime = DateTime.UtcNow;
        SetHudVisible(true);
    }

    public void SetSettings(bool autoHide, bool translucent)
    {
        autoHideEnabled = autoHide;
        translucentEnabled = translucent;

        Dispatcher.Invoke(() =>
        {
            if (!autoHideEnabled)
            {
                isCurrentlyVisible = true;
                StartOpacityAnimation(TargetVisibleOpacity);
            }
            else
            {
                bool isInside = CheckMouseInside();
                isCurrentlyVisible = isInside;
                StartOpacityAnimation(isInside ? TargetVisibleOpacity : 0.0);
            }

            PostWebMessage(new
            {
                type = "hudConfigUpdate",
                payload = new { autoHide = autoHideEnabled, translucent = translucentEnabled }
            });
        });
    }

    public void ApplyTheme(bool isLight)
    {
        Dispatcher.Invoke(() =>
        {
            PostWebMessage(new
            {
                type = "themeChanged",
                payload = new { effectiveTheme = isLight ? "light" : "dark" }
            });
        });
    }

    public void PostWebMessage(object message)
    {
        if (!isLoaded) return;

        Dispatcher.Invoke(() =>
        {
            try
            {
                if (hudWebView?.CoreWebView2 != null)
                {
                    var json = JsonSerializer.Serialize(message);
                    hudWebView.CoreWebView2.PostWebMessageAsJson(json);
                }
            }
            catch { }
        });
    }

    public void SendStateUpdate()
    {
        PostWebMessage(new
        {
            type = "stateUpdate",
            payload = new
            {
                currentFrame = viewModel.CurrentFrame,
                frameCount = viewModel.FrameCount,
                machineState = stateMachine.CurrentState.ToString()
            }
        });
    }
}
