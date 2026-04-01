namespace NekoT.Core.LlmProviders;

public class ProviderRegistry
{
    private readonly Dictionary<string, LlmProviderBase> _providers = new();
    private readonly object _lock = new();

    public void Register(LlmProviderBase provider)
    {
        lock (_lock)
        {
            _providers[provider.Name] = provider;
        }
    }

    public void Unregister(string name)
    {
        lock (_lock)
        {
            _providers.Remove(name);
        }
    }

    public LlmProviderBase? Get(string name)
    {
        lock (_lock)
        {
            return _providers.TryGetValue(name, out var provider) ? provider : null;
        }
    }

    public IEnumerable<LlmProviderBase> GetAll()
    {
        lock (_lock)
        {
            return _providers.Values.ToList();
        }
    }

    public bool Contains(string name)
    {
        lock (_lock)
        {
            return _providers.ContainsKey(name);
        }
    }

    public async Task<(bool isValid, string? errorMessage)> ValidateProviderUrlAsync(string name, string url)
    {
        var provider = Get(name);
        if (provider == null)
        {
            return (false, $"Provider '{name}' not found");
        }

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode ? (true, null) : (false, $"URL returned status {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool isValid, string? errorMessage)> ValidateLlmApiAsync(string name, string apiKey, string? baseUrl = null)
    {
        var provider = Get(name);
        if (provider == null)
        {
            return (false, $"Provider '{name}' not found");
        }

        try
        {
            var testUrl = baseUrl ?? provider.DefaultApiUrl;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(30);

            var requestContent = new StringContent("{\"model\": \"test\", \"messages\": [{\"role\": \"user\", \"content\": \"test\"}], \"max_tokens\": 1}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{testUrl}/chat/completions", requestContent);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"API returned status {(int)response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}