using System.Text.Json.Serialization;
using NekoT.Models.Requests;

namespace NekoT.Models.Responses;

public class ChatCompletionResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
    [JsonPropertyName("usage")] public Usage? Usage { get; set; }
    [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = new();
    [JsonPropertyName("object")] public string Object { get; set; } = string.Empty;
    [JsonPropertyName("created")] public long Created { get; set; }
    [JsonIgnore] public string? Error { get; set; }
}

public class Choice { [JsonPropertyName("message")] public MessageChoice Message { get; set; } = new(); [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = string.Empty; [JsonPropertyName("index")] public int Index { get; set; } }
public class MessageChoice { [JsonPropertyName("role")] public string Role { get; set; } = string.Empty; [JsonPropertyName("content")] public string Content { get; set; } = string.Empty; [JsonPropertyName("name")] public string? Name { get; set; } }