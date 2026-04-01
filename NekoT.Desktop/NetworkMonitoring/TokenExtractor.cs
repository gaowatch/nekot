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
        "openai.com", "api.openai.com", "anthropic.com", "api.anthropic.com",
        "minimax.chat", "api.minimax.chat", "deepseek.com", "api.deepseek.com",
        "moonshot.cn", "api.moonshot.cn", "kimi.com", "api.kimi.com",
        "zhipuai.cn", "open.bigmodel.cn", "dashscope.aliyuncs.com", "aigc.siliconflow.cn",
        "api.siliconflow.cn", "doubao.com", "www.doubao.com", "wss100-normal.doubao.com",
        "wss.doubao.com", "mcs.doubao.com", "yiyan.baidu.com", "aip.baidubce.com",
        "xinghuo.xfyun.cn", "tongyi.aliyun.com", "qwenlm.aliyun.com"
    };

    public static bool IsLlmApiRequest(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.ToLowerInvariant();
        var pathAndQuery = uri.PathAndQuery.ToLowerInvariant();

        if (host.EndsWith("doubao.com", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") || pathAndQuery.Contains("/obj/flow-doubao") ||
                pathAndQuery.Contains(".js") || pathAndQuery.Contains(".css") ||
                pathAndQuery.Contains(".png") || pathAndQuery.Contains(".jpg") ||
                pathAndQuery.Contains(".woff") || pathAndQuery.Contains(".ico") ||
                pathAndQuery.Contains("monitor_browser")) return false;
            if (pathAndQuery.Contains("/chat/completion") || pathAndQuery.Contains("/im/chain") ||
                pathAndQuery.Contains("/im/conversation") || pathAndQuery.Contains("/im/message") ||
                pathAndQuery.Contains("/api/") || pathAndQuery.Contains("/list") ||
                uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        if (host.EndsWith("kimi.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("moonshot.cn", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") || pathAndQuery.Contains("/assets/") ||
                pathAndQuery.Contains("/kimi-web") || pathAndQuery.Contains("/kimi-web-seo") ||
                pathAndQuery.Contains(".js") || pathAndQuery.Contains(".css") ||
                pathAndQuery.Contains(".png") || pathAndQuery.Contains(".jpg") ||
                pathAndQuery.Contains(".woff") || pathAndQuery.Contains(".ico") ||
                pathAndQuery.Contains(".riv") || pathAndQuery.Contains(".ttf")) return false;
            if (pathAndQuery == "/" || pathAndQuery == "/zh/" ||
                (pathAndQuery.StartsWith("/chat/") && !pathAndQuery.Contains("/apiv2/"))) return false;
            if (pathAndQuery.Contains("/apiv2/") || pathAndQuery.Contains("/v1/chat") ||
                pathAndQuery.Contains("/api/")) return true;
            return false;
        }

        if (host.EndsWith("deepseek.com", StringComparison.OrdinalIgnoreCase))
        {
            if (pathAndQuery.Contains("/static/") || pathAndQuery.Contains("/chat/static/") ||
                pathAndQuery.Contains("/fe-static/") || pathAndQuery.Contains(".js") ||
                pathAndQuery.Contains(".css") || pathAndQuery.Contains(".png") ||
                pathAndQuery.Contains(".jpg") || pathAndQuery.Contains(".woff") ||
                pathAndQuery.Contains(".ico") || pathAndQuery.Contains(".wasm") ||
                pathAndQuery.Contains(".ttf")) return false;
            if (pathAndQuery.Contains("/api/") || pathAndQuery.Contains("/chat/completions") ||
                pathAndQuery.Contains("/v1/chat")) return true;
            return false;
        }

        return LlmApiHosts.Any(allowedHost =>
            host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + allowedHost, StringComparison.OrdinalIgnoreCase) && host.Length > allowedHost.Length + 1);
    }

    public static string? DetectProvider(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.ToLowerInvariant();

        if (IsExactDomainOrSubdomain(host, "openai.com")) return "OpenAI";
        if (IsExactDomainOrSubdomain(host, "anthropic.com")) return "Anthropic";
        if (IsExactDomainOrSubdomain(host, "minimax.chat")) return "MiniMax";
        if (IsExactDomainOrSubdomain(host, "deepseek.com")) return "DeepSeek";
        if (IsExactDomainOrSubdomain(host, "moonshot.cn") || IsExactDomainOrSubdomain(host, "kimi.com")) return "Moonshot";
        if (IsExactDomainOrSubdomain(host, "zhipuai.cn") || IsExactDomainOrSubdomain(host, "bigmodel.cn")) return "ZhipuAI";
        if (IsExactDomainOrSubdomain(host, "dashscope.aliyuncs.com") || IsExactDomainOrSubdomain(host, "tongyi.aliyun.com") ||
            IsExactDomainOrSubdomain(host, "qwenlm.aliyun.com")) return "Alibaba";
        if (IsExactDomainOrSubdomain(host, "siliconflow.cn")) return "SiliconFlow";
        if (host.EndsWith("doubao.com", StringComparison.OrdinalIgnoreCase)) return "Doubao";
        if (IsExactDomainOrSubdomain(host, "yiyan.baidu.com") || IsExactDomainOrSubdomain(host, "aip.baidubce.com")) return "Baidu";
        if (IsExactDomainOrSubdomain(host, "xinghuo.xfyun.cn")) return "iFlytek";
        return "Unknown LLM";
    }

    private static bool IsExactDomainOrSubdomain(string host, string allowedDomain)
    {
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(allowedDomain)) return false;
        return host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||
               (host.EndsWith("." + allowedDomain, StringComparison.OrdinalIgnoreCase) && host.Length > allowedDomain.Length + 1);
    }
}
