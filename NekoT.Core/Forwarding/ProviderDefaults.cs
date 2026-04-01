namespace NekoT.Core.Forwarding;

public static class ProviderDefaults
{
    public static readonly Dictionary<string, string> DefaultUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        { "minimax", "https://api.minimaxi.com/v1/text/chatcompletion_v2" },
        { "minimaxi", "https://api.minimaxi.com/v1/text/chatcompletion_v2" },
        { "M2.5", "https://api.minimaxi.com/v1/text/chatcompletion_v2" },
        { "abab", "https://api.minimaxi.com/v1/text/chatcompletion_v2" },
        { "gpt", "https://api.openai.com/v1/chat/completions" },
        { "claude", "https://api.anthropic.com/v1/messages" },
        { "openai", "https://api.openai.com/v1/chat/completions" },
        { "deepseek", "https://api.deepseek.com/v1/chat/completions" },
        { "moonshot", "https://api.moonshot.cn/v1/chat/completions" },
        { "zhipu", "https://open.bigmodel.cn/api/paas/v4/chat/completions" }
    };

    public const string DefaultOpenAiUrl = "https://api.openai.com/v1/chat/completions";
}