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
    private readonly Queue<string> _requestIdQueue = new();

    public TokenService()
    {
        UsageRecords = new ObservableCollection<UsageRecord>();
    }

    public int TotalTokens => _totalTokens;
    public int SessionTokens => _sessionTokens;
    public ObservableCollection<UsageRecord> UsageRecords { get; }

    public void RecordUsage(int tokens, string? provider = null, string? requestId = null)
    {
        if (!string.IsNullOrEmpty(requestId))
        {
            lock (_recordsLock)
            {
                if (_processedRequestIds.Contains(requestId))
                    return;

                while (_requestIdQueue.Count >= AppConstants.TokenManagement.DeduplicationCacheSize)
                {
                    var oldestId = _requestIdQueue.Dequeue();
                    _processedRequestIds.Remove(oldestId);
                }

                _processedRequestIds.Add(requestId);
                _requestIdQueue.Enqueue(requestId);
            }
        }

        Interlocked.Add(ref _totalTokens, tokens);
        Interlocked.Add(ref _sessionTokens, tokens);

        var record = new UsageRecord
        {
            Id = requestId ?? Guid.NewGuid().ToString(),
            Tokens = tokens,
            Provider = provider ?? "Unknown",
            Timestamp = DateTime.Now
        };

        lock (_recordsLock)
        {
            while (UsageRecords.Count >= AppConstants.TokenManagement.MaxRecordCount)
            {
                UsageRecords.RemoveAt(UsageRecords.Count - 1);
            }

            UsageRecords.Insert(0, record);
        }
    }

    public void ResetSession()
    {
        int originalValue;
        do
        {
            originalValue = _sessionTokens;
        } while (Interlocked.CompareExchange(ref _sessionTokens, 0, originalValue) != originalValue);
    }

    public TokenStatistics GetStatistics()
    {
        int recordsCount;
        lock (_recordsLock)
        {
            recordsCount = UsageRecords.Count;
        }

        return new TokenStatistics
        {
            TotalTokens = _totalTokens,
            SessionTokens = _sessionTokens,
            RecordsCount = recordsCount
        };
    }

    public Dictionary<string, int> GetProviderBreakdown()
    {
        List<UsageRecord> snapshot;
        lock (_recordsLock)
        {
            snapshot = UsageRecords.ToList();
        }

        return snapshot
            .GroupBy(r => r.Provider)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Tokens));
    }
}

public class UsageRecord
{
    public string Id { get; set; } = string.Empty;
    public int Tokens { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TokenStatistics
{
    public int TotalTokens { get; set; }
    public int SessionTokens { get; set; }
    public int RecordsCount { get; set; }
}