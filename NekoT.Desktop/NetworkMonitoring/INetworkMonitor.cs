using System;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace NekoT.Desktop.NetworkMonitoring;

public interface INetworkMonitor
{
    event EventHandler<TokenExtractedEventArgs>? TokenExtracted;
    event EventHandler<TrafficStatsEventArgs>? TrafficUpdated;
    void StartMonitoring(CoreWebView2 webView);
    void StopMonitoring();
    bool IsMonitoring { get; }
}

public class TokenExtractedEventArgs : EventArgs
{
    public int Tokens { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Model { get; set; }
    public string? PromptTokens { get; set; }
    public string? CompletionTokens { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public string? TokenHashPrefix { get; set; }
    public bool IsAuthExtraction => !string.IsNullOrEmpty(TokenType) && TokenType != "Unknown";
}

public class TrafficStatsEventArgs : EventArgs
{
    public double UploadSpeed { get; init; }
    public double DownloadSpeed { get; init; }
    public DateTime Timestamp { get; init; }
    public string UploadSpeedFormatted => FormatSpeed(UploadSpeed);
    public string DownloadSpeedFormatted => FormatSpeed(DownloadSpeed);

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024) return $"{bytesPerSecond:F0} B/s";
        else if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F1} KB/s";
        else if (bytesPerSecond < 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        else return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
    }
}