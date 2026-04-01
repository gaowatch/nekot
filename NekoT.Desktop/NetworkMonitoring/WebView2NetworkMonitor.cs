using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Web.WebView2.Core;
using NekoT.Desktop.Services.Logging;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.NetworkMonitoring;

public class WebView2NetworkMonitor : INetworkMonitor, IDisposable
{
    private static readonly ILoggerService Logger = LoggerService.Instance;
    private const string LogCategory = "NetworkMonitor";

    private CoreWebView2? _webView;
    private bool _isMonitoring;
    private readonly HashSet<string> _pendingRequests = new();
    private readonly Dictionary<string, string> _requestUrls = new();
    private readonly Dictionary<string, string> _webSocketUrls = new();
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
    private CoreWebView2DevToolsProtocolEventReceiver? _webSocketCreatedReceiver;
    private CoreWebView2DevToolsProtocolEventReceiver? _webSocketFrameReceivedReceiver;
    private CoreWebView2DevToolsProtocolEventReceiver? _dataReceivedReceiver;
    private CoreWebView2DevToolsProtocolEventReceiver? _fetchRequestPausedReceiver;

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
        Logger.LogInfo(LogCategory, "NetworkMonitor: Cleanup timer started (60s interval)");
    }

    private void StartTrafficCalcTimer()
    {
        _lastTrafficCalcTime = DateTime.UtcNow;
        _trafficCalcTimer = new System.Timers.Timer(2000);
        _trafficCalcTimer.Elapsed += CalculateTrafficSpeed;
        _trafficCalcTimer.AutoReset = true;
        _trafficCalcTimer.Start();
        Logger.LogInfo(LogCategory, "Traffic: Traffic calculation timer started (2s interval)");
    }

    private void CalculateTrafficSpeed(object? sender, ElapsedEventArgs e)
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
                    var args = new TrafficStatsEventArgs
                    {
                        UploadSpeed = uploadSpeed,
                        DownloadSpeed = downloadSpeed,
                        Timestamp = now
                    };
                    TrafficUpdated?.Invoke(this, args);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"Traffic: Calculate speed error: {ex.Message}");
        }
    }

    private static bool TryExtractLongValue(string json, string fieldName, out long value)
    {
        value = 0;
        var searchStr = $"\"{fieldName}\":";
        var index = json.IndexOf(searchStr, StringComparison.Ordinal);
        if (index < 0) return false;

        var start = index + searchStr.Length;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\t'))
            start++;

        if (start >= json.Length) return false;

        var end = start;
        while (end < json.Length && char.IsDigit(json[end]))
            end++;

        if (end == start) return false;

        return long.TryParse(json.Substring(start, end - start), out value);
    }

    private void CleanupStaleRequests(object? sender, ElapsedEventArgs e)
    {
        if (_disposed) return;
        
        try
        {
            var now = DateTime.Now;
            var staleIds = new List<string>();
            
            lock (_lockObject)
            {
                foreach (var kvp in _requestTimestamps)
                {
                    if (now - kvp.Value > RequestTimeout)
                    {
                        staleIds.Add(kvp.Key);
                    }
                }
                
                foreach (var id in staleIds)
                {
                    _pendingRequests.Remove(id);
                    _requestUrls.Remove(id);
                    _webSocketUrls.Remove(id);
                    _requestTimestamps.Remove(id);
                }
                
                if (_pendingRequests.Count > MaxPendingRequests)
                {
                    var toRemove = _requestTimestamps
                        .OrderBy(x => x.Value)
                        .Take(_pendingRequests.Count - MaxPendingRequests)
                        .Select(x => x.Key)
                        .ToList();
                        
                    foreach (var id in toRemove)
                    {
                        _pendingRequests.Remove(id);
                        _requestUrls.Remove(id);
                        _webSocketUrls.Remove(id);
                        _requestTimestamps.Remove(id);
                    }
                }
            }
            
            if (staleIds.Count > 0)
            {
                Logger.LogInfo(LogCategory, $"CDP: Cleaned up {staleIds.Count} stale requests");
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: Cleanup error: {ex.Message}");
        }
    }

    private bool TryMarkAsProcessed(string requestId)
    {
        lock (_lockObject)
        {
            if (_processedRequests.Contains(requestId))
                return false;

            while (_processedQueue.Count >= ProcessedCacheSize)
            {
                var oldest = _processedQueue.Dequeue();
                _processedRequests.Remove(oldest);
            }

            _processedRequests.Add(requestId);
            _processedQueue.Enqueue(requestId);
            return true;
        }
    }

    public event EventHandler<TokenExtractedEventArgs>? TokenExtracted;
    public event EventHandler<TrafficStatsEventArgs>? TrafficUpdated;
    public bool IsMonitoring => _isMonitoring;

    public void StartMonitoring(CoreWebView2 webView)
    {
        if (_isMonitoring && ReferenceEquals(_webView, webView)) return;
        
        if (_isMonitoring && !ReferenceEquals(_webView, webView))
        {
            Logger.LogInfo(LogCategory, "NetworkMonitor: Different WebView instance detected, stopping current monitoring");
            StopMonitoring();
        }

        _webView = webView ?? throw new ArgumentNullException(nameof(webView));

        SetupNetworkMonitoring();
        StartCleanupTimer();
        StartTrafficCalcTimer();
        _isMonitoring = true;
        Logger.LogInfo(LogCategory, "NetworkMonitor: Monitoring started successfully");
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        if (_cleanupTimer != null)
        {
            try
            {
                _cleanupTimer.Stop();
                _cleanupTimer.Elapsed -= CleanupStaleRequests;
                _cleanupTimer.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LogCategory, $"NetworkMonitor: Timer cleanup error: {ex.Message}");
            }
            finally
            {
                _cleanupTimer = null;
            }
        }

        if (_trafficCalcTimer != null)
        {
            try
            {
                _trafficCalcTimer.Stop();
                _trafficCalcTimer.Elapsed -= CalculateTrafficSpeed;
                _trafficCalcTimer.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LogCategory, $"NetworkMonitor: Traffic timer cleanup error: {ex.Message}");
            }
            finally
            {
                _trafficCalcTimer = null;
            }
        }

        if (_webView != null)
        {
            try
            {
                if (_requestReceiver != null)
                    _requestReceiver.DevToolsProtocolEventReceived -= OnNetworkRequestWillBeSent;
                if (_responseReceiver != null)
                    _responseReceiver.DevToolsProtocolEventReceived -= OnNetworkResponseReceived;
                if (_loadingFinishedReceiver != null)
                    _loadingFinishedReceiver.DevToolsProtocolEventReceived -= OnNetworkLoadingFinished;
                if (_webSocketCreatedReceiver != null)
                    _webSocketCreatedReceiver.DevToolsProtocolEventReceived -= OnWebSocketCreated;
                if (_webSocketFrameReceivedReceiver != null)
                    _webSocketFrameReceivedReceiver.DevToolsProtocolEventReceived -= OnWebSocketFrameReceived;
                if (_dataReceivedReceiver != null)
                    _dataReceivedReceiver.DevToolsProtocolEventReceived -= OnDataReceived;
                if (_fetchRequestPausedReceiver != null)
                    _fetchRequestPausedReceiver.DevToolsProtocolEventReceived -= OnFetchRequestPaused;

                SafeCallCdpMethodAsync("Network.disable", "{}").SafeFireAndForget(ex => Logger.LogInfo(LogCategory, $"CDP: Network.disable failed: {ex.Message}"));
                SafeCallCdpMethodAsync("Fetch.disable", "{}").SafeFireAndForget(ex => Logger.LogInfo(LogCategory, $"CDP: Fetch.disable failed: {ex.Message}"));
                Logger.LogInfo(LogCategory, "CDP: Network.disable and Fetch.disable called");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetworkMonitor] Warning: WebView cleanup error: {ex.Message}");
            }
        }

        lock (_lockObject)
        {
            _pendingRequests.Clear();
            _requestUrls.Clear();
            _webSocketUrls.Clear();
            _requestTimestamps.Clear();
        }
        _isMonitoring = false;
        Logger.LogInfo(LogCategory, "NetworkMonitor: Monitoring stopped");
    }

    private async Task SafeCallCdpMethodAsync(string method, string parameters)
    {
        try
        {
            if (_webView != null)
            {
                await _webView.CallDevToolsProtocolMethodAsync(method, parameters);
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: {method} failed: {ex.Message}");
        }
    }

    private void SetupNetworkMonitoring()
    {
        if (_webView == null) return;
        SetupCdpNetworkMonitoring();
    }

    private void SetupCdpNetworkMonitoring()
    {
        if (_webView == null) return;

        try
        {
            _requestReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.requestWillBeSent");
            _requestReceiver.DevToolsProtocolEventReceived += OnNetworkRequestWillBeSent;
            Logger.LogInfo(LogCategory, "CDP: requestWillBeSent receiver registered");

            _responseReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.responseReceived");
            _responseReceiver.DevToolsProtocolEventReceived += OnNetworkResponseReceived;
            Logger.LogInfo(LogCategory, "CDP: responseReceived receiver registered");

            _loadingFinishedReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.loadingFinished");
            _loadingFinishedReceiver.DevToolsProtocolEventReceived += OnNetworkLoadingFinished;
            Logger.LogInfo(LogCategory, "CDP: loadingFinished receiver registered");

            _webSocketCreatedReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.webSocketCreated");
            _webSocketCreatedReceiver.DevToolsProtocolEventReceived += OnWebSocketCreated;
            Logger.LogInfo(LogCategory, "CDP: webSocketCreated receiver registered");

            _webSocketFrameReceivedReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.webSocketFrameReceived");
            _webSocketFrameReceivedReceiver.DevToolsProtocolEventReceived += OnWebSocketFrameReceived;
            Logger.LogInfo(LogCategory, "CDP: webSocketFrameReceived receiver registered");

            _dataReceivedReceiver = _webView.GetDevToolsProtocolEventReceiver("Network.dataReceived");
            _dataReceivedReceiver.DevToolsProtocolEventReceived += OnDataReceived;
            Logger.LogInfo(LogCategory, "CDP: dataReceived receiver registered");

            _webView.CallDevToolsProtocolMethodAsync("Network.enable", "{}").SafeFireAndForget(ex => Logger.LogInfo(LogCategory, $"CDP: Network.enable failed: {ex.Message}"));
            Logger.LogInfo(LogCategory, "CDP: Network.enable called");

            _fetchRequestPausedReceiver = _webView.GetDevToolsProtocolEventReceiver("Fetch.requestPaused");
            _fetchRequestPausedReceiver.DevToolsProtocolEventReceived += OnFetchRequestPaused;
            Logger.LogInfo(LogCategory, "CDP: Fetch.requestPaused receiver registered");

            _ = EnableFetchDomain();
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: Failed to setup network monitoring: {ex.Message}");
        }
    }

    private async Task EnableFetchDomain()
    {
        if (_webView == null) return;

        try
        {
            var patterns = new[]
            {
                new { urlPattern = "*doubao.com/chat/completion*", requestStage = "Response" },
                new { urlPattern = "*openai.com/v1/chat*", requestStage = "Response" },
                new { urlPattern = "*anthropic.com/v1/messages*", requestStage = "Response" },
                new { urlPattern = "*deepseek.com/chat*", requestStage = "Response" },
                new { urlPattern = "*moonshot.cn/v1/chat*", requestStage = "Response" },
                new { urlPattern = "*kimi.com/*", requestStage = "Response" },
                new { urlPattern = "*api.kimi.com/*", requestStage = "Response" }
            };

            var patternsJson = JsonSerializer.Serialize(new { patterns = patterns });
            
            await _webView.CallDevToolsProtocolMethodAsync("Fetch.enable", patternsJson);
            Logger.LogInfo(LogCategory, $"CDP: Fetch.enable called with {patterns.Length} patterns");
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: Fetch.enable failed: {ex.Message}");
        }
    }

    private void OnNetworkRequestWillBeSent(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson))
            {
                Logger.LogInfo(LogCategory, "CDP: requestWillBeSent: empty JSON");
                return;
            }

            if (TryExtractLongValue(e.ParameterObjectAsJson, "postDataLength", out var postDataLength))
            {
                Interlocked.Add(ref _currentUploadBytes, postDataLength);
            }
            var headersIndex = e.ParameterObjectAsJson.IndexOf("\"headers\":", StringComparison.Ordinal);
            if (headersIndex > 0)
            {
                var headersStart = headersIndex + 10;
                var braceCount = 0;
                var headersEnd = headersStart;
                while (headersEnd < e.ParameterObjectAsJson.Length)
                {
                    if (e.ParameterObjectAsJson[headersEnd] == '{') braceCount++;
                    if (e.ParameterObjectAsJson[headersEnd] == '}') braceCount--;
                    if (braceCount == 0 && e.ParameterObjectAsJson[headersEnd] == '}') break;
                    headersEnd++;
                }
                if (headersEnd > headersStart)
                {
                    var headersSize = (headersEnd - headersStart) / 2;
                    Interlocked.Add(ref _currentUploadBytes, headersSize);
                }
            }

            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("request", out var request) &&
                request.TryGetProperty("url", out var urlElem))
            {
                var url = urlElem.GetString();
                var isLlm = !string.IsNullOrEmpty(url) && TokenExtractor.IsLlmApiRequest(url);
                Logger.LogInfo(LogCategory, $"CDP: requestWillBeSent: {url?.Substring(0, Math.Min(100, url?.Length ?? 0))}... (isLlm={isLlm})");

                if (!string.IsNullOrEmpty(url) && isLlm)
                {
                    var requestId = root.TryGetProperty("requestId", out var idElem)
                        ? idElem.GetString()
                        : Guid.NewGuid().ToString();

                    if (requestId != null)
                    {
                        if (request.TryGetProperty("headers", out var headers))
                        {
                            var authInfo = TokenExtractor.ExtractAuthFromHeaders(headers, url);
                            if (authInfo != null)
                            {
                                Logger.LogInfo(LogCategory, "CDP: Auth information extracted");
                                TokenExtracted?.Invoke(this, authInfo);
                            }
                        }

                        lock (_lockObject)
                        {
                            _pendingRequests.Add(requestId);
                            _requestTimestamps[requestId] = DateTime.Now;
                        }
                        Logger.LogInfo(LogCategory, $"CDP: Tracking LLM request: {requestId}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: requestWillBeSent error: {ex.Message}");
        }
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

            bool isPending;
            lock (_lockObject)
            {
                isPending = _pendingRequests.Contains(requestId);
            }

            if (!isPending) return;

            if (root.TryGetProperty("response", out var response) &&
                response.TryGetProperty("url", out var urlElem))
            {
                var url = urlElem.GetString();
                if (!string.IsNullOrEmpty(url) && TokenExtractor.IsLlmApiRequest(url))
                {
                    Logger.LogInfo(LogCategory, $"CDP: Response received for LLM request: {requestId}, URL saved for loadingFinished");
                    lock (_lockObject)
                    {
                        _requestUrls[requestId] = url;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: responseReceived error: {ex.Message}");
        }
    }

    private void OnNetworkLoadingFinished(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;

            if (TryExtractLongValue(e.ParameterObjectAsJson, "encodedDataLength", out var encodedDataLength))
            {
                Interlocked.Add(ref _currentDownloadBytes, encodedDataLength);
            }

            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("requestId", out var idElem))
            {
                var requestId = idElem.GetString();
                if (!string.IsNullOrEmpty(requestId))
                {
                    string? url = null;
                    bool isPending;
                    lock (_lockObject)
                    {
                        isPending = _pendingRequests.Contains(requestId);
                        if (isPending)
                        {
                            _requestUrls.TryGetValue(requestId, out url);
                        }
                    }

                    if (isPending && !string.IsNullOrEmpty(url))
                    {
                        Logger.LogInfo(LogCategory, $"CDP: Loading finished for LLM request: {requestId}, fetching response body");
                        FetchAndProcessResponseBodyAsync(requestId, url).SafeFireAndForget(ex => Logger.LogInfo(LogCategory, $"CDP: FetchAndProcessResponseBody failed: {ex.Message}"));
                    }

                    lock (_lockObject)
                    {
                        _pendingRequests.Remove(requestId);
                        _requestUrls.Remove(requestId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: loadingFinished error: {ex.Message}");
        }
    }

    private async Task FetchAndProcessResponseBodyAsync(string requestId, string url)
    {
        if (_webView == null) return;

        if (!TryMarkAsProcessed(requestId))
        {
            Logger.LogInfo(LogCategory, $"CDP: Request already processed, skipping: {requestId}");
            return;
        }

        try
        {
            Logger.LogInfo(LogCategory, $"CDP: Fetching response body for: {requestId}");

            var result = await _webView.CallDevToolsProtocolMethodAsync(
                "Network.getResponseBody",
                JsonSerializer.Serialize(new { requestId })
            );

            if (string.IsNullOrEmpty(result))
            {
                Logger.LogInfo(LogCategory, $"CDP: Empty response body for: {requestId}");
                return;
            }

            Logger.LogInfo(LogCategory, $"CDP: Response body length: {result.Length}");

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            string? body = null;
            if (root.TryGetProperty("body", out var bodyElem))
            {
                body = bodyElem.GetString();
            }

            if (!string.IsNullOrEmpty(body))
            {
                if (root.TryGetProperty("base64Encoded", out var base64Elem) && 
                    base64Elem.GetBoolean())
                {
                    var bytes = Convert.FromBase64String(body);
                    body = Encoding.UTF8.GetString(bytes);
                }

                Logger.LogInfo(LogCategory, $"CDP: Response body processed, length: {body.Length}");

                var tokenArgs = TokenExtractor.ExtractTokensFromResponse(body, url);
                if (tokenArgs != null)
                {
                    Logger.LogInfo(LogCategory, $"Token: SUCCESS! Extracted {tokenArgs.Tokens} tokens from {tokenArgs.Provider}");
                    TokenExtracted?.Invoke(this, tokenArgs);
                }
                else
                {
                    Logger.LogInfo(LogCategory, $"Token: No tokens found in response");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: Failed to fetch response body: {ex.Message}");
        }
    }

    private void OnWebSocketCreated(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;

            Logger.LogInfo(LogCategory, $"WebSocket: Event received (length: {e.ParameterObjectAsJson.Length})");

            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("requestId", out var idElem) &&
                root.TryGetProperty("url", out var urlElem))
            {
                var requestId = idElem.GetString();
                var url = urlElem.GetString();

                Logger.LogInfo(LogCategory, $"WebSocket: URL: {url}, isLlm: {TokenExtractor.IsLlmApiRequest(url ?? "")}");

                if (!string.IsNullOrEmpty(url) && TokenExtractor.IsLlmApiRequest(url))
                {
                    Logger.LogInfo(LogCategory, $"WebSocket: LLM WebSocket created: {url}");
                    if (requestId != null)
                    {
                        lock (_lockObject)
                        {
                            _webSocketUrls[requestId] = url;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"WebSocket: Created error: {ex.Message}");
        }
    }

    private void OnWebSocketFrameReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;

            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("requestId", out var idElem))
            {
                var requestId = idElem.GetString();
                if (string.IsNullOrEmpty(requestId)) return;

                string? url;
                lock (_lockObject)
                {
                    _webSocketUrls.TryGetValue(requestId, out url);
                }

                if (string.IsNullOrEmpty(url)) return;

                if (root.TryGetProperty("response", out var response) &&
                    response.TryGetProperty("payloadData", out var payloadElem))
                {
                    var payload = payloadElem.GetString();
                    if (!string.IsNullOrEmpty(payload))
                    {
                        Logger.LogInfo(LogCategory, $"WebSocket: Frame received from LLM, length: {payload?.Length ?? 0}");
                        ProcessStreamingData(payload, url);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"WebSocket: Frame received error: {ex.Message}");
        }
    }

    private void OnDataReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;

            if (TryExtractLongValue(e.ParameterObjectAsJson, "encodedDataLength", out var encodedLen))
            {
                Interlocked.Add(ref _currentDownloadBytes, encodedLen);
            }
            else if (TryExtractLongValue(e.ParameterObjectAsJson, "dataLength", out var dataLen))
            {
                Interlocked.Add(ref _currentDownloadBytes, dataLen);
            }

            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("requestId", out var idElem))
            {
                var requestId = idElem.GetString();
                if (string.IsNullOrEmpty(requestId)) return;

                bool isPending;
                lock (_lockObject)
                {
                    isPending = _pendingRequests.Contains(requestId);
                }

                if (!isPending) return;

                Logger.LogInfo(LogCategory, $"CDP: Data received for streaming request: {requestId}");

                FetchStreamingDataAsync(requestId).SafeFireAndForget(ex => Logger.LogInfo(LogCategory, $"CDP: FetchStreamingData failed: {ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: Data received error: {ex.Message}");
        }
    }

    private async Task FetchStreamingDataAsync(string requestId)
    {
        if (_webView == null) return;

        try
        {
            var result = await _webView.CallDevToolsProtocolMethodAsync(
                "Network.getResponseBody",
                JsonSerializer.Serialize(new { requestId })
            );

            if (string.IsNullOrEmpty(result))
            {
                Logger.LogInfo(LogCategory, $"CDP: Streaming data empty, will retry in LoadingFinished: {requestId}");
                return;
            }

            if (!TryMarkAsProcessed(requestId))
            {
                Logger.LogInfo(LogCategory, $"CDP: Streaming request already processed, skipping: {requestId}");
                return;
            }

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            string? body = null;
            if (root.TryGetProperty("body", out var bodyElem))
            {
                body = bodyElem.GetString();
            }

            if (!string.IsNullOrEmpty(body))
            {
                if (root.TryGetProperty("base64Encoded", out var base64Elem) &&
                    base64Elem.GetBoolean())
                {
                    var bytes = Convert.FromBase64String(body);
                    body = Encoding.UTF8.GetString(bytes);
                }

                string? url = null;
                lock (_lockObject)
                {
                    _requestUrls.TryGetValue(requestId, out url);
                }

                ProcessStreamingData(body, url ?? "unknown", requestId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CDP: Fetch streaming data error: {ex.Message}, will retry in LoadingFinished");
        }
    }

    private void ProcessStreamingData(string data, string url, string? requestId = null)
    {
        if (string.IsNullOrEmpty(data)) return;

        if (!string.IsNullOrEmpty(requestId) && !TryMarkAsProcessed(requestId))
        {
            Logger.LogInfo(LogCategory, $"Token: Request already processed, skipping: {requestId}");
            return;
        }

        var tokenArgs = TokenExtractor.ExtractTokensFromResponse(data, url);
        if (tokenArgs != null)
        {
            Logger.LogInfo(LogCategory, $"Token: SUCCESS from streaming! Extracted {tokenArgs.Tokens} tokens from {tokenArgs.Provider}");
            TokenExtracted?.Invoke(this, tokenArgs);
            return;
        }

        var streamingTokenArgs = TokenExtractor.ExtractTokensFromStreamingChunk(data, url);
        if (streamingTokenArgs != null)
        {
            Logger.LogInfo(LogCategory, $"Token: SUCCESS from chunk! Extracted {streamingTokenArgs.Tokens} tokens from {streamingTokenArgs.Provider}");
            TokenExtracted?.Invoke(this, streamingTokenArgs);
        }
    }

    private void OnFetchRequestPaused(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        if (_webView == null) return;

        try
        {
            if (string.IsNullOrEmpty(e.ParameterObjectAsJson)) return;

            using var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = doc.RootElement;

            var requestId = root.TryGetProperty("requestId", out var idElem) ? idElem.GetString() : null;
            var url = root.TryGetProperty("request", out var req) && req.TryGetProperty("url", out var urlElem) ? urlElem.GetString() : null;
            var resourceType = root.TryGetProperty("resourceType", out var rtElem) ? rtElem.GetString() : null;

            if (string.IsNullOrEmpty(requestId)) return;

            Logger.LogInfo(LogCategory, $"Fetch: Request paused: {url?.Substring(0, Math.Min(100, url?.Length ?? 0))}... type={resourceType}");

            if (!string.IsNullOrEmpty(url) && TokenExtractor.IsLlmApiRequest(url))
            {
                Logger.LogInfo(LogCategory, $"Fetch: LLM API request detected, saving URL for later: {url}");
                lock (_lockObject)
                {
                    _requestUrls[requestId] = url;
                    _pendingRequests.Add(requestId);
                }
            }

            ContinueFetchRequestAsync(requestId).SafeFireAndForget(ex =>
            {
                Logger.LogInfo(LogCategory, $"Fetch: Failed to continue request {requestId}: {ex.Message}");
                FailFetchRequestAsync(requestId, "Failed to continue request").SafeFireAndForget(
                    failEx => Logger.LogInfo(LogCategory, $"Fetch: Also failed to fail request {requestId}: {failEx.Message}")
                );
            });
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"Fetch: Request paused error: {ex.Message}");
        }
    }

    private async Task ContinueFetchRequestAsync(string requestId)
    {
        if (_webView == null || string.IsNullOrEmpty(requestId)) return;

        const int maxRetries = 3;
        const int retryDelayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                Logger.LogInfo(LogCategory, $"Fetch: Continuing request {requestId} (attempt {attempt + 1}/{maxRetries})");
                await _webView.CallDevToolsProtocolMethodAsync(
                    "Fetch.continueRequest",
                    JsonSerializer.Serialize(new { requestId })
                );
                Logger.LogInfo(LogCategory, $"Fetch: Request {requestId} continued successfully");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LogCategory, $"Fetch: Continue request {requestId} failed (attempt {attempt + 1}): {ex.Message}");
                if (attempt == maxRetries - 1)
                {
                    throw;
                }
                await Task.Delay(retryDelayMs);
            }
        }
    }

    private async Task FailFetchRequestAsync(string requestId, string errorReason)
    {
        if (_webView == null || string.IsNullOrEmpty(requestId)) return;

        try
        {
            Logger.LogInfo(LogCategory, $"Fetch: Failing request {requestId} with reason: {errorReason}");
            await _webView.CallDevToolsProtocolMethodAsync(
                "Fetch.failRequest",
                JsonSerializer.Serialize(new 
                { 
                    requestId, 
                    errorReason = errorReason,
                    networkErrorReason = "Failed"
                })
            );
            Logger.LogInfo(LogCategory, $"Fetch: Request {requestId} failed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"Fetch: Fail request {requestId} failed: {ex.Message}");
            throw;
        }
    }

    private async Task InterceptAndContinueAsync(string requestId, string url)
    {
        if (_webView == null) return;

        try
        {
            var result = await _webView.CallDevToolsProtocolMethodAsync(
                "Fetch.getResponseBody",
                JsonSerializer.Serialize(new { requestId })
            );

            if (!string.IsNullOrEmpty(result))
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                string? body = null;
                if (root.TryGetProperty("body", out var bodyElem))
                {
                    body = bodyElem.GetString();
                }

                if (!string.IsNullOrEmpty(body))
                {
                    if (root.TryGetProperty("base64Encoded", out var base64Elem) && base64Elem.GetBoolean())
                    {
                        var bytes = Convert.FromBase64String(body);
                        body = Encoding.UTF8.GetString(bytes);
                    }

                    Logger.LogInfo(LogCategory, $"Fetch: Response body length: {body.Length}");
                    Logger.LogInfo(LogCategory, $"Fetch: Response body length: {body?.Length ?? 0}");

                    ProcessStreamingData(body, url, requestId);
                }
            }

            await _webView.CallDevToolsProtocolMethodAsync(
                "Fetch.continueRequest",
                JsonSerializer.Serialize(new { requestId })
            );
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"Fetch: Intercept error: {ex.Message}");
            try
            {
                await _webView.CallDevToolsProtocolMethodAsync(
                    "Fetch.continueRequest",
                    JsonSerializer.Serialize(new { requestId })
                );
            }
            catch { }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopMonitoring();

        try
        {
            _cleanupTimer?.Stop();
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;
        }
        catch { }

        try
        {
            _trafficCalcTimer?.Stop();
            _trafficCalcTimer?.Dispose();
            _trafficCalcTimer = null;
        }
        catch { }

        _disposed = true;
    }
}