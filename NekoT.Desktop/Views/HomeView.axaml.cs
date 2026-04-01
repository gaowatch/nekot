using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NekoT.Desktop.ViewModels;
using NekoT.Desktop.Views;
using System.Threading;

namespace NekoT.Desktop.Views;

public partial class HomeView : UserControl
{
    public event EventHandler<string>? NavigationRequested;
    private DispatcherTimer? _glowAnimationTimer;
    private double _glowAngle = 0;
    private bool _isNavigating = false;
    private DateTime _lastNavigateTime;
    private readonly object _navigationLock = new object();

    public HomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_glowAnimationTimer != null)
        {
            _glowAnimationTimer.Stop();
            _glowAnimationTimer.Start();
            return;
        }
        StartGlowAnimation();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_glowAnimationTimer != null)
        {
            _glowAnimationTimer.Tick -= OnGlowAnimationTick;
            _glowAnimationTimer.Stop();
            _glowAnimationTimer = null;
        }
    }

    private void StartGlowAnimation()
    {
        _glowAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(66)
        };
        _glowAnimationTimer.Tick += OnGlowAnimationTick;
        _glowAnimationTimer.Start();
    }

    private void OnGlowAnimationTick(object? sender, EventArgs e)
    {
        _glowAngle += 1.33;
        if (_glowAngle >= 360) _glowAngle = 0;

        var glowBorder = this.FindControl<Border>("SearchGlowBorder");
        if (glowBorder != null)
        {
            var angleRad = _glowAngle * Math.PI / 180;
            var startX = 0.5 + 0.5 * Math.Cos(angleRad);
            var startY = 0.5 + 0.5 * Math.Sin(angleRad);
            var endX = 0.5 - 0.5 * Math.Cos(angleRad);
            var endY = 0.5 - 0.5 * Math.Sin(angleRad);

            var gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(startX, startY, RelativeUnit.Relative),
                EndPoint = new RelativePoint(endX, endY, RelativeUnit.Relative)
            };

            var pulseIntensity = 0.6 + 0.4 * Math.Sin(_glowAngle * Math.PI / 180 * 2);
            var centerAlpha = (byte)(102 * pulseIntensity);
            var sideAlpha = (byte)(51 * pulseIntensity);

            gradient.GradientStops.Add(new GradientStop(Color.Parse("#00FFFFFF"), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromUInt32((uint)(sideAlpha << 24 | 0xFFFFFF)), 0.25));
            gradient.GradientStops.Add(new GradientStop(Color.FromUInt32((uint)(centerAlpha << 24 | 0xFFFFFF)), 0.5));
            gradient.GradientStops.Add(new GradientStop(Color.FromUInt32((uint)(sideAlpha << 24 | 0xFFFFFF)), 0.75));
            gradient.GradientStops.Add(new GradientStop(Color.Parse("#00FFFFFF"), 1.0));

            glowBorder.Background = gradient;
        }
    }

    public void SetViewModel(HomeViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.NavigateRequested += (s, url) => NavigationRequested?.Invoke(this, url);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is HomeViewModel vm)
        {
            var query = vm.SearchQuery?.Trim() ?? "";
            if (!string.IsNullOrEmpty(query))
            {
                var url = vm.GetNavigateUrl(query);
                NavigationRequested?.Invoke(this, url);
            }
        }
    }

    private void OnHelpClick(object? sender, RoutedEventArgs e)
    {
        var guideWindow = new GuideWindow();
        var parentWindow = this.FindAncestorOfType<Window>();
        if (parentWindow != null)
        {
            guideWindow.Show(parentWindow);
        }
        else
        {
            guideWindow.Show();
        }
    }
}