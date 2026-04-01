namespace NekoT.Core.LlmProviders;

public interface IProviderRegistry
{
    LlmProvider? DetectProviderByUrl(string url);
    LlmProvider? DetectProviderByModel(string model);
    bool IsLlmApiRequest(string url);
    IReadOnlyDictionary<string, LlmProvider> GetAllProviders();
    IReadOnlySet<string> GetAllowedHosts();
}