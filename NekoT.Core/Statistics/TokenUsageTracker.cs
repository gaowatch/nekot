using System;
using System.Threading;

namespace NekoT.Core.Statistics;

public class TokenUsageTracker : IDisposable
{
    private readonly object _lock = new();
    private int _sessionInputTokens;
    private int _sessionOutputTokens;
    private int _sessionRequestCount;
    private readonly DateTime _sessionStartTime;
    private bool _disposed;

    public event EventHandler<TokenUsageRecordedEventArgs>? TokenUsageRecorded;

    public TokenUsageTracker() => _sessionStartTime = DateTime.Now;

    public void RecordUsage(int inputTokens, int outputTokens)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TokenUsageTracker));
        if (inputTokens < 0 || outputTokens < 0) throw new ArgumentException("Token counts cannot be negative");
        lock (_lock) { _sessionInputTokens += inputTokens; _sessionOutputTokens += outputTokens; _sessionRequestCount++; }
        TokenUsageRecorded?.Invoke(this, new TokenUsageRecordedEventArgs { InputTokens = inputTokens, OutputTokens = outputTokens, TotalInputTokens = _sessionInputTokens, TotalOutputTokens = _sessionOutputTokens, RequestCount = _sessionRequestCount });
    }

    public SessionStatistics GetSnapshot()
    {
        lock (_lock) { return new SessionStatistics { InputTokens = _sessionInputTokens, OutputTokens = _sessionOutputTokens, RequestCount = _sessionRequestCount, SessionStartTime = _sessionStartTime }; }
    }

    public void Reset()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TokenUsageTracker));
        lock (_lock) { _sessionInputTokens = 0; _sessionOutputTokens = 0; _sessionRequestCount = 0; }
    }

    public void Dispose() { if (!_disposed) _disposed = true; }
}

public class TokenUsageRecordedEventArgs : EventArgs { public int InputTokens { get; set; } public int OutputTokens { get; set; } public int TotalInputTokens { get; set; } public int TotalOutputTokens { get; set; } public int RequestCount { get; set; } }
public class SessionStatistics { public int InputTokens { get; set; } public int OutputTokens { get; set; } public int RequestCount { get; set; } public DateTime SessionStartTime { get; set; } public int TotalTokens => InputTokens + OutputTokens; }