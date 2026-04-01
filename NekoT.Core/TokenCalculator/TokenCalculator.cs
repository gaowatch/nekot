using System.Text.Json;

namespace NekoT.Core.TokenCalculator;

public class TokenCalculator
{
    public int CalculateFromUsage(object response)
    {
        if (response is JsonElement element) return ExtractTokensFromJsonElement(element);
        var json = JsonSerializer.Serialize(response);
        using var doc = JsonDocument.Parse(json);
        return ExtractTokensFromJsonElement(doc.RootElement);
    }
    
    private int ExtractTokensFromJsonElement(JsonElement element)
    {
        if (element.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("total_tokens", out var totalTokens)) return totalTokens.GetInt32();
        return 0;
    }
}