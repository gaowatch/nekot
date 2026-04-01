using System;

namespace NekoT.Core.Pricing;

public class PricingCalculator
{
    private readonly PricingStorage _storage;

    public PricingCalculator() : this(PricingStorage.Instance)
    {
    }

    public PricingCalculator(PricingStorage storage)
    {
        _storage = storage;
    }

    public UsageCost CalculateCost(string modelId, int inputTokens, int outputTokens)
    {
        var pricing = _storage.GetModelPricing(modelId);
        if (pricing == null)
        {
            pricing = ModelPricing.GetDefaultPricing(modelId);
        }

        var inputCost = (inputTokens / 1000.0m) * pricing.InputPricePer1K;
        var outputCost = (outputTokens / 1000.0m) * pricing.OutputPricePer1K;

        return new UsageCost
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            InputCost = inputCost,
            OutputCost = outputCost,
            TotalCost = inputCost + outputCost,
            Currency = pricing.Currency,
            Timestamp = DateTime.Now
        };
    }

    public void RecordUsage(string serviceType, string modelId, int inputTokens, int outputTokens)
    {
        var cost = CalculateCost(modelId, inputTokens, outputTokens);
        _storage.SaveUsageCost(serviceType, cost);
    }

    public DailyCostSummary GetTodaySummary(string serviceType)
    {
        return _storage.GetDailySummary(serviceType, DateTime.Today);
    }

    public decimal GetTodayTotalCost(string serviceType)
    {
        var summary = GetTodaySummary(serviceType);
        return summary.TotalCost;
    }

    public string FormatCost(decimal cost, string currency = "USD")
    {
        return currency.ToUpperInvariant() switch
        {
            "USD" => $"${cost:F4}",
            "CNY" => $"\u00a5{cost:F4}",
            "EUR" => $"\u20ac{cost:F4}",
            _ => $"{cost:F4} {currency}"
        };
    }

    public string FormatCostWithTokens(decimal cost, int tokens, string currency = "USD")
    {
        var costStr = FormatCost(cost, currency);
        return $"{costStr} ({tokens} tokens)";
    }
}