namespace NekoT.Core.Billing;

public class CostEstimator
{
    private readonly Dictionary<string, ModelPricing> _pricing;

    public CostEstimator()
    {
        _pricing = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
        {
            ["MiniMax-M2.5"] = new ModelPricing { InputPer1K = 0.02m, OutputPer1K = 0.10m },
            ["MiniMax-M2.5-highspeed"] = new ModelPricing { InputPer1K = 0.05m, OutputPer1K = 0.15m },
            ["gpt-4"] = new ModelPricing { InputPer1K = 0.03m, OutputPer1K = 0.06m },
            ["gpt-4-turbo"] = new ModelPricing { InputPer1K = 0.01m, OutputPer1K = 0.03m },
            ["gpt-3.5-turbo"] = new ModelPricing { InputPer1K = 0.0005m, OutputPer1K = 0.0015m },
            ["claude-3-opus"] = new ModelPricing { InputPer1K = 0.015m, OutputPer1K = 0.075m },
            ["claude-3-sonnet"] = new ModelPricing { InputPer1K = 0.003m, OutputPer1K = 0.015m },
            ["claude-3-haiku"] = new ModelPricing { InputPer1K = 0.00025m, OutputPer1K = 0.00125m },
            ["gemini-pro"] = new ModelPricing { InputPer1K = 0.0005m, OutputPer1K = 0.0015m },
            ["gemini-ultra"] = new ModelPricing { InputPer1K = 0.002m, OutputPer1K = 0.008m },
            ["default"] = new ModelPricing { InputPer1K = 0.01m, OutputPer1K = 0.03m }
        };
    }

    public decimal? EstimateCost(string model, int promptTokens, int completionTokens)
    {
        if (!_pricing.TryGetValue(model, out var pricing))
        {
            return null;
        }

        var inputCost = (promptTokens / 1000.0m) * pricing.InputPer1K;
        var outputCost = (completionTokens / 1000.0m) * pricing.OutputPer1K;
        return inputCost + outputCost;
    }

    public decimal? EstimateCost(int totalTokens)
    {
        return EstimateCost("default", totalTokens / 2, totalTokens - totalTokens / 2);
    }

    public decimal? EstimateCost(int promptTokens, int completionTokens)
    {
        return EstimateCost("default", promptTokens, completionTokens);
    }

    public bool IsModelSupported(string model)
    {
        return _pricing.ContainsKey(model);
    }

    public IEnumerable<string> GetSupportedModels()
    {
        return _pricing.Keys;
    }

    public CostBreakdown? GetCostBreakdown(string model, int promptTokens, int completionTokens)
    {
        if (!_pricing.TryGetValue(model, out var pricing))
        {
            return null;
        }

        return new CostBreakdown
        {
            Model = model,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            InputCost = Math.Round((promptTokens / 1000.0m) * pricing.InputPer1K, 6),
            OutputCost = Math.Round((completionTokens / 1000.0m) * pricing.OutputPer1K, 6)
        };
    }
}

public class ModelPricing
{
    public decimal InputPer1K { get; set; }
    public decimal OutputPer1K { get; set; }
}

public class CostBreakdown
{
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal InputCost { get; set; }
    public decimal OutputCost { get; set; }
    public decimal TotalCost => InputCost + OutputCost;
}
