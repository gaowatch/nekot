using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NekoT.Core.Http;
using NekoT.Models.Requests;
using NekoT.Models.Responses;

namespace NekoT.Core.Forwarding;

public class ForwardingService
{
    private readonly HttpClient _httpClient;
    private readonly WhitelistValidator _whitelistValidator;
    private readonly StreamHandler _streamHandler;

    public ForwardingService(HttpClient? httpClient = null, WhitelistValidator? whitelistValidator = null, StreamHandler? streamHandler = null)
    {
        _httpClient = httpClient ?? HttpClientManager.GetSharedClient();
        _whitelistValidator = whitelistValidator ?? new WhitelistValidator();
        _streamHandler = streamHandler ?? new StreamHandler();
    }

    public async Task<string> ForwardAsync(ChatCompletionRequest request)
    {
        var targetUrl = UrlResolver.ResolveUrl(request);
        if (!_whitelistValidator.IsWhitelisted(targetUrl))
            throw new UnauthorizedAccessException($"Endpoint not whitelisted: {targetUrl}");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        if (!string.IsNullOrEmpty(request.ApiKey))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        var payload = new { model = request.Model, messages = request.Messages, temperature = request.Temperature, max_tokens = request.MaxTokens, stream = request.Stream };
        httpRequest.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<Usage> ForwardStreamAsync(ChatCompletionRequest request)
    {
        var targetUrl = UrlResolver.ResolveUrl(request);
        if (!_whitelistValidator.IsWhitelisted(targetUrl))
            throw new UnauthorizedAccessException($"Endpoint not whitelisted: {targetUrl}");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        if (!string.IsNullOrEmpty(request.ApiKey))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        var streamRequest = new { model = request.Model, messages = request.Messages, stream = true, stream_options = new { include_usage = true } };
        var json = JsonSerializer.Serialize(streamRequest);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var stream = ReadSseStream(response.Content);
        return await _streamHandler.HandleStreamAsync(stream);
    }

    private async IAsyncEnumerable<string> ReadSseStream(HttpContent content)
    {
        using var stream = await content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            yield return line;
        }
    }
}