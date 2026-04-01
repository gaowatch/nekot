using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using NekoT.Desktop.Services;

namespace NekoT.Desktop.Services.WebView2;

public class WebView2InitializationService : IWebView2InitializationService
{
    private CoreWebView2Environment? _environment;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private WebView2InitializationState _state = WebView2InitializationState.NotCreated;
    private string? _initializationError;
    private int _initializationCount;
    private int _retryCount;
    
    private bool _simulateFailure;
    private int _failCount;
    private int _currentFailCount;
    
    public const int MaxRetries = 3;

    public WebView2InitializationState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                StateChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsInitialized => State == WebView2InitializationState.Ready || State == WebView2InitializationState.Failed;
    public bool IsInitializationSuccessful => State == WebView2InitializationState.Ready;
    public string? InitializationError => _initializationError;
    public int InitializationCount => _initializationCount;
    public int RetryCount => _retryCount;

    public event EventHandler<WebView2InitializationState>? StateChanged;

    public async Task<CoreWebView2Environment?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_environment != null)
        {
            return _environment;
        }

        if (State == WebView2InitializationState.Failed && _retryCount >= MaxRetries)
        {
            return null;
        }

        bool lockAcquired = false;
        try
        {
            lockAcquired = await _initLock.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            if (!lockAcquired)
            {
                return null;
            }

            if (_environment != null)
            {
                return _environment;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                State = WebView2InitializationState.Failed;
                _initializationError = "Operation cancelled";
                return null;
            }

            State = WebView2InitializationState.Creating;
            _initializationCount++;

            if (_simulateFailure && _currentFailCount < _failCount)
            {
                _currentFailCount++;
                throw new InvalidOperationException("Simulated failure");
            }

            var envOptions = new CoreWebView2EnvironmentOptions
            {
                AllowSingleSignOnUsingOSPrimaryAccount = false
            };

            var proxyUrl = UserSettingsService.Instance.ProxyUrl;
            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                envOptions.AdditionalBrowserArguments = $"--proxy-server={proxyUrl}";
            }

            var userDataFolder = GetWebView2UserDataFolder();

            _environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder,
                options: envOptions);

            State = WebView2InitializationState.Ready;
            _initializationError = null;
            _retryCount = 0;

            return _environment;
        }
        catch (OperationCanceledException)
        {
            State = WebView2InitializationState.Failed;
            _initializationError = "Operation cancelled";
            return null;
        }
        catch (Exception ex)
        {
            _retryCount++;
            State = WebView2InitializationState.Failed;
            _initializationError = ex.Message;
            return null;
        }
        finally
        {
            if (lockAcquired)
            {
                _initLock.Release();
            }
        }
    }

    public CoreWebView2Environment? GetEnvironment()
    {
        return _environment;
    }

    public void Reset()
    {
        _environment = null;
        State = WebView2InitializationState.NotCreated;
        _initializationError = null;
        _initializationCount = 0;
        _retryCount = 0;
        _currentFailCount = 0;
        _simulateFailure = false;
        _failCount = 0;
    }

    private static string GetWebView2UserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseFolder = Path.Combine(localAppData, "NekoT");
        var userDataFolder = Path.Combine(baseFolder, $"WebView2Data_{Environment.ProcessId}");

        if (!Directory.Exists(userDataFolder))
        {
            Directory.CreateDirectory(userDataFolder);
        }

        return userDataFolder;
    }

    internal void SetTestEnvironment(CoreWebView2Environment environment)
    {
        _environment = environment;
    }

    internal void SetTestState(WebView2InitializationState state)
    {
        _state = state;
    }

    internal void SetSimulateFailure(bool simulate, int failCount = int.MaxValue)
    {
        _simulateFailure = simulate;
        _failCount = failCount;
        _currentFailCount = 0;
    }
}