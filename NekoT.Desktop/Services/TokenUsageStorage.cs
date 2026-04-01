using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NekoT.Core.Storage;

namespace NekoT.Desktop.Services;

public class TokenUsageStorage
{
    private static TokenUsageStorage? _instance;
    private static readonly object _lock = new();
    private readonly string _dataFilePath;
    private readonly IAtomicFileEngine _atomicEngine;

    public static TokenUsageStorage Instance { get { lock (_lock) { _instance ??= new TokenUsageStorage(); return _instance; } } }

    private TokenUsageStorage()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT");
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
        _dataFilePath = Path.Combine(appDataPath, "token_usage.json");
        _atomicEngine = new AtomicFileEngine(_dataFilePath);
    }

    public async Task SaveAsync(TokenUsageData data)
    {
        try
        {
            data.LastSavedTime = DateTime.Now;
            var success = await _atomicEngine.WriteAsync(data);
            if (!success) await FallbackSaveAsync(data);
        }
        catch { await FallbackSaveAsync(data); }
    }

    private async Task FallbackSaveAsync(TokenUsageData data)
    {
        try { var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }); await File.WriteAllTextAsync(_dataFilePath, json); }
        catch { }
    }

    public async Task<TokenUsageData> LoadAsync()
    {
        try
        {
            var data = await _atomicEngine.ReadAsync<TokenUsageData>();
            if (data == null) return new TokenUsageData();
            if (data.LastSavedTime.Date < DateTime.Today) { data.TodayTokenCount = 0; data.TodayRequestCount = 0; data.BarDataPoints.Clear(); }
            return data;
        }
        catch { return new TokenUsageData(); }
    }

    public void Clear()
    {
        try { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); }
        catch { }
    }
}

public class TokenUsageData
{
    public int LatestTokenCount { get; set; }
    public int TodayTokenCount { get; set; }
    public int TodayRequestCount { get; set; }
    public List<BarDataPointInfo> BarDataPoints { get; set; } = new();
    public DateTime LastSavedTime { get; set; } = DateTime.Now;
}

public class BarDataPointInfo
{
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
}