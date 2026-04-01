using System.Text.Json.Serialization;

namespace NekoT.Models.Requests;

public class ChatCompletionRequest
{
    [JsonPropertyName("url")] public string Url { get; set; } = "https://api.openai.com/v1/chat/completions";
    [JsonPropertyName("model")] public string Model { get; set; } = "gpt-4";
    [JsonPropertyName("messages")] public Message[] Messages { get; set; } = Array.Empty<Message>();
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }
    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
    [JsonPropertyName("api_key")] public string? ApiKey { get; set; }
}

public class Message { [JsonPropertyName("role")] public string Role { get; set; } = string.Empty; [JsonPropertyName("content")] public string Content { get; set; } = string.Empty; }