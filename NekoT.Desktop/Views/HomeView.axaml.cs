using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Timers;

namespace NekoT.Desktop.Views;

public partial class HomeView : UserControl
    
{
    private Timer? _glowTimer;
    private double _glowPhase;
    private const double GlowSpeed = 0.02;

    public HomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        StartGlowAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        StopGlowAnimation();
    }

    private void StartGlowAnimation()
    {
        _glowTimer = new Timer(50);
        _glowTimer.Elapsed += OnGlowAnimationTick;
        _glowTimer.Start();
    }

    private void StopGlowAnimation()
    {
        _glowTimer?.Stop();
        _glowTimer?.Dispose();
        _glowTimer = null;
    }

    private void OnGlowAnimationTick(object? sender, EventArgs e)
    {
        _glowPhase = (_glowPhase + GlowSpeed) % (2 * Math.PI);
        var intensity = (Math.Sin(_glowPhase) + 1) / 2;
        var r = (byte)(40 + intensity * 60);
        var g = (byte)(130 + intensity * 80);
        var b = (byte)(255);

        var glowBorder = this.FindControl<Border>("GlowBorder");
        if (glowBorder != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.FromRgb(r, g, b), 0.0),
                        new GradientStop(Colors.Transparent, 1.0)
                    }
                };
                glowBorder.Background = gradient;
            });
        }
    }
}
