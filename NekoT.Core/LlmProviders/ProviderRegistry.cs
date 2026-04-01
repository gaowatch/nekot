using System;
using System.Collections.Generic;
using System.Linq;
using NekoT.Core.Contracts;

namespace NekoT.Core.LlmProviders;

public interface ILlmProviderManager
{
    IReadOnlyDictionary<string, LlmProvider> Providers { get; }
    LlmProvider? GetProvider(string providerName);
    LlmProvider? GetProviderByModel(string model);
    IEnumerable<ModelDisplayItem> GetSupportedModels(string providerName);
    string? GetDefaultModel(string providerName);
}

public class ProviderRegistry : ILlmProviderManager
{
    private readonly Dictionary<string, LlmProvider> _providers;
    private readonly Dictionary<string, string> _modelToProviderMap;

    public ProviderRegistry()
    {
        _providers = LlmProviderDefaults.BuildDefaultProviders();
        _modelToProviderMap = BuildModelToProviderMap();
    }

    private static Dictionary<string, string> BuildModelToProviderMap()
    {
        var providers = LlmProviderDefaults.BuildDefaultProviders();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers.Values)
        {
            foreach (var model in provider.SupportedModels)
            {
                map[model.Id] = provider.Name;
                map[model.Alias] = provider.Name;

                foreach (var keyword in provider.ModelKeywords)
                {
                    if (model.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        map[keyword] = provider.Name;
                    }
                }
            }
        }

        return map;
    }

    public IReadOnlyDictionary<string, LlmProvider> Providers => _providers;

    public LlmProvider? GetProvider(string providerName)
    {
        if (string.IsNullOrEmpty(providerName))
            return null;

        if (_providers.TryGetValue(providerName, out var provider))
            return provider;

        foreach (var p in _providers.Values)
        {
            if (p.Alias.Equals(providerName, StringComparison.OrdinalIgnoreCase))
                return p;
        }

        return null;
    }

    public LlmProvider? GetProviderByModel(string model)
    {
        if (string.IsNullOrEmpty(model))
            return null;

        if (_modelToProviderMap.TryGetValue(model, out var providerName))
        {
            return _providers.GetValueOrDefault(providerName);
        }

        var modelLower = model.ToLowerInvariant();
        foreach (var provider in _providers.Values)
        {
            foreach (var keyword in provider.ModelKeywords)
            {
                if (modelLower.Contains(keyword.ToLowerInvariant()))
                {
                    return provider;
                }
            }
        }

        return null;
    }

    public IEnumerable<ModelDisplayItem> GetSupportedModels(string providerName)
    {
        var provider = GetProvider(providerName);
        return provider?.SupportedModels ?? Enumerable.Empty<ModelDisplayItem>();
    }

    public string? GetDefaultModel(string providerName)
    {
        var provider = GetProvider(providerName);
        return provider?.DefaultModel;
    }
}