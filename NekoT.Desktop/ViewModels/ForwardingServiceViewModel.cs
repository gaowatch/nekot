using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoT.Core.LlmProviders;

namespace NekoT.Desktop.ViewModels;

public class ForwardingServiceViewModel : ViewModelBase, IDisposable
{
    private readonly List<UsageRecord> _usageRecords = new();
    private readonly object _lock = new();
    private int _sessionInputTokens;
    private int _sessionOutputTokens;
    private decimal _sessionCost;
    private decimal _todayCost;
    private bool _disposed;

    public event EventHandler? UsageUpdated;

    public int SessionInputTokens
    {
        get => _sessionInputTokens;
        private set => SetField(ref _sessionInputTokens, value);
    }

    public int SessionOutputTokens
    {
        get => _sessionOutputTokens;
        private set => SetField(ref _sessionOutputTokens, value);
    }

    public int SessionTotalTokens => SessionInputTokens + SessionOutputTokens;

    public decimal SessionCost
    {
        get => _sessionCost;
        private set => SetField(ref _sessionCost, value);
    }

    public decimal TodayCost
    {
        get => _todayCost;
        private set => SetField(ref _todayCost, value);
    }

    public void RecordTokenUsage(int inputTokens, int outputTokens)
    {
        lock (_lock)
        {
            _sessionInputTokens += inputTokens;
            _sessionOutputTokens += outputTokens;
        }

        OnPropertyChanged(nameof(SessionInputTokens));
        OnPropertyChanged(nameof(SessionOutputTokens));
        OnPropertyChanged(nameof(SessionTotalTokens));
        UsageUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void ResetSession()
    {
        lock (_lock)
        {
            _sessionInputTokens = 0;
            _sessionOutputTokens = 0;
            _sessionCost = 0;
        }

        OnPropertyChanged(nameof(SessionInputTokens));
        OnPropertyChanged(nameof(SessionOutputTokens));
        OnPropertyChanged(nameof(SessionTotalTokens));
        OnPropertyChanged(nameof(SessionCost));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}