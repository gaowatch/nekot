using NekoT.Models.Requests;

namespace NekoT.Core.Forwarding;

public static class UrlResolver
{
    private static readonly Dictionary<string, string> DefaultUrls = ProviderDefaults.DefaultUrls;
    private static readonly string DefaultOpenAiUrl = ProviderDefaults.DefaultOpenAiUrl;

    public static string ResolveUrl(ChatCompletionRequest request)
    {
        var model = request.Model?.ToLowerInvariant() ?? "";
        foreach (var (key, url) in DefaultUrls) { if (model.Contains(key)) return url; }
        if (!string.IsNullOrEmpty(request.Url) && Uri.TryCreate(request.Url, UriKind.Absolute, out _)) return request.Url;
        return DefaultOpenAiUrl;
    }

    public static string ResolveUrl(string? model, string? customUrl = null)
    {
        var modelLower = model?.ToLowerInvariant() ?? "";
        foreach (var (key, url) in DefaultUrls) { if (modelLower.Contains(key)) return url; }
        if (!string.IsNullOrEmpty(customUrl) && Uri.TryCreate(customUrl, UriKind.Absolute, out _)) return customUrl;
        return DefaultOpenAiUrl;
    }
}