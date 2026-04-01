using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NekoT.Core.Contracts;
using NekoT.Core.Http;
using NekoT.Core.Security;
using NekoT.Models.Requests;
using NekoT.Models.Responses;

namespace NekoT.Core.Forwarding;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public ChatMessage() { }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

public class ChatForwardingService
{
    private readonly SecureStorage _secureStorage;
    private readonly HttpClient _httpClient;

    public ChatForwardingService()
    {
        _secureStorage = new SecureStorage();
        _httpClient = HttpClientManager.GetSharedClient();
    }

    public string? GetApiKey(string provider)
    {
        return _secureStorage.GetApiKey(provider);
    }

    public void SaveApiKey(string provider, string apiKey)
    {
        _secureStorage.SaveApiKey(provider, apiKey);
    }

    public bool HasApiKey(string provider)
    {
        return _secureStorage.HasApiKey(provider);
    }

    public void DeleteApiKey(string provider)
    {
        _secureStorage.DeleteApiKey(provider);
    }

    public Dictionary<string, string> GetAllApiKeys()
    {
        return _secureStorage.LoadAllKeys();
    }

    public async Task<ChatCompletionResponse> SendMessageAsync(
        string model,
        string apiKey,
        List<ChatMessage> messages,
        string? customUrl = null,
        bool stream = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new UnauthorizedAccessException("API key is required");

        var targetUrl = customUrl ?? UrlResolver.ResolveUrl(model);

        if (!_secureStorage.HasApiKey("api_keys"))
        {
            _secureStorage.SaveApiKey("api_keys", apiKey);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream = stream,
            stream_options = new { include_usage = true }
        };

        request.Content = JsonContent.Create(requestBody);

        try
        {
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ChatCompletionResponse
                {
                    Error = $"HTTP {(int)response.StatusCode}: {errorContent}"
                };
            }

            if (stream)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var usage = ExtractUsageFromSSE(content);
                return new ChatCompletionResponse
                {
                    Usage = usage,
                    Choices = new List<Choice>
                    {
                        new Choice
                        {
                            Message = new MessageChoice { Content = content },
                            FinishReason = "stop"
                        }
                    }
                };
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<ChatCompletionResponse>(content) ?? new ChatCompletionResponse();
            }
        }
        catch (HttpRequestException ex)
        {
            return new ChatCompletionResponse { Error = $"Network error: {ex.Message}" };
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            return new ChatCompletionResponse { Error = "Request timed out" };
        }
    }

    private Usage? ExtractUsageFromSSE(string sseContent)
    {
        try
        {
            var lines = sseContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
                {
                    var json = line.Substring("data: ".Length);
                    var chunk = JsonSerializer.Deserialize<StreamChunk>(json);
                    if (chunk?.Usage != null)
                        return chunk.Usage;
                }
            }
        }
        catch
        {
        }
        return null;
    }
}