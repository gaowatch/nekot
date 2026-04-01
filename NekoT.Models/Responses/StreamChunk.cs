using System.Text.Json.Serialization;

namespace NekoT.Models.Responses;

public class StreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<StreamChoice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}

public class StreamChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("delta")]
    public StreamDelta Delta { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class StreamDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}