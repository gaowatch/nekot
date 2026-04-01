using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace NekoT.Desktop.Services.WebView2;

public class WebView2InitializationService : IWebView2InitializationService
{
    private CoreWebView2Environment? _environment;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private WebView2InitializationState _state = WebView2InitializationState.NotCreated;
    private string? _initializationError;

    public WebView2InitializationState State { get => _state; private set { if (_state != value) { _state = value; StateChanged?.Invoke(this, value); } } }
    public bool IsInitialized => State == WebView2InitializationState.Ready || State == WebView2InitializationState.Failed;
    public bool IsInitializationSuccessful => State == WebView2InitializationState.Ready;
    public string? InitializationError => _initializationError;
    public event EventHandler<WebView2InitializationState>? StateChanged;

    public async Task<CoreWebView2Environment?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_environment != null) return _environment;
        bool lockAcquired = false;
        try
        {
            lockAcquired = await _initLock.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            if (!lockAcquired) return null;
            if (_environment != null) return _environment;
            State = WebView2InitializationState.Creating;
            var envOptions = new CoreWebView2EnvironmentOptions();
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NekoT", $"WebView2Data_{Environment.ProcessId}");
            if (!Directory.Exists(userDataFolder)) Directory.CreateDirectory(userDataFolder);
            _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, envOptions);
            State = WebView2InitializationState.Ready;
            return _environment;
        }
        catch (Exception ex)
        {
            State = WebView2InitializationState.Failed;
            _initializationError = ex.Message;
            return null;
        }
        finally { if (lockAcquired) _initLock.Release(); }
    }

    public CoreWebView2Environment? GetEnvironment() => _environment;
    public void Reset() { _environment = null; State = WebView2InitializationState.NotCreated; _initializationError = null; }
}
