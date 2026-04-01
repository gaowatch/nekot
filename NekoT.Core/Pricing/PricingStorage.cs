using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NekoT.Core.Pricing;

public class PricingStorage
{
    private readonly string _storageFile;
    private readonly JsonSerializerOptions _jsonOptions;

    public PricingStorage()
    {
        var appDataPath = GetAppDataPath();
        _storageFile = Path.Combine(appDataPath, "pricing_data.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private static string GetAppDataPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, "NekoT");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    public async Task SavePricingDataAsync(Dictionary<string, ProviderPricing> pricing)
    {
        try
        {
            var json = JsonSerializer.Serialize(pricing, _jsonOptions);
            await File.WriteAllTextAsync(_storageFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PricingStorage] Save failed: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, ProviderPricing>?> LoadPricingDataAsync()
    {
        try
        {
            if (!File.Exists(_storageFile))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(_storageFile);
            return JsonSerializer.Deserialize<Dictionary<string, ProviderPricing>>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PricingStorage] Load failed: {ex.Message}");
            return null;
        }
    }
}

public class ProviderPricing
{
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public decimal InputPricePer1M { get; set; }
    public decimal OutputPricePer1M { get; set; }
    public DateTime LastUpdated { get; set; }
}