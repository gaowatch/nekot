using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.ViewModels;
using Res = NekoT.Desktop.Resources.Strings;
using NekoT.Desktop.Views;

namespace NekoT.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private SettingsWindow? _settingsWindow;
    private DispatcherTimer? _glowAnimationTimer;
    private double _glowAngle = 0;
    private TrayIcon? _trayIcon;
    private bool _isClosing;

    private static readonly string LogFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "wv2_debug.log");

    private static readonly object LogLock = new object();

    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try
        {
            lock (LogLock)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine, System.Text.Encoding.UTF8);
            }
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogError] IO: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogError] Access: {ex.Message}");
        }
    }

    public MainWindow() : this(App.Services.GetRequiredService<MainViewModel>()) { }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        Log("[INIT] MainWindow constructor started");
        WindowIconHelper.RemoveIcon(this);
        Log("[INIT] Window icon removal configured");
        _viewModel = viewModel;
        DataContext = _viewModel;
        Log("[INIT] ViewModel injected via DI");
        _viewModel.Tabs.CollectionChanged += OnTabsCollectionChanged;
        StartGlowAnimation();
        ApplyStartupSettings();
        SetupTrayIcon();
    }

    private void OnTabsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }

    private void OnMainWindowLoaded(object? sender, RoutedEventArgs e)
    {
        Log("[Window] MainWindow loaded event triggered");
    }

    private void ApplyStartupSettings()
    {
        Log("[Window] ApplyStartupSettings called");
        var settings = UserSettingsService.Instance;
        Log($"[Window] Current WindowState: {WindowState}, StartMinimized: {settings.StartMinimized}, StartMaximized: {settings.StartMaximized}");

        if (settings.StartMinimized)
        {
            Log("[Window] Starting minimized, skipping window show");
        }
        else if (settings.StartMaximized)
        {
            Log("[Window] Starting maximized");
            Dispatcher.UIThread.Post(() =>
            {
                WindowState = WindowState.Maximized;
                EnsureWindowVisible();
            }, DispatcherPriority.Input);
        }
        else
        {
            Log("[Window] Starting normal, ensuring window visible");
            Dispatcher.UIThread.Post(() => { EnsureWindowVisible(); }, DispatcherPriority.Input);
        }
        SystemFeaturesHelper.ApplyStartupSettings();
    }

    private void EnsureWindowVisible()
    {
        try
        {
            Log("[Window] EnsureWindowVisible called");
            EnsureWindowOnPrimaryScreen();
            if (WindowState == WindowState.Minimized)
            {
                Log("[Window] Changing WindowState from Minimized to Normal");
                WindowState = WindowState.Normal;
            }
            if (!IsVisible)
            {
                Log("[Window] Window not visible, calling Show()");
                Show();
            }
            Log("[Window] Activating window");
            Activate();
            Topmost = true;
            Topmost = false;
            Log("[Window] EnsureWindowVisible completed successfully");
        }
        catch (Exception ex)
        {
            Log($"[Window] Error in EnsureWindowVisible: {ex.Message}");
        }
    }

    private void EnsureWindowOnPrimaryScreen()
    {
        try
        {
            if (WindowState == WindowState.Maximized)
            {
                Log("[Window] Window is maximized, skipping position adjustment");
                return;
            }
            if (Screens.Primary != null)
            {
                var primaryScreen = Screens.Primary;
                var workArea = primaryScreen.WorkingArea;
                var windowWidth = Width;
                var windowHeight = Height;
                if (windowWidth > workArea.Width * 0.9) windowWidth = workArea.Width * 0.9;
                if (windowHeight > workArea.Height * 0.9) windowHeight = workArea.Height * 0.9;
                Position = new PixelPoint((int)(workArea.X + (workArea.Width - windowWidth) / 2), (int)(workArea.Y + (workArea.Height - windowHeight) / 2));
                Width = windowWidth;
                Height = windowHeight;
                Log($"[Window] Window positioned on primary screen: {Position}, size: {Width}x{Height}");
            }
        }
        catch (Exception ex)
        {
            Log($"[Window] Error positioning window: {ex.Message}");
        }
    }

    private void SetupTrayIcon()
    {
        var settings = UserSettingsService.Instance;
        if (!settings.MinimizeToTray) return;
        try
        {
            var showWindowMenuItem = new NativeMenuItem(Res.Tray_ShowWindow);
            showWindowMenuItem.Click += (s, e) => ShowMainWindow();
            var exitMenuItem = new NativeMenuItem(Res.Tray_Exit);
            exitMenuItem.Click += (s, e) => ShutdownApp();
            _trayIcon = new TrayIcon();
            _trayIcon.Menu = new NativeMenu();
            _trayIcon.Menu.Items.Add(showWindowMenuItem);
            _trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
            _trayIcon.Menu.Items.Add(exitMenuItem);
            _trayIcon.ToolTipText = "NekoT - AI Token Monitor";
            using (var stream = AssetLoader.Open(new Uri("avares://NekoT.Desktop/Assets/nekotlogo.png")))
            {
                _trayIcon.Icon = new WindowIcon(stream);
            }
            _trayIcon.Clicked += (s, e) => ShowMainWindow();
        }
        catch (Exception ex)
        {
            Log($"[INIT] Failed to setup tray icon: {ex.Message}");
        }
    }

    private void ShowMainWindow()
    {
        Dispatcher.UIThread.Post(() => { Show(); WindowState = WindowState.Normal; Activate(); });
    }

    private void ShutdownApp()
    {
        _isClosing = true;
        _ = CleanupBeforeExitAsync();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private async Task CleanupBeforeExitAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Cleaning up before exit...");
            if (DataContext is MainViewModel mainViewModel)
            {
                var forwardingService = mainViewModel.ForwardingService;
                if (forwardingService != null && !forwardingService.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Disposing ForwardingServiceViewModel...");
                    await forwardingService.DisposeAsync();
                }
            }
            System.Diagnostics.Debug.WriteLine("[MainWindow] Cleanup completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Cleanup error: {ex.Message}");
        }
    }

    private void CleanupBeforeExit()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Cleaning up before exit...");
            if (DataContext is MainViewModel mainViewModel)
            {
                var forwardingService = mainViewModel.ForwardingService;
                if (forwardingService != null && !forwardingService.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Disposing ForwardingServiceViewModel...");
                    forwardingService.Dispose();
                }
            }
            System.Diagnostics.Debug.WriteLine("[MainWindow] Cleanup completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Cleanup error: {ex.Message}");
        }
    }

    private void StartGlowAnimation()
    {
        _glowAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _glowAnimationTimer.Tick += OnGlowAnimationTick;
        _glowAnimationTimer.Start();
    }

    private void OnGlowAnimationTick(object? sender, EventArgs e)
    {
        _glowAngle += 1.5;
        if (_glowAngle >= 360) _glowAngle = 0;
        var glowBorder = this.FindControl<Border>("LogoGlowBorder");
        if (glowBorder != null)
        {
            var angleRad = _glowAngle * Math.PI / 180;
            var startX = 0.5 + 0.5 * Math.Cos(angleRad);
            var startY = 0.5 + 0.5 * Math.Sin(angleRad);
            var endX = 0.5 - 0.5 * Math.Cos(angleRad);
            var endY = 0.5 - 0.5 * Math.Sin(angleRad);
            var gradient = new LinearGradientBrush { StartPoint = new RelativePoint(startX, startY, RelativeUnit.Relative), EndPoint = new RelativePoint(endX, endY, RelativeUnit.Relative) };
            var pulseIntensity = 0.7 + 0.3 * Math.Sin(_glowAngle * Math.PI / 180 * 2);
            var centerAlpha = (byte)(170 * pulseIntensity);
            var sideAlpha = (byte)(100 * pulseIntensity);
            gradient.GradientStops.Add(new GradientStop(Color.Parse("#00FFFFFF"), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromUInt32((uint)(sideAlpha << 24 | 0xFFFFFF)), 0.25));
            gradient.GradientStops.Add(new GradientStop(Color.FromUInt32((uint)(centerAlpha << 24 | 0xFFFFFF)), 0.5));
            gradient.GradientStops.Add(new GradientStop(Color.FromUInt32((uint)(sideAlpha << 24 | 0xFFFFFF)), 0.75));
            gradient.GradientStops.Add(new GradientStop(Color.Parse("#00FFFFFF"), 1.0));
            glowBorder.Background = gradient;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            var activeTaskCount = GetActiveTaskCount();
            var dialog = new CloseConfirmationDialog { ActiveTaskCount = activeTaskCount };
            await dialog.ShowDialog(this);
            if (dialog.Result == CloseConfirmationResult.Exit)
            {
                _isClosing = true;
                SaveTokenUsageData();
                await CleanupBeforeExitAsync();
                _glowAnimationTimer?.Stop();
                _trayIcon?.Dispose();
                _trayIcon = null;
                Close();
            }
            return;
        }
        SaveTokenUsageData();
        await CleanupBeforeExitAsync();
        _glowAnimationTimer?.Stop();
        _trayIcon?.Dispose();
        _trayIcon = null;
        base.OnClosing(e);
        Close();
    }

    private int GetActiveTaskCount()
    {
        try
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                var forwardingService = mainViewModel.ForwardingService;
                if (forwardingService != null && forwardingService.CurrentConnectionCount > 0)
                {
                    return forwardingService.CurrentConnectionCount;
                }
            }
        }
        catch { }
        return 0;
    }

    private void SaveTokenUsageData()
    {
        try
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                var forwardingService = mainViewModel.ForwardingService;
                if (forwardingService != null && !forwardingService.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Saving token usage data before hide...");
                    forwardingService.SaveTokenUsageSync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Save token usage error: {ex.Message}");
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_settingsWindow == null || !_settingsWindow.IsVisible)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.SetMainViewModel(_viewModel);
                _settingsWindow.Show(this);
            }
            else
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                {
                    _settingsWindow.WindowState = WindowState.Normal;
                }
                _settingsWindow.Activate();
                _settingsWindow.Focus();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open SettingsWindow: {ex}");
        }
    }

    private void OnToolbarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnLogoButtonClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.ToggleLogoMode();
    }

    private void OnTabScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            e.Handled = true;
            var delta = e.Delta.Y;
            scrollViewer.Offset = scrollViewer.Offset.WithX(scrollViewer.Offset.X - delta * 50);
        }
    }

    private void OnTabScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer && _viewModel != null)
        {
            _viewModel.UpdateAvailableWidth(e.NewSize.Width);
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
    }
}