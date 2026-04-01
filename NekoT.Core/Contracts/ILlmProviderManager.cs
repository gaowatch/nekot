using NekoT.Core.LlmProviders;

namespace NekoT.Core.Contracts;

public interface ILlmProviderManager
{
    IReadOnlyDictionary<string, LlmProvider> Providers { get; }
    LlmProvider? GetProviderByModel(string model);
    LlmProvider? GetProvider(string providerName);
    string? GetApiUrl(string model);
    string? GetDefaultModel(string providerName);
    IEnumerable<LlmProvider> GetAllProviders();
    IEnumerable<ModelDisplayItem> GetSupportedModels(string providerName);
}