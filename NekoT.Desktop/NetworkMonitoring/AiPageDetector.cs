using System;
using System.Collections.Generic;
using System.IO;

namespace NekoT.Desktop.NetworkMonitoring;

public static class AiPageDetector
{
    private static readonly string LogFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "token_monitor.log");
    private static readonly object LogLock = new object();
    
    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try
        {
            lock (LogLock)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine, System.Text.Encoding.UTF8);
            }
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(msg);
    }
    
    private static readonly Dictionary<string, AiProviderInfo> AiChatProviders = new()
    {
        ["openai.com"] = new AiProviderInfo
        {
            Name = "OpenAI",
            DisplayName = "ChatGPT",
            ChatPatterns = new[] { "/chat", "/c/" },
            ApiHosts = new[] { "api.openai.com", "openai.com" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["chat.openai.com"] = new AiProviderInfo
        {
            Name = "OpenAI",
            DisplayName = "ChatGPT",
            ChatPatterns = new[] { "/" },
            ApiHosts = new[] { "api.openai.com" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["claude.ai"] = new AiProviderInfo
        {
            Name = "Anthropic",
            DisplayName = "Claude",
            ChatPatterns = new[] { "/chat", "/new" },
            ApiHosts = new[] { "api.anthropic.com" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["anthropic.com"] = new AiProviderInfo
        {
            Name = "Anthropic",
            DisplayName = "Claude",
            ChatPatterns = new[] { "/" },
            ApiHosts = new[] { "api.anthropic.com" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["gemini.google.com"] = new AiProviderInfo
        {
            Name = "Google",
            DisplayName = "Gemini",
            ChatPatterns = new[] { "/app" },
            ApiHosts = new[] { "generativelanguage.googleapis.com" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["poe.com"] = new AiProviderInfo
        {
            Name = "Quora",
            DisplayName = "Poe",
            ChatPatterns = new[] { "/" },
            ApiHosts = Array.Empty<string>(),
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["perplexity.ai"] = new AiProviderInfo
        {
            Name = "Perplexity",
            DisplayName = "Perplexity",
            ChatPatterns = new[] { "/" },
            ApiHosts = new[] { "api.perplexity.ai" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "不支持监控（网页版私有协议）"
        },
        ["yiyan.baidu.com"] = new AiProviderInfo
        {
            Name = "Baidu",
            DisplayName = "文心一言",
            ChatPatterns = new[] { "/chat" },
            ApiHosts = new[] { "aip.baidubce.com", "yiyan.baidu.com" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["tongyi.aliyun.com"] = new AiProviderInfo
        {
            Name = "Alibaba",
            DisplayName = "通义千问",
            ChatPatterns = new[] { "/conversation" },
            ApiHosts = new[] { "dashscope.aliyuncs.com", "tongyi.aliyun.com" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["qwenlm.aliyun.com"] = new AiProviderInfo
        {
            Name = "Alibaba",
            DisplayName = "通义千问",
            ChatPatterns = new[] { "/" },
            ApiHosts = new[] { "dashscope.aliyuncs.com" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["doubao.com"] = new AiProviderInfo
        {
            Name = "Doubao",
            DisplayName = "豆包",
            ChatPatterns = new[] { "/chat/", "/conversation/" },
            ApiHosts = new[] { "mcs.doubao.com", "doubao.com" },
            Accuracy = AccuracyLevel.Estimated,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["kimi.com"] = new AiProviderInfo
        {
            Name = "Moonshot",
            DisplayName = "Kimi",
            ChatPatterns = new[] { "/", "/zh/" },
            ApiHosts = new[] { "api.moonshot.cn", "kimi.com" },
            Accuracy = AccuracyLevel.NotSupported,
            Note = "该功能正在开发中，敬请期待"
        },
        ["chat.deepseek.com"] = new AiProviderInfo
        {
            Name = "DeepSeek",
            DisplayName = "DeepSeek",
            ChatPatterns = new[] { "/" },
            ApiHosts = new[] { "api.deepseek.com" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["chatglm.cn"] = new AiProviderInfo
        {
            Name = "ZhipuAI",
            DisplayName = "智谱清言",
            ChatPatterns = new[] { "/" },
            ApiHosts = new[] { "open.bigmodel.cn" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["xinghuo.xfyun.cn"] = new AiProviderInfo
        {
            Name = "iFlytek",
            DisplayName = "讯飞星火",
            ChatPatterns = new[] { "/chat" },
            ApiHosts = new[] { "xinghuo.xfyun.cn" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        },
        ["minimax.chat"] = new AiProviderInfo
        {
            Name = "MiniMax",
            DisplayName = "MiniMax",
            ChatPatterns = new[] { "/chat" },
            ApiHosts = new[] { "api.minimax.chat" },
            Accuracy = AccuracyLevel.Precise,
            Note = "Token 计算可能有误差，仅供参考"
        }
    };

    public static bool IsAiChatPage(string url)
    {
        Log($"[AiPageDetector] IsAiChatPage called with URL: {url}");
        
        if (string.IsNullOrEmpty(url))
        {
            Log("[AiPageDetector] URL is null or empty, returning false");
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            Log($"[AiPageDetector] Failed to parse URL: {url}");
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        var path = uri.PathAndQuery.ToLowerInvariant();
        Log($"[AiPageDetector] Parsed URL - Host: {host}, Path: {path}");

        foreach (var (domain, info) in AiChatProviders)
        {
            if (host.Contains(domain))
            {
                Log($"[AiPageDetector] Host matched domain: {domain}");
                foreach (var pattern in info.ChatPatterns)
                {
                    Log($"[AiPageDetector] Checking pattern: {pattern} against path: {path}");
                    if (path.StartsWith(pattern))
                    {
                        Log($"[AiPageDetector] MATCH FOUND! Provider: {info.DisplayName}");
                        return true;
                    }
                }
            }
        }

        Log($"[AiPageDetector] No match found for URL: {url}");
        return false;
    }

    public static AiProviderInfo? DetectProvider(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.ToLowerInvariant();

        foreach (var (domain, info) in AiChatProviders)
        {
            if (host.Contains(domain))
            {
                return info;
            }
        }

        return null;
    }

    public static AccuracyLevel GetAccuracyLevel(string url)
    {
        var provider = DetectProvider(url);
        return provider?.Accuracy ?? AccuracyLevel.Unknown;
    }

    public static bool IsMonitoringSupported(string url)
    {
        var accuracy = GetAccuracyLevel(url);
        return accuracy == AccuracyLevel.Precise || accuracy == AccuracyLevel.Estimated;
    }
}

public class AiProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] ChatPatterns { get; set; } = Array.Empty<string>();
    public string[] ApiHosts { get; set; } = Array.Empty<string>();
    public AccuracyLevel Accuracy { get; set; }
    public string Note { get; set; } = string.Empty;
}

public enum AccuracyLevel
{
    Unknown,
    NotSupported,
    Estimated,
    Precise
}