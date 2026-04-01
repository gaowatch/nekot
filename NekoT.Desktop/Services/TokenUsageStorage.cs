using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NekoT.Core.Security;

namespace NekoT.Desktop.Services;

public class TokenUsageStorage
{
    private static TokenUsageStorage? _instance;
    private static readonly object _lock = new();
    private readonly string _storagePath;
    private readonly Dictionary<string, List<TokenUsageRecord>> _usageCache;

    public static TokenUsageStorage Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new TokenUsageStorage();
                }
            }
            return _instance;
        }
    }

    private TokenUsageStorage()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NekoT");
        Directory.CreateDirectory(appDataPath);
        _storagePath = Path.Combine(appDataPath, "token_usage.json");
        _usageCache = new Dictionary<string, List<TokenUsageRecord>>();
        LoadUsageData();
    }

    private void LoadUsageData()
    {
        try
        {
            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<TokenUsageRecord>>>(json);
                if (data != null)
                {
                    _usageCache = data;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TokenUsageStorage] Load failed: {ex.Message}");
        }
    }

    private void SaveUsageData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_usageCache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TokenUsageStorage] Save failed: {ex.Message}");
        }
    }

    public void RecordUsage(string provider, int inputTokens, int outputTokens)
    {
        var key = $"{provider}_{DateTime.Now:yyyy-MM-dd}";

        if (!_usageCache.ContainsKey(key))
        {
            _usageCache[key] = new List<TokenUsageRecord>();
        }

        _usageCache[key].Add(new TokenUsageRecord
        {
            Timestamp = DateTime.Now,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Provider = provider
        });

        SaveUsageData();
    }

    public List<TokenUsageRecord> GetUsage(string provider, DateTime date)
    {
        var key = $"{provider}_{date:yyyy-MM-dd}";
        if (_usageCache.TryGetValue(key, out var records))
        {
            return records;
        }
        return new List<TokenUsageRecord>();
    }

    public int GetTotalTokens(string provider, DateTime date)
    {
        var records = GetUsage(provider, date);
        return records.Sum(r => r.InputTokens + r.OutputTokens);
    }

    public (int input, int output) GetTokenBreakdown(string provider, DateTime date)
    {
        var records = GetUsage(provider, date);
        return (records.Sum(r => r.InputTokens), records.Sum(r => r.OutputTokens));
    }
}

public class TokenUsageRecord
{
    public DateTime Timestamp { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string Provider { get; set; } = string.Empty;
}