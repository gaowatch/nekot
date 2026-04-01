using System.Text.Json;

namespace NekoT.Core.TokenCalculator;

public class TokenCalculator
{
    public int CalculateFromUsage(object response)
    {
        // 优化：直接检查对象类型，避免不必要的序列化
        if (response is JsonElement element)
        {
            return ExtractTokensFromJsonElement(element);
        }
        
        // 仅在必要时才进行序列化
        var json = JsonSerializer.Serialize(response);
        using var doc = JsonDocument.Parse(json);
        return ExtractTokensFromJsonElement(doc.RootElement);
    }
    
    private int ExtractTokensFromJsonElement(JsonElement element)
    {
        if (element.TryGetProperty("usage", out var usage) &&
            usage.ValueKind == JsonValueKind.Object &&
            usage.TryGetProperty("total_tokens", out var totalTokens))
        {
            return totalTokens.GetInt32();
        }
        return 0;
    }
}
