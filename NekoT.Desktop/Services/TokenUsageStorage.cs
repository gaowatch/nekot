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
    private readonly string _storagePath;
    private readonly string _dataFilePath;
    private readonly IAtomicFileEngine _atomicEngine;

    public static TokenUsageStorage Instance
    {
        get { lock (_lock) { _instance ??= new TokenUsageStorage(); return _instance; } }
    }

    private TokenUsageStorage()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT");
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
        _storagePath = appDataPath;
        _dataFilePath = Path.Combine(appDataPath, "token_usage.json");
        _atomicEngine = new AtomicFileEngine(_dataFilePath);
    }

    public async Task SaveAsync(TokenUsageData data)
    {
        try
        {
            data.LastSavedTime = DateTime.Now;
            var coreData = MapToCoreData(data);
            var success = await _atomicEngine.WriteAsync(coreData);
            if (!success) await FallbackSaveAsync(data);
        }
        catch { await FallbackSaveAsync(data); }
    }

    private async Task FallbackSaveAsync(TokenUsageData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataFilePath, json);
        }
        catch { }
    }

    public async Task<TokenUsageData> LoadAsync()
    {
        try
        {
            var coreData = await _atomicEngine.ReadAsync<TokenUsageDataCore>();
            if (coreData == null) return new TokenUsageData();
            var data = MapFromCoreData(coreData);
            if (data.LastSavedTime.Date < DateTime.Today) { data.TodayTokenCount = 0; data.TodayRequestCount = 0; data.BarDataPoints.Clear(); }
            return data;
        }
        catch { return new TokenUsageData(); }
    }

    public void Clear()
    {
        try { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); if (File.Exists(_dataFilePath + ".bak")) File.Delete(_dataFilePath + ".bak"); } catch { }
    }

    private static TokenUsageDataCore MapToCoreData(TokenUsageData data) => new TokenUsageDataCore
    {
        Version = 1, LatestTokenCount = data.LatestTokenCount, TodayTokenCount = data.TodayTokenCount,
        TodayRequestCount = data.TodayRequestCount, LastSavedTime = data.LastSavedTime, LastRecordDate = data.LastSavedTime.Date,
        BarDataPoints = data.BarDataPoints.Count > 0 ? data.BarDataPoints.Select(p => new BarDataPointInfoCore { TokenCount = p.Value, Time = p.Timestamp }).ToList() : new List<BarDataPointInfoCore>()
    };

    private static TokenUsageData MapFromCoreData(TokenUsageDataCore coreData) => new TokenUsageData
    {
        LatestTokenCount = coreData.LatestTokenCount, TodayTokenCount = coreData.TodayTokenCount,
        TodayRequestCount = coreData.TodayRequestCount, LastSavedTime = coreData.LastSavedTime,
        BarDataPoints = coreData.BarDataPoints?.Select(p => new BarDataPointInfo { Value = p.TokenCount, Timestamp = p.Time }).ToList() ?? new List<BarDataPointInfo>()
    };
}

public class TokenUsageData
{
    public int LatestTokenCount { get; set; }
    public int TodayTokenCount { get; set; }
    public int TodayRequestCount { get; set; }
    public List<BarDataPointInfo> BarDataPoints { get; set; } = new();
    public DateTime LastSavedTime { get; set; } = DateTime.Now;
}

public class BarDataPointInfo { public int Value { get; set; } public DateTime Timestamp { get; set; } }

internal class TokenUsageDataCore
{
    public int Version { get; set; } = 1;
    public int LatestTokenCount { get; set; }
    public int TodayTokenCount { get; set; }
    public int TodayRequestCount { get; set; }
    public List<BarDataPointInfoCore>? BarDataPoints { get; set; } = new();
    public DateTime LastSavedTime { get; set; }
    public DateTime LastRecordDate { get; set; }
}

internal class BarDataPointInfoCore { public DateTime Time { get; set; } public int TokenCount { get; set; } }