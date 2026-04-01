using System;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace NekoT.Desktop.NetworkMonitoring;

/// <summary>
/// 网络监控接口，用于监控 WebView2 网络请求并提取 Token 信息
/// </summary>
public interface INetworkMonitor
{
    /// <summary>
    /// Token 提取事件
    /// </summary>
    event EventHandler<TokenExtractedEventArgs>? TokenExtracted;

    /// <summary>
    /// 流量统计更新事件（每秒触发一次）
    /// </summary>
    event EventHandler<TrafficStatsEventArgs>? TrafficUpdated;

    /// <summary>
    /// 开始监控指定的 WebView2 实例
    /// </summary>
    /// <param name="webView">要监控的 CoreWebView2 实例</param>
    void StartMonitoring(CoreWebView2 webView);

    /// <summary>
    /// 停止监控
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 是否正在监控
    /// </summary>
    bool IsMonitoring { get; }
}

/// <summary>
/// Token提取事件参数
/// 注意：此类包含敏感数据，使用后应尽快让GC回收
/// </summary>
public class TokenExtractedEventArgs : EventArgs
{
    // Token 使用量相关
    public int Tokens { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Model { get; set; }
    public string? PromptTokens { get; set; }
    public string? CompletionTokens { get; set; }

    // 认证信息相关
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Token的安全Hash前缀（HMAC-SHA256，128位）
    /// 用于安全标识Token，不暴露原始Token值
    /// </summary>
    public string? TokenHashPrefix { get; set; }

    /// <summary>
    /// 是否为认证信息提取事件
    /// </summary>
    public bool IsAuthExtraction => !string.IsNullOrEmpty(TokenType) && TokenType != "Unknown";

    // 安全说明：不再存储RawToken，避免敏感数据以明文形式驻留内存
    // 如需临时处理Token，请在使用后立即清除变量引用
}

/// <summary>
/// 流量统计事件参数（极简设计，零性能影响）
/// </summary>
public class TrafficStatsEventArgs : EventArgs
{
    /// <summary>
    /// 上传速度（字节/秒）
    /// </summary>
    public double UploadSpeed { get; init; }

    /// <summary>
    /// 下载速度（字节/秒）
    /// </summary>
    public double DownloadSpeed { get; init; }

    /// <summary>
    /// 统计时间戳
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// 格式化上传速度为可读字符串
    /// </summary>
    public string UploadSpeedFormatted => FormatSpeed(UploadSpeed);

    /// <summary>
    /// 格式化下载速度为可读字符串
    /// </summary>
    public string DownloadSpeedFormatted => FormatSpeed(DownloadSpeed);

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:F0} B/s";
        else if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:F1} KB/s";
        else if (bytesPerSecond < 1024 * 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        else
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
    }
}