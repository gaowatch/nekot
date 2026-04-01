using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using NekoT.Core.Contracts;

namespace NekoT.Core.TokenManagement;

public class TokenService : ITokenService
{
    private int _totalTokens;
    private int _sessionTokens;
    private readonly object _lock = new();
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ObservableCollection<UsageRecord> _usageRecords;
    private readonly ConcurrentDictionary<string, int> _providerTokens;

    public TokenService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var nekotPath = Path.Combine(appDataPath, "NekoT");
        if (!Directory.Exists(nekotPath)) Directory.CreateDirectory(nekotPath);
        _storagePath = Path.Combine(nekotPath, "token_usage.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _usageRecords = new ObservableCollection<UsageRecord>();
        _providerTokens = new ConcurrentDictionary<string, int>();
        LoadUsageData();
    }

    public int TotalTokens => _totalTokens;
    public int SessionTokens => _sessionTokens;
    public ObservableCollection<UsageRecord> UsageRecords => _usageRecords;

    public void RecordUsage(int tokens, string? provider = null, string? requestId = null)
    {
        if (tokens <= 0) return;
        Interlocked.Add(ref _totalTokens, tokens);
        Interlocked.Add(ref _sessionTokens, tokens);
        var record = new UsageRecord
        {
            Timestamp = DateTime.Now,
            Tokens = tokens,
            Provider = provider ?? "Unknown",
            RequestId = requestId ?? Guid.NewGuid().ToString()
        };
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _usageRecords.Add(record));
        if (!string.IsNullOrEmpty(provider)) _providerTokens.AddOrUpdate(provider, tokens, (_, existing) => existing + tokens);
        System.Diagnostics.Debug.WriteLine($"[TokenService] Recorded {tokens} tokens from {provider}");
    }

    public void ResetSession()
    {
        Interlocked.Exchange(ref _sessionTokens, 0);
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _usageRecords.Clear());
    }

    public TokenStatistics GetStatistics()
    {
        lock (_lock)
        {
            var todayRecords = _usageRecords.Where(r => r.Timestamp.Date == DateTime.Today).ToList();
            return new TokenStatistics
            {
                TotalTokens = _totalTokens,
                SessionTokens = _sessionTokens,
                TodayTokens = todayRecords.Sum(r => r.Tokens),
                RecordCount = _usageRecords.Count
            };
        }
    }

    public Dictionary<string, int> GetProviderBreakdown()
    {
        return _providerTokens.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void SaveUsageData()
    {
        try
        {
            lock (_lock)
            {
                var data = new TokenUsageData { TotalTokens = _totalTokens, Records = _usageRecords.ToList() };
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(_storagePath, json);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TokenService] Save failed: {ex.Message}"); }
    }

    private void LoadUsageData()
    {
        try
        {
            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                var data = JsonSerializer.Deserialize<TokenUsageData>(json, _jsonOptions);
                if (data != null)
                {
                    _totalTokens = data.TotalTokens;
                    foreach (var record in data.Records) _usageRecords.Add(record);
                }
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TokenService] Load failed: {ex.Message}"); }
    }
}

public class UsageRecord
{
    public DateTime Timestamp { get; set; }
    public int Tokens { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}

public class TokenUsageData
{
    public int TotalTokens { get; set; }
    public List<UsageRecord> Records { get; set; } = new();
}

public class TokenStatistics
{
    public int TotalTokens { get; set; }
    public int SessionTokens { get; set; }
    public int TodayTokens { get; set; }
    public int RecordCount { get; set; }
}