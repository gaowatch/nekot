using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using NekoT.Desktop.Services.Logging;

namespace NekoT.Desktop.NetworkMonitoring;

public class WebView2NetworkMonitor : INetworkMonitor, IDisposable
{
    private static readonly ILoggerService Logger = LoggerService.Instance;
    private const string LogCategory = "NetworkMonitor";

    private CoreWebView2? _webView;
    private bool _isMonitoring;
    private readonly HashSet<string> _pendingRequests = new();
    private readonly Dictionary<string, string> _requestUrls = new();
    private readonly Dictionary<string, DateTime> _requestTimestamps = new();
    private readonly object _lockObject = new();
    private readonly HashSet<string> _processedRequests = new();
    private readonly Queue<string> _processedQueue = new();
    private const int ProcessedCacheSize = 200;
    private const int MaxPendingRequests = 500;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);
    private System.Timers.Timer? _cleanupTimer;
    private bool _disposed;
    private CoreWebView2DevToolsProtocolEventReceiver? _requestReceiver;
    private CoreWebView2DevToolsProtocolEventReceiver? _responseReceiver;
    private CoreWebView2DevToolsProtocolEventReceiver? _loadingFinishedReceiver;
    private long _currentUploadBytes;
    private long _currentDownloadBytes;
    private DateTime _lastTrafficCalcTime;
    private System.Timers.Timer? _trafficCalcTimer;

    private void StartCleanupTimer()
    {
        _cleanupTimer = new System.Timers.Timer(60000);
        _cleanupTimer.Elapsed += CleanupStaleRequests;
        _cleanupTimer.AutoReset = true;
        _cleanupTimer.Start();
        Logger.LogInfo(LogCategory, "Cleanup timer started (60s)");
    }

    private void StartTrafficCalcTimer()
    {
        _lastTrafficCalcTime = DateTime.UtcNow;
        _trafficCalcTimer = new System.Timers.Timer(2000);
        _trafficCalcTimer.Elapsed += CalculateTrafficSpeed;
        _trafficCalcTimer.AutoReset = true;
        _trafficCalcTimer.Start();
        Logger.LogInfo(LogCategory, "Traffic calc timer started (2s)");
    }

    private void CalculateTrafficSpeed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_disposed) return;
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastTrafficCalcTime).TotalSeconds;
            if (elapsed > 0)
            {
                var uploadBytes = Interlocked.Exchange(ref _currentUploadBytes, 0);
                var downloadBytes = Interlocked.Exchange(ref _currentDownloadBytes, 0);
                var uploadSpeed = uploadBytes / elapsed;
                var downloadSpeed = downloadBytes / elapsed;
                _lastTrafficCalcTime = now;
                if (TrafficUpdated != null && (uploadSpeed > 0 || downloadSpeed > 0))
                {
                    TrafficUpdated?.Invoke(this, new TrafficStatsEventArgs
                    {
                        UploadSpeed = uploadSpeed, DownloadSpeed = downloadSpeed, Timestamp = now
                    });
                }
            }
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"Traffic calc error: {ex.Message}"); }
    }

    private void CleanupStaleRequests(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_disposed) return;
        try
        {
            var now = DateTime.Now;
            var staleIds = new List<string>();
            lock (_lockObject)
            {
                foreach (var kvp in _requestTimestamps)
                    if (now - kvp.Value > RequestTimeout) staleIds.Add(kvp.Key);
                foreach (var id in staleIds)
                {
                    _pendingRequests.Remove(id);
                    _requestUrls.Remove(id);
                    _requestTimestamps.Remove(id);
                }
                if (_pendingRequests.Count > MaxPendingRequests)
                {
                    var toRemove = _requestTimestamps.OrderBy(x => x.Value)
                        .Take(_pendingRequests.Count - MaxPendingRequests).Select(x => x.Key).ToList();
                    foreach (var id in toRemove)
                    {
                        _pendingRequests.Remove(id);
                        _requestUrls.Remove(id);
                        _requestTimestamps.Remove(id);
                    }
                }
            }
            if (staleIds.Count > 0) Logger.LogInfo(LogCategory, $"Cleaned up {staleIds.Count} stale requests");
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"Cleanup error: {ex.Message}"); }
    }

    public event EventHandler<TokenExtractedEventArgs>? TokenExtracted;
    public event EventHandler<TrafficStatsEventArgs>? TrafficUpdated;
    public bool IsMonitoring => _isMonitoring;

    public void StartMonitoring(CoreWebView2 webView)
    {
        if (_isMonitoring && ReferenceEquals(_webView, webView)) return;
        if (_isMonitoring && !ReferenceEquals(_webView, webView)) { StopMonitoring(); }
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        SetupNetworkMonitoring();
        StartCleanupTimer();
        StartTrafficCalcTimer();
        _isMonitoring = true;
        Logger.LogInfo(LogCategory, "Monitoring started");
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;
        if (_cleanupTimer != null) { try { _cleanupTimer.Stop(); _cleanupTimer.Dispose(); } catch { } _cleanupTimer = null; }
        if (_trafficCalcTimer != null) { try { _trafficCalcTimer.Stop(); _trafficCalcTimer.Dispose(); } catch { } _trafficCalcTimer = null; }
        if (_webView != null)
        {
            try
            {
                if (_requestReceiver != null) _requestReceiver.DevToolsProtocolEventReceived -= OnNetworkRequestWillBeSent;
                if (_responseReceiver != null) _responseReceiver.DevToolsProtocolEventReceived -= OnNetworkResponseReceived;
                if (_loadingFinishedReceiver != null) _loadingFinishedReceiver.DevToolsProtocolEventReceived -= OnNetworkLoadingFinished;
                _webView.CallDevToolsProtocolMethodAsync("Network.disable", "{}").SafeFireAndForget();
            }
            catch { }
        }
        lock (_lockObject) { _pendingRequests.Clear(); _requestUrls.Clear(); _requestTimestamps.Clear(); }
        _isMonitoring = false;
        Logger.LogInfo(LogCategory, "Monitoring stopped");
    }

    private void SetupNetworkMonitoring()
    {
        if (_webView == null) return;
        try
        {
            _requestReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.requestWillBeSent");
            _requestReceiver.DevToolsProtocolEventReceived += OnNetworkRequestWillBeSent;
            _responseReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.responseReceived");
            _responseReceiver.DevToolsProtocolEventReceived += OnNetworkResponseReceived;
            _loadingFinishedReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.loadingFinished");
            _loadingFinishedReceiver.DevToolsProtocolEventReceived += OnNetworkLoadingFinished;
            _webView.CallDevToolsProtocolMethodAsync("Network.enable", "{}").SafeFireAndForget();
            Logger.LogInfo(LogCategory, "CDP monitoring enabled");
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"Setup error: {ex.Message}"); }
    }

    private void OnNetworkRequestWillBeSent(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;
            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("request", out var request) && request.TryGetProperty("url", out var urlElem))
            {
                var url = urlElem.GetString();
                if (!string.IsNullOrEmpty(url) && TokenExtractor.IsLlmApiRequest(url))
                {
                    var requestId = root.TryGetProperty("requestId", out var idElem) ? idElem.GetString() : Guid.NewGuid().ToString();
                    if (requestId != null)
                    {
                        if (request.TryGetProperty("headers", out var headers))
                        {
                            var authInfo = TokenExtractor.ExtractAuthFromHeaders(headers, url);
                            if (authInfo != null) TokenExtracted?.Invoke(this, authInfo);
                        }
                        lock (_lockObject) { _pendingRequests.Add(requestId); _requestTimestamps[requestId] = DateTime.Now; }
                        Logger.LogInfo(LogCategory, $"Tracking LLM request: {requestId}");
                    }
                }
            }
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"requestWillBeSent error: {ex.Message}"); }
    }

    private void OnNetworkResponseReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;
            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("requestId", out var idElem)) return;
            var requestId = idElem.GetString();
            if (string.IsNullOrEmpty(requestId)) return;
            bool isPending; lock (_lockObject) { isPending = _pendingRequests.Contains(requestId); }
            if (!isPending) return;
            if (root.TryGetProperty("response", out var response) && response.TryGetProperty("url", out var urlElem))
            {
                var url = urlElem.GetString();
                if (!string.IsNullOrEmpty(url) && TokenExtractor.IsLlmApiRequest(url))
                    lock (_lockObject) { _requestUrls[requestId] = url; }
            }
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"responseReceived error: {ex.Message}"); }
    }

    private void OnNetworkLoadingFinished(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;
            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("requestId", out var idElem)) return;
            var requestId = idElem.GetString();
            if (string.IsNullOrEmpty(requestId)) return;
            string? url = null; bool isPending;
            lock (_lockObject) { isPending = _pendingRequests.Contains(requestId); if (isPending) _requestUrls.TryGetValue(requestId, out url); }
            if (isPending && !string.IsNullOrEmpty(url))
                FetchAndProcessResponseBodyAsync(requestId, url).SafeFireAndForget(ex => Logger.LogInfo(LogCategory, $"FetchAndProcess error: {ex.Message}"));
            lock (_lockObject) { _pendingRequests.Remove(requestId); _requestUrls.Remove(requestId); }
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"loadingFinished error: {ex.Message}"); }
    }

    private async Task FetchAndProcessResponseBodyAsync(string requestId, string url)
    {
        if (_webView == null) return;
        if (!_processedRequests.Add(requestId)) { Logger.LogInfo(LogCategory, $"Request already processed: {requestId}"); return; }
        try
        {
            var result = await _webView.CallDevToolsProtocolMethodAsync("Network.getResponseBody", JsonSerializer.Serialize(new { requestId }));
            if (string.IsNullOrEmpty(result)) return;
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;
            if (root.TryGetProperty("body", out var bodyElem))
            {
                var body = bodyElem.GetString();
                if (!string.IsNullOrEmpty(body))
                {
                    if (root.TryGetProperty("base64Encoded", out var base64Elem) && base64Elem.GetBoolean())
                        body = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(body));
                    var tokenArgs = TokenExtractor.ExtractTokensFromResponse(body, url);
                    if (tokenArgs != null) { Logger.LogInfo(LogCategory, $"Token extracted: {tokenArgs.Tokens}"); TokenExtracted?.Invoke(this, tokenArgs); }
                }
            }
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"FetchAndProcess error: {ex.Message}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopMonitoring();
        _cleanupTimer?.Stop(); _cleanupTimer?.Dispose();
        _trafficCalcTimer?.Stop(); _trafficCalcTimer?.Dispose();
        _disposed = true;
    }
}