using NekoT.Models.Requests;

namespace NekoT.Core.Forwarding;

public static class UrlResolver
{
    private static readonly Dictionary<string, string> DefaultUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        { "minimax", "https://api.minimaxi.com/v1/text/chatcompletion_v2" },
        { "gpt", "https://api.openai.com/v1/chat/completions" },
        { "claude", "https://api.anthropic.com/v1/messages" },
        { "deepseek", "https://api.deepseek.com/v1/chat/completions" },
        { "moonshot", "https://api.moonshot.cn/v1/chat/completions" },
        { "zhipu", "https://open.bigmodel.cn/api/paas/v4/chat/completions" }
    };

    private const string DefaultOpenAiUrl = "https://api.openai.com/v1/chat/completions";

    public static string ResolveUrl(ChatCompletionRequest request)
    {
        var model = request.Model?.ToLowerInvariant() ?? "";

        foreach (var (key, url) in DefaultUrls)
        {
            if (model.Contains(key))
                return url;
        }

        if (!string.IsNullOrEmpty(request.Url) && Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return request.Url;

        return DefaultOpenAiUrl;
    }
}
