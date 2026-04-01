using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NekoT.Models.Requests;
using NekoT.Models.Responses;
using NekoT.Core.Http;

namespace NekoT.Core.Forwarding;

public class ChatForwardingService
{
    private readonly HttpClient _httpClient;
    private readonly WhitelistValidator _whitelistValidator;

    public ChatForwardingService() : this(null, null) { }
    public ChatForwardingService(HttpClient? httpClient, WhitelistValidator? whitelistValidator = null) { _httpClient = httpClient ?? HttpClientManager.GetSharedClient(); _whitelistValidator = whitelistValidator ?? new WhitelistValidator(); }

    public Message[] BuildMessages(List<ChatMessage> messages) => messages.Select(msg => new Message { Role = msg.Role, Content = msg.Content }).ToArray();

    public ChatCompletionRequest CreateRequest(string model, string apiKey, List<ChatMessage> messages, string? customUrl = null, double temperature = 0.7, int? maxTokens = null) => new ChatCompletionRequest { Model = model, ApiKey = apiKey, Url = customUrl, Messages = BuildMessages(messages), Temperature = temperature, MaxTokens = maxTokens, Stream = false };

    public string ExtractContent(string jsonResponse)
    {
        try { using var doc = JsonDocument.Parse(jsonResponse); var root = doc.RootElement; if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0) { var firstChoice = choices[0]; if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content)) return content.GetString() ?? string.Empty; } return string.Empty; }
        catch { return string.Empty; }
    }

    public Usage? ExtractUsage(string jsonResponse)
    {
        try { using var doc = JsonDocument.Parse(jsonResponse); var root = doc.RootElement; if (root.TryGetProperty("usage", out var usage)) return new Usage { PromptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0, CompletionTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0, TotalTokens = usage.TryGetProperty("total_tokens", out var tt) ? tt.GetInt32() : 0 }; return null; }
        catch { return null; }
    }

    public async Task<ChatResponse> SendMessageAsync(ChatCompletionRequest request)
    {
        var targetUrl = UrlResolver.ResolveUrl(request);
        if (!_whitelistValidator.IsWhitelisted(targetUrl)) throw new UnauthorizedAccessException($"Endpoint not whitelisted: {targetUrl}");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        if (!string.IsNullOrEmpty(request.ApiKey)) httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
        var payload = new { model = request.Model, messages = request.Messages, temperature = request.Temperature, max_tokens = request.MaxTokens, stream = false };
        httpRequest.Content = JsonContent.Create(payload);
        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var content = ExtractContent(jsonResponse);
        var usage = ExtractUsage(jsonResponse);
        return new ChatResponse { Content = content, Usage = usage, RawResponse = jsonResponse };
    }
}

public class ChatMessage { public string Role { get; } public string Content { get; } public ChatMessage(string role, string content) { Role = role; Content = content; } }
public class ChatMessageDto { public string Role { get; set; } = string.Empty; public string Content { get; set; } = string.Empty; }
public class ChatResponse { public string Content { get; set; } = string.Empty; public Usage? Usage { get; set; } public string RawResponse { get; set; } = string.Empty; }