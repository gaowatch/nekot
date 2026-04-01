using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace NekoT.Desktop.NetworkMonitoring;

public static class TokenExtractor
{
    private static readonly string[] LlmApiHosts = new[]
    {
        "openai.com", "api.openai.com",
        "anthropic.com", "api.anthropic.com",
        "minimax.chat", "api.minimax.chat",
        "deepseek.com", "api.deepseek.com",
        "moonshot.cn", "api.moonshot.cn",
        "kimi.com", "api.kimi.com",
        "zhipuai.cn", "open.bigmodel.cn",
        "dashscope.aliyuncs.com",
        "aigc.siliconflow.cn", "api.siliconflow.cn",
        "doubao.com", "www.doubao.com",
        "yiyan.baidu.com", "aip.baidubce.com",
        "xinghuo.xfyun.cn",
        "tongyi.aliyun.com", "qwenlm.aliyun.com"
    };

    public static bool IsLlmApiRequest(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var host = uri.Host.ToLowerInvariant();
        var pathAndQuery = uri.PathAndQuery.ToLowerInvariant();

        if (host.EndsWith("doubao.com", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") || pathAndQuery.Contains(".js") ||
                pathAndQuery.Contains(".css") || pathAndQuery.Contains(".png"))
                return false;
            if (pathAndQuery.Contains("/chat/completion") || pathAndQuery.Contains("/api/") ||
                uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        if (host.EndsWith("kimi.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("moonshot.cn", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") || pathAndQuery.Contains(".js"))
                return false;
            if (pathAndQuery.Contains("/apiv2/") || pathAndQuery.Contains("/v1/chat"))
                return true;
            return false;
        }

        if (host.EndsWith("deepseek.com", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") || pathAndQuery.Contains(".js"))
                return false;
            if (pathAndQuery.Contains("/api/") || pathAndQuery.Contains("/chat/completions"))
                return true;
            return false;
        }

        return LlmApiHosts.Any(allowedHost =>
            host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + allowedHost, StringComparison.OrdinalIgnoreCase));
    }

    public static string? DetectProvider(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        var host = uri.Host.ToLowerInvariant();

        if (host.Contains("openai.com")) return "OpenAI";
        if (host.Contains("anthropic.com")) return "Anthropic";
        if (host.Contains("minimax.chat")) return "MiniMax";
        if (host.Contains("deepseek.com")) return "DeepSeek";
        if (host.Contains("moonshot.cn") || host.Contains("kimi.com")) return "Moonshot";
        if (host.Contains("zhipuai.cn") || host.Contains("bigmodel.cn")) return "ZhipuAI";
        if (host.Contains("dashscope.aliyuncs.com") || host.Contains("tongyi.aliyun.com")) return "Alibaba";
        if (host.Contains("siliconflow.cn")) return "SiliconFlow";
        if (host.Contains("doubao.com")) return "Doubao";
        if (host.Contains("yiyan.baidu.com") || host.Contains("baidubce.com")) return "Baidu";
        if (host.Contains("xinghuo.xfyun.cn")) return "iFlytek";

        return "Unknown LLM";
    }

    public static TokenExtractedEventArgs? ExtractTokensFromResponse(string responseBody, string url)
    {
        if (string.IsNullOrEmpty(responseBody)) return null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var provider = DetectProvider(url) ?? "Unknown";
            var args = new TokenExtractedEventArgs
                       {
                Provider = provider,
                RequestUrl = url,
                Timestamp = DateTime.Now
            };

            int totalTokens = 0, promptTokens = 0, completionTokens = 0;

            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("total_tokens", out var totalElem))
                    totalTokens = totalElem.GetInt32();
                if (usage.TryGetProperty("prompt_tokens", out var promptElem))
                    promptTokens = promptElem.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var completionElem))
                    completionTokens = completionElem.GetInt32();
            }

            if (totalTokens > 0)
            {
                args.Tokens = totalTokens;
                args.PromptTokens = promptTokens.ToString();
                args.CompletionTokens = completionTokens.ToString();
            }

            if (root.TryGetProperty("model", out var modelElem))
                args.Model = modelElem.GetString();

            return args.Tokens > 0 ? args : null;
        }
        catch
        {
            return null;
        }
    }
}

public class TokenExtractedEventArgs : EventArgs
{
    public string Provider { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Tokens { get; set; }
    public string PromptTokens { get; set; } = string.Empty;
    public string CompletionTokens { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string TokenType { get; set; } = "Unknown";
    public string? TokenHashPrefix { get; set; }
}
