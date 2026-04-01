using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NekoT.Core.Proxy;

public class LLMApiGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _providerEndpoints;

    public LLMApiGatewayService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _providerEndpoints = new Dictionary<string, string>
        {
            { "openai", "https://api.openai.com/v1" },
            { "anthropic", "https://api.anthropic.com/v1" },
            { "minimax", "https://api.minimax.chat/v1" },
            { "deepseek", "https://api.deepseek.com/v1" }
        };
    }

    public async Task<GatewayResponse> ForwardRequestAsync(GatewayRequest request)
    {
        try
        {
            if (!_providerEndpoints.TryGetValue(request.Provider.ToLowerInvariant(), out var endpoint))
            {
                return new GatewayResponse { Success = false, Error = "Unknown provider" };
            }

            var response = await _httpClient.PostAsJsonAsync($"{endpoint}/chat/completions", request.Payload);
            var content = await response.Content.ReadAsStringAsync();

            return new GatewayResponse
            {
                Success = response.IsSuccessStatusCode,
                Content = content,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            return new GatewayResponse { Success = false, Error = ex.Message };
        }
    }
}

public class GatewayRequest
{
    public string Provider { get; set; } = string.Empty;
    public object Payload { get; set; } = new();
}

public class GatewayResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Error { get; set; }
    public int StatusCode { get; set; }
}