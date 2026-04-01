using System.Collections.ObjectModel;
using System.Threading;
using NekoT.Core.Configuration;
using NekoT.Core.Contracts;

namespace NekoT.Core.TokenManagement;

public class TokenService : ITokenService
 {
    private int _totalTokens;
    private int _sessionTokens;
    private readonly object _recordsLock = new();
    private readonly HashSet<string> _processedRequestIds = new();
    private readonly List<UsageRecord> _records = new();
    public ReadOnlyObservableCollection<UsageRecord> Records { get; }

    public int TotalTokens => _totalTokens;
    public int SessionTokens => _sessionTokens;

    public void AddTokens(int tokens, int promptTokens, int completionTokens)
    {
        Interlocked.Increment(ref _totalTokens, tokens);
        Interlocked.Increment(ref _sessionTokens, tokens);

        lock (_recordsLock)
        {
            var record = new UsageRecord
            {
                Id = Guid.NewGuid().ToString(),
                TotalTokens = tokens,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                Timestamp = DateTime.Now
            };
            _records.Add(record);
        }
    }

    public void ResetSession()
    {
        Interlocked.Exchange(ref _sessionTokens, ref _);
        lock (_recordsLock)
        {
            _records.Clear();
        }
    }

    public SessionStatistics GetSnapshot()
    {
        lock (_recordsLock)
        {
            return new SessionStatistics
            {
                InputTokens = _records.Sum(r => r.PromptTokens),
                OutputTokens = _records.Sum(r => r.CompletionTokens),
                RequestCount = _records.Count,
                SessionStartTime = DateTime.Now
            };
        }
    }
}

public class TokenUsageRecordedEventArgs : EventArgs
 {
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int RequestCount { get; set; }
}

public class SessionStatistics
 {
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int RequestCount { get; set; }
    public DateTime SessionStartTime { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
}
