using NekoT.Core.Contracts;

namespace NekoT.Core.LlmProviders;

public class ModelDisplayItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class LlmProvider
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string[] ModelKeywords { get; set; } = Array.Empty<string>();
    public string[] HostPatterns { get; set; } = Array.Empty<string>();
    public string DefaultModel { get; set; } = string.Empty;
    public ModelDisplayItem[] SupportedModels { get; set; } = Array.Empty<ModelDisplayItem>();
}

public class LlmProviderManager : ILlmProviderManager
{
    private static readonly Lazy<LlmProviderManager> _instance = new(() => new LlmProviderManager(), LazyThreadSafetyMode.ExecutionAndPublication);
    public static LlmProviderManager Instance => _instance.Value;
    private readonly Dictionary<string, LlmProvider> _providers;

    public LlmProviderManager() { _providers = LlmProviderDefaults.BuildDefaultProviders(); }
    internal LlmProviderManager(Dictionary<string, LlmProvider> providers) { _providers = providers ?? throw new ArgumentNullException(nameof(providers)); }
    public IReadOnlyDictionary<string, LlmProvider> Providers => _providers;

    public LlmProvider? GetProviderByModel(string model)
    {
        if (string.IsNullOrEmpty(model)) return null;
        var modelLower = model.ToLowerInvariant();
        foreach (var provider in _providers.Values)
        {
            foreach (var keyword in provider.ModelKeywords)
            {
                if (modelLower.Contains(keyword.ToLowerInvariant())) return provider;
            }
        }
        return null;
    }

    public string? GetApiUrl(string model) => GetProviderByModel(model)?.ApiUrl;
    public string? GetDefaultModel(string providerName) => _providers.TryGetValue(providerName, out var provider) ? provider.DefaultModel : null;
    public IEnumerable<LlmProvider> GetAllProviders() => _providers.Values;
    public IEnumerable<ModelDisplayItem> GetSupportedModels(string providerName) => _providers.TryGetValue(providerName, out var provider) ? provider.SupportedModels : Array.Empty<ModelDisplayItem>();
    public LlmProvider? GetProvider(string providerName) => _providers.TryGetValue(providerName, out var provider) ? provider : null;
}