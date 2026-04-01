using System;
using System.Collections.Generic;

namespace NekoT.Core.Pricing;

public class ModelPricing
{
    public string ModelId { get; set; } = string.Empty;
    public decimal InputPricePer1K { get; set; }
    public decimal OutputPricePer1K { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime EffectiveDate { get; set; } = DateTime.Now;

    public static ModelPricing GetDefaultPricing(string modelId)
    {
        return modelId.ToLowerInvariant() switch
        {
            var id when id.Contains("gpt-4o") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.003m,
                OutputPricePer1K = 0.012m,
                Currency = "USD"
            },
            var id when id.Contains("gpt-4") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.03m,
                OutputPricePer1K = 0.06m,
                Currency = "USD"
            },
            var id when id.Contains("gpt-3.5") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.0003m,
                OutputPricePer1K = 0.0015m,
                Currency = "USD"
            },
            var id when id.Contains("claude-3-opus") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.015m,
                OutputPricePer1K = 0.075m,
                Currency = "USD"
            },
            var id when id.Contains("claude-3-sonnet") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.003m,
                OutputPricePer1K = 0.015m,
                Currency = "USD"
            },
            var id when id.Contains("claude-3-haiku") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.00025m,
                OutputPricePer1K = 0.00125m,
                Currency = "USD"
            },
            var id when id.Contains("gemini-pro") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.00025m,
                OutputPricePer1K = 0.0005m,
                Currency = "USD"
            },
            var id when id.Contains("gemini-1.5") => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.0025m,
                OutputPricePer1K = 0.0075m,
                Currency = "USD"
            },
            _ => new ModelPricing
            {
                ModelId = modelId,
                InputPricePer1K = 0.01m,
                OutputPricePer1K = 0.03m,
                Currency = "USD"
            }
        };
    }
}

public class UsageCost
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal InputCost { get; set; }
    public decimal OutputCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class DailyCostSummary
{
    public DateTime Date { get; set; }
    public decimal TotalCost { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int RequestCount { get; set; }
    public string Currency { get; set; } = "USD";
}