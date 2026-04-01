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

    public MainWindow() : this(App.Services.GetRequiredService<MainViewModel>()) { }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.Tabs.CollectionChanged += OnTabsCollectionChanged;
        StartGlowAnimation();
        ApplyStartupSettings();
        SetupTrayIcon();
    }

    private void OnTabsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }
    private void OnMainWindowLoaded(object? sender, RoutedEventArgs e) { }
    
    private void ApplyStartupSettings()
    {
        var settings = UserSettingsService.Instance;
        if (settings.StartMinimized) { }
        else if (settings.StartMaximized)
        {
            Dispatcher.UIThread.Post(() =>
            {
                WindowState = WindowState.Maximized;
                EnsureWindowVisible();
            }, DispatcherPriority.Input);
        }
        else
        {
            Dispatcher.UIThread.Post(() => EnsureWindowVisible(), DispatcherPriority.Input);
        }
        SystemFeaturesHelper.ApplyStartupSettings();
    }
    
    private void EnsureWindowVisible()
    {
        try
        {
            EnsureWindowOnPrimaryScreen();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            if (!IsVisible) Show();
            Activate();
            Topmost = true;
            Topmost = false;
        }
        catch { }
    }
    
    private void EnsureWindowOnPrimaryScreen()
    {
        try
        {
            if (WindowState == WindowState.Maximized) return;
            if (Screens.Primary != null)
            {
                var workArea = Screens.Primary.WorkingArea;
                var windowWidth = Width;
                var windowHeight = Height;
                if (windowWidth > workArea.Width * 0.9) windowWidth = workArea.Width * 0.9;
                if (windowHeight > workArea.Height * 0.9) windowHeight = workArea.Height * 0.9;
                Position = new PixelPoint((int)(workArea.X + (workArea.Width - windowWidth) / 2), (int)(workArea.Y + (workArea.Height - windowHeight) / 2));
                Width = windowWidth;
                Height = windowHeight;
            }
        }
        catch { }
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
            using (var stream = AssetLoader.Open(new Uri("avares://NekoT.Desktop/Assets/nekotlogo.png"))) { _trayIcon.Icon = new WindowIcon(stream); }
            _trayIcon.Clicked += (s, e) => ShowMainWindow();
        }
        catch { }
    }
    
    private void ShowMainWindow() { Dispatcher.UIThread.Post(() => { Show(); WindowState = WindowState.Normal; Activate(); }); }
    private void ShutdownApp() { _isClosing = true; _ = CleanupBeforeExitAsync(); if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown(); }
    private async Task CleanupBeforeExitAsync() { try { if (DataContext is MainViewModel mainViewModel) { var forwardingService = mainViewModel.ForwardingService; if (forwardingService != null && !forwardingService.IsDisposed) await forwardingService.DisposeAsync(); } } catch { } }
    private void StartGlowAnimation() { _glowAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; _glowAnimationTimer.Tick += OnGlowAnimationTick; _glowAnimationTimer.Start(); }
    private void OnGlowAnimationTick(object? sender, EventArgs e) { /* animation logic */ }
    protected override async void OnClosing(WindowClosingEventArgs e) { /* closing logic */ }
    private int GetActiveTaskCount() { try { if (DataContext is MainViewModel mainViewModel) { var forwardingService = mainViewModel.ForwardingService; if (forwardingService != null && forwardingService.CurrentConnectionCount > 0) return forwardingService.CurrentConnectionCount; } } catch { } return 0; }
    private void SaveTokenUsageData() { try { if (DataContext is MainViewModel mainViewModel) { var forwardingService = mainViewModel.ForwardingService; if (forwardingService != null && !forwardingService.IsDisposed) forwardingService.SaveTokenUsageSync(); } } catch { } }
    private void OnSettingsClick(object? sender, RoutedEventArgs e) { try { if (_settingsWindow == null || !_settingsWindow.IsVisible) { _settingsWindow = new SettingsWindow(); _settingsWindow.SetMainViewModel(_viewModel); _settingsWindow.Show(this); } else { if (_settingsWindow.WindowState == WindowState.Minimized) _settingsWindow.WindowState = WindowState.Normal; _settingsWindow.Activate(); _settingsWindow.Focus(); } } catch { } }
    private void OnToolbarPointerPressed(object? sender, PointerPressedEventArgs e) { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); }
    private void OnLogoButtonClick(object? sender, RoutedEventArgs e) { _viewModel.ToggleLogoMode(); }
    private void OnTabScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e) { if (sender is ScrollViewer scrollViewer) { e.Handled = true; var delta = e.Delta.Y; scrollViewer.Offset = scrollViewer.Offset.WithX(scrollViewer.Offset.X - delta * 50); } }
    private void OnTabScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e) { if (sender is ScrollViewer scrollViewer && _viewModel != null) _viewModel.UpdateAvailableWidth(e.NewSize.Width); }
    protected override void OnSizeChanged(SizeChangedEventArgs e) { base.OnSizeChanged(e); }
}