using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NekoT.Core.Security;

namespace NekoT.Core.Pricing;

public class PricingStorage
{
    private static PricingStorage? _instance;
    private static readonly object _lock = new();
    private readonly string _storagePath;
    private readonly Dictionary<string, ModelPricing> _pricingCache;
    private readonly Dictionary<string, List<UsageCost>> _usageHistory;

    public static PricingStorage Instance { get { if (_instance == null) { lock (_lock) { _instance ??= new PricingStorage(); } } return _instance; } }

    private PricingStorage()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var nekoTPath = Path.Combine(appDataPath, "NekoT");
        Directory.CreateDirectory(nekoTPath);
        _storagePath = Path.Combine(nekoTPath, "pricing");
        Directory.CreateDirectory(_storagePath);
        _pricingCache = new Dictionary<string, ModelPricing>();
        _usageHistory = new Dictionary<string, List<UsageCost>>();
        LoadAllPricing();
    }

    public void SaveModelPricing(string modelId, ModelPricing pricing) { if (!string.IsNullOrWhiteSpace(modelId)) { _pricingCache[modelId] = pricing; var filePath = Path.Combine(_storagePath, $"pricing_{SanitizeFileName(modelId)}.json"); var json = JsonSerializer.Serialize(pricing, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(filePath, json, System.Text.Encoding.UTF8); } }
    public ModelPricing? GetModelPricing(string modelId) { if (string.IsNullOrWhiteSpace(modelId)) return null; if (_pricingCache.TryGetValue(modelId, out var pricing)) return pricing; var loaded = LoadPricingFromFile(modelId); if (loaded != null) { _pricingCache[modelId] = loaded; return loaded; } return ModelPricing.GetDefaultPricing(modelId); }
    public void SaveUsageCost(string serviceType, UsageCost usage) { var key = $"{serviceType}_{DateTime.Now:yyyy-MM-dd}"; if (!_usageHistory.ContainsKey(key)) _usageHistory[key] = new List<UsageCost>(); _usageHistory[key].Add(usage); var filePath = Path.Combine(_storagePath, $"usage_{key}.json"); var json = JsonSerializer.Serialize(_usageHistory[key], new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(filePath, json, System.Text.Encoding.UTF8); }
    public List<UsageCost> GetDailyUsage(string serviceType, DateTime date) { var key = $"{serviceType}_{date:yyyy-MM-dd}"; if (_usageHistory.TryGetValue(key, out var usage)) return usage; var filePath = Path.Combine(_storagePath, $"usage_{key}.json"); if (File.Exists(filePath)) { var json = File.ReadAllText(filePath); var loaded = JsonSerializer.Deserialize<List<UsageCost>>(json); if (loaded != null) { _usageHistory[key] = loaded; return loaded; } } return new List<UsageCost>(); }
    public DailyCostSummary GetDailySummary(string serviceType, DateTime date) { var usage = GetDailyUsage(serviceType, date); return new DailyCostSummary { Date = date, TotalCost = usage.Sum(u => u.TotalCost), TotalInputTokens = usage.Sum(u => u.InputTokens), TotalOutputTokens = usage.Sum(u => u.OutputTokens), RequestCount = usage.Count, Currency = usage.FirstOrDefault()?.Currency ?? "USD" }; }
    private void LoadAllPricing() { try { var files = Directory.GetFiles(_storagePath, "pricing_*.json"); foreach (var file in files) { try { var json = File.ReadAllText(file); var pricing = JsonSerializer.Deserialize<ModelPricing>(json); if (pricing != null && !string.IsNullOrEmpty(pricing.ModelId)) _pricingCache[pricing.ModelId] = pricing; } catch { } } } catch { } }
    private ModelPricing? LoadPricingFromFile(string modelId) { var filePath = Path.Combine(_storagePath, $"pricing_{SanitizeFileName(modelId)}.json"); if (File.Exists(filePath)) { try { var json = File.ReadAllText(filePath); return JsonSerializer.Deserialize<ModelPricing>(json); } catch { } } return null; }
    private static string SanitizeFileName(string fileName) { foreach (var c in Path.GetInvalidFileNameChars()) { fileName = fileName.Replace(c, '_'); } return fileName; }
}