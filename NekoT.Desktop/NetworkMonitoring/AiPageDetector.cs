using System;
using System.Collections.Generic;

namespace NekoT.Desktop.NetworkMonitoring;

public static class AiPageDetector
{
    private static readonly Dictionary<string, AiProviderInfo> AiChatProviders = new()
    {
        ["openai.com"] = new AiProviderInfo { Name = "OpenAI", DisplayName = "ChatGPT", Accuracy = AccuracyLevel.NotSupported },
        ["claude.ai"] = new AiProviderInfo { Name = "Anthropic", DisplayName = "Claude", Accuracy = AccuracyLevel.NotSupported },
        ["doubao.com"] = new AiProviderInfo { Name = "Doubao", DisplayName = "豆包", Accuracy = AccuracyLevel.Estimated },
        ["kimi.com"] = new AiProviderInfo { Name = "Moonshot", DisplayName = "Kimi", Accuracy = AccuracyLevel.NotSupported },
        ["chat.deepseek.com"] = new AiProviderInfo { Name = "DeepSeek", DisplayName = "DeepSeek", Accuracy = AccuracyLevel.Precise },
        ["chatglm.cn"] = new AiProviderInfo { Name = "ZhipuAI", DisplayName = "智谱清言", Accuracy = AccuracyLevel.Precise },
        ["xinghuo.xfyun.cn"] = new AiProviderInfo { Name = "iFlytek", DisplayName = "讯飞星火", Accuracy = AccuracyLevel.Precise },
        ["yiyan.baidu.com"] = new AiProviderInfo { Name = "Baidu", DisplayName = "文心一言", Accuracy = AccuracyLevel.Precise },
        ["tongyi.aliyun.com"] = new AiProviderInfo { Name = "Alibaba", DisplayName = "通义千问", Accuracy = AccuracyLevel.Precise }
    };

    public static bool IsAiChatPage(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var host = uri.Host.ToLowerInvariant();
        foreach (var domain in AiChatProviders.Keys)
        {
            if (host.Contains(domain)) return true;
        }
        return false;
    }

    public static AiProviderInfo? DetectProvider(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        var host = uri.Host.ToLowerInvariant();
        foreach (var (domain, info) in AiChatProviders)
        {
            if (host.Contains(domain)) return info;
        }
        return null;
    }

    public static bool IsMonitoringSupported(string url)
    {
        var provider = DetectProvider(url);
        return provider?.Accuracy == AccuracyLevel.Precise || provider?.Accuracy == AccuracyLevel.Estimated;
    }
}

public class AiProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AccuracyLevel Accuracy { get; set; }
}

public enum AccuracyLevel
{
    Unknown,
    NotSupported,
    Estimated,
    Precise
}
