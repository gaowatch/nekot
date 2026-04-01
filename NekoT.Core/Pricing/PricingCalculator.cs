using System;

namespace NekoT.Core.Pricing;

public class PricingCalculator
{
    private readonly PricingStorage _storage;

    public PricingCalculator() : this(PricingStorage.Instance) { }
    public PricingCalculator(PricingStorage storage) => _storage = storage;

    public UsageCost CalculateCost(string modelId, int inputTokens, int outputTokens)
    {
        var pricing = _storage.GetModelPricing(modelId) ?? ModelPricing.GetDefaultPricing(modelId);
        var inputCost = (inputTokens / 1000.0m) * pricing.InputPricePer1K;
        var outputCost = (outputTokens / 1000.0m) * pricing.OutputPricePer1K;
        return new UsageCost { InputTokens = inputTokens, OutputTokens = outputTokens, InputCost = inputCost, OutputCost = outputCost, TotalCost = inputCost + outputCost, Currency = pricing.Currency, Timestamp = DateTime.Now };
    }

    public void RecordUsage(string serviceType, string modelId, int inputTokens, int outputTokens)
    {
        var cost = CalculateCost(modelId, inputTokens, outputTokens);
        _storage.SaveUsageCost(serviceType, cost);
    }

    public DailyCostSummary GetTodaySummary(string serviceType) => _storage.GetDailySummary(serviceType, DateTime.Today);
    public decimal GetTodayTotalCost(string serviceType) => GetTodaySummary(serviceType).TotalCost;

    public string FormatCost(decimal cost, string currency = "USD") => currency.ToUpperInvariant() switch
    {
        "USD" => $"${cost:F4}", "CNY" => $"¥{cost:F4}", "EUR" => $"€{cost:F4}", _ => $"{cost:F4} {currency}"
    };

    public string FormatCostWithTokens(decimal cost, int tokens, string currency = "USD") => $"{FormatCost(cost, currency)} ({tokens} tokens)";
}

public class UsageCost { public int InputTokens { get; set; } public int OutputTokens { get; set; } public decimal InputCost { get; set; } public decimal OutputCost { get; set; } public decimal TotalCost { get; set; } public string Currency { get; set; } = "USD"; public DateTime Timestamp { get; set; } }
public class DailyCostSummary { public decimal TotalCost { get; set; } public int TotalTokens { get; set; } public DateTime Date { get; set; } }