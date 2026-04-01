using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NekoT.Desktop.Views.TokenVisualization;

public class TokenBarChartViewModel : INotifyPropertyChanged, IDisposable
{
    public ObservableCollection<BarDataPoint> BarDataPoints { get; } = new();
    private const int MaxBars = 100;
    private const double MaxBarHeight = 200.0;
    private const int MaxTokenValue = 200000;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TokenBarChartViewModel()
    {
    }

    public void AddDataPoint(DateTime timestamp, int inputTokens, int outputTokens)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (BarDataPoints.Count >= MaxBars)
            {
                BarDataPoints.RemoveAt(0);
            }

            var totalTokens = inputTokens + outputTokens;
            BarDataPoints.Add(new BarDataPoint
            {
                Timestamp = timestamp,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = totalTokens,
                BarHeight = Math.Min((double)totalTokens / MaxTokenValue * MaxBarHeight, MaxBarHeight)
            });

            OnPropertyChanged(nameof(BarDataPoints));
        });
    }

    public void Clear()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            BarDataPoints.Clear();
        });
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        BarDataPoints.Clear();
    }
}

public class BarDataPoint : INotifyPropertyChanged
{
    private int _inputTokens;
    private int _outputTokens;
    private int _totalTokens;
    private double _barHeight;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTime Timestamp { get; set; }

    public int InputTokens
    {
        get => _inputTokens;
        set { _inputTokens = value; OnPropertyChanged(); }
    }

    public int OutputTokens
    {
        get => _outputTokens;
        set { _outputTokens = value; OnPropertyChanged(); }
    }

    public int TotalTokens
    {
        get => _totalTokens;
        set { _totalTokens = value; OnPropertyChanged(); }
    }

    public double BarHeight
    {
        get => _barHeight;
        set { _barHeight = value; OnPropertyChanged(); }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
