using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace NekoT.Desktop.Services.WebView2;

public interface IWebView2InitializationService
{
    WebView2InitializationState State { get; }
    bool IsInitializationSuccessful { get; }
    string? InitializationError { get; }
    CoreWebView2Environment? GetEnvironment();
    Task<CoreWebView2Environment?> InitializeAsync(CancellationToken cancellationToken = default);
    void Reset();
    event EventHandler<WebView2InitializationState>? StateChanged;
}

public class WebView2InitializationService : IWebView2InitializationService
{
    private static CoreWebView2Environment? _sharedEnvironment;
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private static int _initCount;
    private WebView2InitializationState _state = WebView2InitializationState.NotInitialized;
    private string? _error;
    private CoreWebView2Environment? _environment;
    public event EventHandler<WebView2InitializationState>? StateChanged;

    public WebView2InitializationState State => _state;
    public bool IsInitializationSuccessful => _state == WebView2InitializationState.Succeeded;
    public string? InitializationError => _error;

    public async Task<CoreWebView2Environment?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_state == WebView2InitializationState.Succeeded && _environment != null) return _environment;
        if (_state == WebView2InitializationState.InProgress) { await WaitForInitAsync(cancellationToken); return _environment; }
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_state == WebView2InitializationState.Succeeded && _environment != null) return _environment;
            SetState(WebView2InitializationState.InProgress);
            _environment = await CoreWebView2Environment.CreateAsync();
            _sharedEnvironment = _environment;
            Interlocked.Increment(ref _initCount);
            SetState(WebView2InitializationState.Succeeded);
            return _environment;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            SetState(WebView2InitializationState.Failed);
            return null;
        }
        finally { _initLock.Release(); }
    }

    private async Task WaitForInitAsync(CancellationToken cancellationToken)
    {
        var start = DateTime.Now;
        while (_state == WebView2InitializationState.InProgress && !cancellationToken.IsCancellationRequested)
        {
            if (DateTime.Now - start > TimeSpan.FromSeconds(30)) break;
            await Task.Delay(100, cancellationToken);
        }
    }

    public CoreWebView2Environment? GetEnvironment() => _environment ?? _sharedEnvironment;
    public void Reset() { _state = WebView2InitializationState.NotInitialized; _environment = null; _error = null; }
    private void SetState(WebView2InitializationState state) { if (_state != state) { _state = state; StateChanged?.Invoke(this, state); } }
}

public enum WebView2InitializationState { NotInitialized, InProgress, Succeeded, Failed, Cancelled }