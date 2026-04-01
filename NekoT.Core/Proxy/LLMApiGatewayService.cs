using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NekoT.Core.Configuration;
using NekoT.Core.Forwarding;
using NekoT.Core.LlmProviders;
using NekoT.Core.Statistics;
using NekoT.Core.Utils;

namespace NekoT.Core.Proxy;

public class LLMApiGatewayService : IAsyncDisposable, IDisposable
{
    public const int DefaultGatewayPort = 8787;
    public const int DefaultStatsPort = 8788;
    public const string DefaultVendor = "openai";
    
    private readonly int _gatewayPort;
    private readonly int _statsPort;
    private readonly string _defaultVendor;
    private readonly WhitelistValidator _whitelistValidator;
    private readonly object _lock = new object();
    
    private static readonly Regex SensitiveParamRegex = new(
        @"[?&](api[_-]?key|token|secret|authorization|password|credential)=[^&]*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public string GatewayBaseUrl => $"http://127.0.0.1:{_gatewayPort}";
    public string StatsUrl => $"http://127.0.0.1:{_statsPort}/stats";
    public bool IsRunning 
    { 
        get 
        { 
            lock (_lock) 
            { 
                return _httpListener?.IsListening ?? false; 
            } 
        } 
    }
    
    private readonly Dictionary<string, string> _llmVendorMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "openai", "https://api.openai.com" },
        { "deepseek", "https://api.deepseek.com" },
        { "doubao", "https://api.doubao.com" },
        { "qwen", "https://dashscope.aliyuncs.com" },
        { "zhipu", "https://open.bigmodel.cn" },
        { "moonshot", "https://api.moonshot.cn" },
        { "anthropic", "https://api.anthropic.com" },
        { "minimaxi", "https://api.minimaxi.com" },
        { "minimax", "https://api.minimaxi.com" },
        { "kimi", "https://api.kimi.com" },
        { "siliconflow", "https://api.siliconflow.cn" }
    };

    private readonly Dictionary<string, string> _pathMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "minimaxi:v1/chat/completions", "v1/text/chatcompletion_v2" },
        { "minimax:v1/chat/completions", "v1/text/chatcompletion_v2" },
        { "kimi:v1/chat/completions", "v1/chat/completions" }
    };

    private HttpListener? _httpListener;
    private HttpListener? _statsListener;
    private HttpClient? _httpClient;
    private CancellationTokenSource? _cts;
    private Task? _requestLoopTask;
    private Task? _statsLoopTask;
    private readonly HashSet<Task> _pendingRequests = new();
    private readonly object _pendingRequestsLock = new();
    private volatile bool _disposed;

    public int ProxyPort => _gatewayPort;
    public string ProxyAddress => GatewayBaseUrl;

    public LLMApiGatewayService(int? gatewayPort = null, int? statsPort = null, string? defaultVendor = null)
    {
        _gatewayPort = gatewayPort ?? AppConstants.Forwarding.GatewayPort;
        _statsPort = statsPort ?? AppConstants.Forwarding.StatsPort;
        _defaultVendor = defaultVendor ?? DefaultVendor;
        _whitelistValidator = new WhitelistValidator();
    }

    public async Task StartProxyAsync()
    {
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LLMApiGatewayService));
            if (IsRunning) 
                return;
        }

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://127.0.0.1:{_gatewayPort}/");
        
        _statsListener = new HttpListener();
        _statsListener.Prefixes.Add($"http://127.0.0.1:{_statsPort}/");
        
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });
        _httpClient.Timeout = TimeSpan.FromMinutes(2);

        _httpListener.Start();
        _statsListener.Start();
        _cts = new CancellationTokenSource();
        
        _requestLoopTask = Task.Run(async () => await RunRequestLoopAsync(_cts.Token));
        _statsLoopTask = Task.Run(async () => await RunStatsLoopAsync(_cts.Token));

        Log($"[Gateway] HTTP Gateway started: {GatewayBaseUrl}");
        Console.WriteLine("[Gateway] Usage: http://127.0.0.1:{0}/{vendor}/v1/chat/completions", _gatewayPort);
        Console.WriteLine("[Gateway] Supported vendors: " + string.Join(", ", _llmVendorMapping.Keys));
        Log($"[Gateway] Stats: {StatsUrl}");
    }

    public async Task StopProxyAsync()
    {
        CancellationTokenSource? localCts = null;
        Task? localRequestLoopTask = null;
        Task? localStatsLoopTask = null;
        HttpListener? localHttpListener = null;
        HttpListener? localStatsListener = null;
        HttpClient? localHttpClient = null;
        Task[]? pendingRequestsSnapshot = null;

        lock (_lock)
        {
            if (!IsRunning) 
                return;

            localCts = _cts;
            localRequestLoopTask = _requestLoopTask;
            localStatsLoopTask = _statsLoopTask;
            localHttpListener = _httpListener;
            localStatsListener = _statsListener;
            localHttpClient = _httpClient;

            _cts = null;
            _requestLoopTask = null;
            _statsLoopTask = null;
            _httpListener = null;
            _statsListener = null;
            _httpClient = null;
        }

        try
        {
            localCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            if (localHttpListener != null)
            {
                localHttpListener.Stop();
                localHttpListener.Close();
            }
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Error stopping http listener: {ex}");
        }

        try
        {
            if (localStatsListener != null)
            {
                localStatsListener.Stop();
                localStatsListener.Close();
            }
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Error stopping stats listener: {ex}");
        }

        lock (_pendingRequestsLock)
        {
            pendingRequestsSnapshot = _pendingRequests.ToArray();
        }

        try
        {
            var tasks = new List<Task>();
            if (localRequestLoopTask != null) 
                tasks.Add(localRequestLoopTask);
            if (localStatsLoopTask != null) 
                tasks.Add(localStatsLoopTask);
            if (pendingRequestsSnapshot != null && pendingRequestsSnapshot.Length > 0)
                tasks.AddRange(pendingRequestsSnapshot);
            
            if (tasks.Count > 0)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    await Task.WhenAll(tasks).WaitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log($"[Gateway] Timeout waiting for tasks to complete, forcing cleanup");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Error waiting for loops: {ex}");
        }

        lock (_pendingRequestsLock)
        {
            _pendingRequests.Clear();
        }

        try
        {
            localHttpClient?.Dispose();
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Error disposing http client: {ex}");
        }

        try
        {
            localCts?.Dispose();
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Error disposing cts: {ex}");
        }
    }

    private async Task RunRequestLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                var tcs = new TaskCompletionSource<HttpListenerContext>();
                using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
                
                var getContextTask = _httpListener?.GetContextAsync() ?? Task.FromException<HttpListenerContext>(new ObjectDisposedException(nameof(_httpListener)));
                var completedTask = await Task.WhenAny(getContextTask, tcs.Task);
                
                if (completedTask == tcs.Task)
                {
                    break;
                }
                
                context = await getContextTask;
                
                var requestTask = ProcessRequestWithTrackingAsync(context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                throw;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                    Log($"[Gateway] Request loop error: {ex.Message}");
            }
        }
    }

    private async Task ProcessRequestWithTrackingAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var task = ProcessRequestAsync(context, cancellationToken);
        
        lock (_pendingRequestsLock)
        {
            _pendingRequests.Add(task);
        }
        
        try
        {
            await task;
        }
        finally
        {
            lock (_pendingRequestsLock)
            {
                _pendingRequests.Remove(task);
            }
        }
    }

    private async Task RunStatsLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var tcs = new TaskCompletionSource<HttpListenerContext>();
                using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
                
                var getContextTask = _statsListener?.GetContextAsync() ?? Task.FromException<HttpListenerContext>(new ObjectDisposedException(nameof(_statsListener)));
                var completedTask = await Task.WhenAny(getContextTask, tcs.Task);
                
                if (completedTask == tcs.Task)
                {
                    break;
                }
                
                var context = await getContextTask;
                await HandleStatsRequestAsync(context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                throw;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                    Log($"[Gateway] Stats loop error: {ex.Message}");
            }
        }
    }

    private async Task HandleStatsRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;
        
        try
        {
            if (context.Request.Url?.ToString().Contains("/stats", StringComparison.OrdinalIgnoreCase) == true)
            {
                var stats = ProxyStatistics.GetStats();
                var json = JsonSerializer.Serialize(stats);
                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Stats handler error: {ex}");
            try
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            catch
            {
            }
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch (Exception ex)
            {
                Log($"[Gateway] Error closing stats response: {ex.Message}");
            }
        }
    }

    private void Log(string msg)
    {
        var safeMsg = FilterSensitiveData(msg);
        Console.WriteLine(safeMsg);
        try 
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gateway.log");
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss} {safeMsg}\n", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gateway] Log write failed: {ex.Message}");
        }
    }

    private static string FilterSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return Regex.Replace(input, @"([?&])(api[_-]?key|token|secret|authorization)=[^&]+", "$1$2=***", RegexOptions.IgnoreCase);
    }

    private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url == null)
            {
                await SendErrorResponseAsync(response, HttpStatusCode.BadRequest, "Invalid request URL");
                return;
            }

            var pathSegments = request.Url.AbsolutePath.Trim('/').Split('/', 2);
            
            string vendorName;
            string targetPath;
            
            if (pathSegments.Length < 2)
            {
                vendorName = _defaultVendor;
                targetPath = pathSegments[0];
                
                if (!_llmVendorMapping.ContainsKey(_defaultVendor))
                {
                    await SendErrorResponseAsync(response, HttpStatusCode.Forbidden, 
                        $"Default vendor '{_defaultVendor}' not configured");
                    return;
                }
            }
            else
            {
                vendorName = pathSegments[0].ToLowerInvariant();
                targetPath = pathSegments[1];
            }

            if (!_llmVendorMapping.TryGetValue(vendorName, out var targetBaseUrl))
            {
                await SendErrorResponseAsync(response, HttpStatusCode.Forbidden, $"Unknown vendor: {vendorName}");
                return;
            }

            var mappingKey = $"{vendorName}:{targetPath}";
            if (_pathMapping.TryGetValue(mappingKey, out var mappedPath))
            {
                targetPath = mappedPath;
            }

            var filteredQuery = SensitiveParamRegex.Replace(request.Url.Query, "");
            var targetUrl = $"{targetBaseUrl}/{targetPath}{filteredQuery}";
            
            if (!ValidateTargetUrl(targetUrl, targetBaseUrl))
            {
                await SendErrorResponseAsync(response, HttpStatusCode.Forbidden, "Invalid target URL");
                return;
            }

            Log($"[Gateway] {vendorName} -> {targetUrl}");

            string requestBody = string.Empty;
            if (request.HasEntityBody)
            {
                var encoding = request.ContentEncoding ?? Encoding.UTF8;
                using var reader = new StreamReader(request.InputStream, encoding);
                requestBody = await reader.ReadToEndAsync();
            }

            HttpClient? localHttpClient;
            bool isServiceAvailable;
            lock (_lock)
            {
                localHttpClient = _httpClient;
                isServiceAvailable = localHttpClient != null && !_disposed;
            }

            if (!isServiceAvailable)
            {
                await SendErrorResponseAsync(response, HttpStatusCode.ServiceUnavailable, "Service unavailable");
                return;
            }

            using var forwardRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), targetUrl);
            
            foreach (string headerName in request.Headers)
            {
                if (WebHeaderCollection.IsRestricted(headerName)) continue;
                forwardRequest.Headers.TryAddWithoutValidation(headerName, request.Headers[headerName]);
            }

            if (!string.IsNullOrEmpty(requestBody) && request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var contentType = request.ContentType ?? "application/json";
                forwardRequest.Content = new StringContent(requestBody, Encoding.UTF8, contentType);
            }

            if (localHttpClient == null)
            {
                await SendErrorResponseAsync(response, HttpStatusCode.ServiceUnavailable, "Service unavailable");
                return;
            }
            using var forwardResponse = await localHttpClient.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            await ForwardResponseAsync(response, forwardResponse, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Request error: {ex}");
            try
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            catch
            {
            }
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch (Exception ex)
            {
                Log($"[Gateway] Error closing response: {ex.Message}");
            }
        }
    }

    private static bool ValidateTargetUrl(string targetUrl, string expectedBaseUrl)
    {
        try
        {
            var uri = new Uri(targetUrl);
            var expectedBaseUri = new Uri(expectedBaseUrl);
            
            if (!uri.Host.Equals(expectedBaseUri.Host, StringComparison.OrdinalIgnoreCase))
                return false;
            
            if (uri.AbsolutePath.Contains("../", StringComparison.Ordinal) || uri.AbsolutePath.Contains("./", StringComparison.Ordinal))
                return false;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ForwardResponseAsync(HttpListenerResponse response, HttpResponseMessage forwardResponse, CancellationToken cancellationToken)
    {
        response.StatusCode = (int)forwardResponse.StatusCode;

        foreach (var header in forwardResponse.Headers)
        {
            foreach (var value in header.Value)
            {
                try 
                { 
                    response.Headers.Add(header.Key, value); 
                }
                catch (Exception ex)
                {
                    Log($"[Gateway] Failed to add header {header.Key}: {ex.Message}");
                }
            }
        }
        
        if (forwardResponse.Content != null)
        {
            foreach (var header in forwardResponse.Content.Headers)
            {
                foreach (var value in header.Value)
                {
                    try 
                    { 
                        response.Headers.Add(header.Key, value); 
                    }
                    catch (Exception ex)
                    {
                        Log($"[Gateway] Failed to add content header {header.Key}: {ex.Message}");
                    }
                }
            }
        }

        var isStream = forwardResponse.Content?.Headers?.ContentType?.MediaType?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isStream && forwardResponse.Content != null)
        {
            await ForwardStreamResponseAsync(response, forwardResponse, cancellationToken);
        }
        else
        {
            await ForwardNonStreamResponseAsync(response, forwardResponse, cancellationToken);
        }
    }

    private async Task ForwardStreamResponseAsync(HttpListenerResponse response, HttpResponseMessage forwardResponse, CancellationToken cancellationToken)
    {
        using var responseStream = await forwardResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);
        using var writer = new StreamWriter(response.OutputStream) { AutoFlush = true };

        var usageTasks = new List<Task>();

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();

            if (!string.IsNullOrEmpty(line) && line.StartsWith("data: ", StringComparison.Ordinal) && !line.Equals("data: [DONE]", StringComparison.Ordinal))
                {
                    var jsonData = line.Substring("data: ".Length).Trim();
                    usageTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            ExtractAndRecordUsage(jsonData);
                        }
                        catch (Exception ex)
                        {
                            Log($"[Gateway] Unhandled exception in ExtractAndRecordUsage: {ex.Message}");
                        }
                    }, cancellationToken));
                }
        }

        if (usageTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(usageTasks);
            }
            catch (Exception ex)
            {
                Log($"[Gateway] Error recording usage: {ex.Message}");
            }
        }
    }

    private async Task ForwardNonStreamResponseAsync(HttpListenerResponse response, HttpResponseMessage forwardResponse, CancellationToken cancellationToken)
    {
        var responseBody = await forwardResponse.Content.ReadAsStringAsync(cancellationToken);
        
        if (!string.IsNullOrEmpty(responseBody))
        {
            ExtractAndRecordUsage(responseBody);
        }

        var responseBytes = Encoding.UTF8.GetBytes(responseBody);
        response.ContentLength64 = responseBytes.Length;
        await response.OutputStream.WriteAsync(responseBytes.AsMemory(0, responseBytes.Length), cancellationToken);
    }

    private static void ExtractAndRecordUsage(string jsonResponse)
    {
        try
        {
            var (input, output, total) = UsageExtractor.ExtractFromResponse(jsonResponse);
            if (total > 0)
            {
                ProxyStatistics.RecordRequest(input, output);
                Console.WriteLine($"[Gateway] Recorded usage: input={input}, output={output}, total={total}");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Gateway] Failed to parse usage from response: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gateway] Unexpected error recording usage: {ex.Message}");
        }
    }

    private static async Task SendErrorResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, string message)
    {
        response.StatusCode = (int)statusCode;
        var msgBytes = Encoding.UTF8.GetBytes(message);
        response.ContentLength64 = msgBytes.Length;
        await response.OutputStream.WriteAsync(msgBytes);
        response.Close();
    }

    public void Dispose()
    {
        try
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log($"[Gateway] Error in Dispose: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        await StopProxyAsync();
        GC.SuppressFinalize(this);
    }

    ~LLMApiGatewayService()
    {
        if (!_disposed)
        {
            Log($"[Gateway] Finalizer called - object not properly disposed");
        }
    }
}