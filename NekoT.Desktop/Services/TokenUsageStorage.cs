using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NekoT.Desktop.Services;

public class TokenUsageStorage
{
    private static TokenUsageStorage? _instance;
    private static readonly object _lock = new();
    private readonly string _dataFilePath;

    public static TokenUsageStorage Instance { get { lock (_lock) { _instance ??= new TokenUsageStorage(); return _instance; } } }

    private TokenUsageStorage()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT");
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
        _dataFilePath = Path.Combine(appDataPath, "token_usage.json");
    }

    public async Task SaveAsync(TokenUsageData data)
    {
        data.LastSavedTime = DateTime.Now;
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_dataFilePath, json);
    }

    public async Task<TokenUsageData> LoadAsync()
    {
        try { if (File.Exists(_dataFilePath)) return JsonSerializer.Deserialize<TokenUsageData>(await File.ReadAllTextAsync(_dataFilePath)) ?? new TokenUsageData(); }
        catch { }
        return new TokenUsageData();
    }

    public void Clear() { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); }
}

public class TokenUsageData
{
    public int LatestTokenCount { get; set; }
    public int TodayTokenCount { get; set; }
    public int TodayRequestCount { get; set; }
    public System.Collections.Generic.List<BarDataPointInfo> BarDataPoints { get; set; } = new();
    public DateTime LastSavedTime { get; set; } = DateTime.Now;
}

public class BarDataPointInfo
{
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
}
