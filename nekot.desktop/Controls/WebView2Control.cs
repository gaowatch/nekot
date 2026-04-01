using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using NekoT.Core.Configuration;
using NekoT.Desktop.Services;
using NekoT.Desktop.Services.Logging;

namespace NekoT.Desktop.Controls;

public class WebView2Control : NativeControlHost
{
    private static readonly ILoggerService Logger = LoggerService.Instance;
    private const string LogCategory = "WebView2";
    
    private static CoreWebView2Environment? _sharedEnvironment;
    private static readonly SemaphoreSlim _environmentInitLock = new(1, 1);
    private static int _environmentInitCount;

    static WebView2Control()
    {
        CleanupOldDataFolders();
    }
    
    private static void CleanupOldDataFolders()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var baseFolder = Path.Combine(localAppData, "NekoT");
            
            if (!Directory.Exists(baseFolder))
                return;
            
            var folders = Directory.GetDirectories(baseFolder, "WebView2Data_*");
            var currentProcessId = Environment.ProcessId;
            
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                var pidStr = folderName.Replace("WebView2Data_", "");
                
                if (int.TryParse(pidStr, out var pid))
                {
                    if (pid != currentProcessId)
                    {
                        try
                        {
                            var process = System.Diagnostics.Process.GetProcessById(pid);
                        }
                        catch (ArgumentException)
                        {
                            try
                            {
                                Directory.Delete(folder, true);
                                System.Diagnostics.Debug.WriteLine($"[WebView2] Cleaned up orphan folder: {folder}");
                            }
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[WebView2] Failed to delete orphan folder {folder}: {deleteEx.Message}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory, "Cleanup failed", ex);
        }
    }
    
    public WebView2Control()
    {
        Logger.LogInfo(LogCategory, "Constructor called");
    }

    private CoreWebView2Controller? _controller;
    private CoreWebView2? _coreWebView2;
    private IntPtr _hwnd;
    private string? _pendingUrl;
    private bool _isReady;
    private string? _lastNavigatedUrl;
    private bool _isShowingErrorPage;
    private int _errorPageRetryCount;
    private const int MaxErrorPageRetries = 3;
    
    private enum WebViewState
    {
        NotInitialized,
        Initializing,
        Ready,
        Failed,
        Disposed
    }
    
    private WebViewState _state = WebViewState.NotInitialized;
    private CancellationTokenSource? _initCancellationTokenSource;
    private CancellationTokenSource? _navigationCancellationTokenSource;
    private TaskCompletionSource<bool>? _initCompletionSource;
    private string? _initFailureReason;
    private int _initRetryCount;
    private const int MaxInitRetries = 3;
    private const int InitRetryDelayMs = 1000;

    public string? Source { get; private set; }
    public bool CanGoBack => _coreWebView2?.CanGoBack ?? false;
    public bool CanGoForward => _coreWebView2?.CanGoForward ?? false;
    public bool IsReady => _isReady;
    public CoreWebView2? CoreWebView2 => _coreWebView2;

    public event EventHandler? NavigationStarting;
    public event EventHandler? NavigationCompleted;
    public event EventHandler? SourceChanged;
    public event EventHandler? Ready;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? DocumentTitleChanged;
    public event EventHandler<string>? InitializationFailed;

    private static IntPtr _darkBackgroundBrush = IntPtr.Zero;
    private static readonly object _brushLock = new object();
    private const uint DarkBackgroundColor = AppConstants.WebView2Theme.LightBackgroundColorUInt;
    
    private IntPtr _originalWndProc;
    private WndProcDelegate? _wndProcDelegate;
    
    private const uint WS_CHILD = 0x40000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_CLIPCHILDREN = 0x02000000;
    private const uint WS_CLIPSIBLINGS = 0x04000000;
    
    private const int GWLP_WNDPROC = -4;
    
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        Logger.LogInfo(LogCategory, $"CreateNativeControlCore START, parent={parent.Handle:X}");
        
        try
        {
            lock (_brushLock)
            {
                if (_darkBackgroundBrush == IntPtr.Zero)
                {
                    _darkBackgroundBrush = CreateSolidBrush(DarkBackgroundColor);
                    Logger.LogInfo(LogCategory, $"Created dark background brush: 0x{DarkBackgroundColor:X8}");
                }
            }
            
            _hwnd = CreateWindowExW(
                0,
                "STATIC",
                "",
                WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                0, 0,
                800, 600,
                parent.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            Logger.LogInfo(LogCategory, $"Created host HWND={_hwnd:X}");

            _wndProcDelegate = CustomWndProc;
            _originalWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
            Logger.LogInfo(LogCategory, $"Subclassed window, original WndProc={_originalWndProc:X}");

            ShowWindow(_hwnd, SW_SHOW);
            
            return new PlatformHandle(_hwnd, "HWND");
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"CreateNativeControlCore FAILED: {ex.Message}");
            throw;
        }
    }
    
    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Logger.LogInfo(LogCategory, $"OnAttachedToVisualTree, State={_state}");
        
        if (_state == WebViewState.NotInitialized)
        {
            StartInitialization();
        }
    }

    private async Task InitializeWebView2Async(CancellationToken cancellationToken)
    {
        if (_state == WebViewState.Disposed)
        {
            Logger.LogInfo(LogCategory, "InitializeWebView2 ABORTED: Control is disposed");
            return;
        }
        
        if (_state == WebViewState.Initializing)
        {
            Logger.LogInfo(LogCategory, "InitializeWebView2 SKIPPED: Already initializing");
            return;
        }
        
        _state = WebViewState.Initializing;
        _initCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        try
        {
            if (_hwnd == IntPtr.Zero)
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: HWND is Zero");
                SetInitFailed("窗口句柄无效");
                return;
            }

            if (!IsWindow(_hwnd))
            {
                Logger.LogInfo(LogCategory, $"InitializeWebView2 FAILED: HWND {_hwnd:X} is not a valid window");
                SetInitFailed("窗口已销毁");
                return;
            }

            Logger.LogInfo(LogCategory, $"InitializeWebView2 START, HWND={_hwnd:X}, RetryCount={_initRetryCount}");

            var env = await GetOrCreateSharedEnvironmentAsync(cancellationToken);
            if (env == null)
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: Failed to create shared environment");
                SetInitFailed("WebView2 环境创建失败");
                return;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 CANCELLED after environment creation");
                SetInitFailed("初始化已取消");
                return;
            }
            
            Logger.LogInfo(LogCategory, "Shared environment ready");
            
            if (_hwnd == IntPtr.Zero || !IsWindow(_hwnd))
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: HWND became invalid during async operation");
                SetInitFailed("窗口在初始化过程中失效");
                return;
            }
            
            CoreWebView2Controller? controller = null;
            controller = await env.CreateCoreWebView2ControllerAsync(_hwnd);
            
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 CANCELLED after controller creation");
                controller?.Close();
                SetInitFailed("初始化已取消");
                return;
            }
            
            Logger.LogInfo(LogCategory, "Controller created");
            
            if (controller == null)
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: Controller is null");
                SetInitFailed("WebView2 控制器创建失败");
                return;
            }
            
            _controller = controller;
            _coreWebView2 = controller.CoreWebView2;
            
            if (_coreWebView2 == null)
            {
                Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: CoreWebView2 is null");
                SetInitFailed("WebView2 核心对象初始化失败");
                _controller = null;
                return;
            }
            
            _controller.DefaultBackgroundColor = System.Drawing.Color.FromArgb(
                AppConstants.WebView2Theme.LightBackgroundColorR, 
                AppConstants.WebView2Theme.LightBackgroundColorG, 
                AppConstants.WebView2Theme.LightBackgroundColorB);
            Logger.LogInfo(LogCategory, "DefaultBackgroundColor set to " + AppConstants.WebView2Theme.LightBackgroundColorHex);

            await ApplyStealthModeAsync(cancellationToken);

            _coreWebView2.SourceChanged += OnCoreWebView2SourceChanged;
            _coreWebView2.DocumentTitleChanged += OnCoreWebView2DocumentTitleChanged;
            _coreWebView2.NavigationStarting += OnCoreWebView2NavigationStarting;
            _coreWebView2.ContentLoading += OnCoreWebView2ContentLoading;
            _coreWebView2.NavigationCompleted += OnCoreWebView2NavigationCompleted;
            _coreWebView2.NewWindowRequested += OnCoreWebView2NewWindowRequested;

            _state = WebViewState.Ready;
            _isReady = true;
            _initRetryCount = 0;
            _initFailureReason = null;
            
            Logger.LogInfo(LogCategory, "CoreWebView2 READY!");
            
            _initCompletionSource.TrySetResult(true);

            UpdateBounds();

            if (!string.IsNullOrEmpty(_pendingUrl) && _pendingUrl != "about:blank")
            {
                var url = _pendingUrl;
                _pendingUrl = null;
                Navigate(url);
            }

            Ready?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInfo(LogCategory, "InitializeWebView2 was cancelled");
            SetInitFailed("初始化已取消");
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"InitializeWebView2 FAILED: {ex.Message}");
            Logger.LogInfo(LogCategory, $"Stack: {ex.StackTrace}");
            SetInitFailed($"WebView2 初始化失败: {ex.Message}");
        }
    }
    
    private void SetInitFailed(string reason)
    {
        _state = WebViewState.Failed;
        _isReady = false;
        _initFailureReason = reason;
        _initCompletionSource?.TrySetResult(false);
        ErrorOccurred?.Invoke(this, reason);
        InitializationFailed?.Invoke(this, reason);
    }
    
    private static string GetWebView2UserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseFolder = Path.Combine(localAppData, "NekoT");
        
        var userDataFolder = Path.Combine(baseFolder, $"WebView2Data_{Environment.ProcessId}");
        
        if (!Directory.Exists(userDataFolder))
        {
            try
            {
                Directory.CreateDirectory(userDataFolder);
                Logger.LogInfo(LogCategory, $"Created unique userDataFolder: {userDataFolder}");
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LogCategory, $"Failed to create userDataFolder: {ex.Message}");
                userDataFolder = Path.Combine(Path.GetTempPath(), "NekoT_WebView2", Guid.NewGuid().ToString());
                Directory.CreateDirectory(userDataFolder);
            }
        }
        
        return userDataFolder;
    }

    private static async Task<CoreWebView2Environment?> GetOrCreateSharedEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (_sharedEnvironment != null)
        {
            var count = Interlocked.Increment(ref _environmentInitCount);
            Logger.LogInfo(LogCategory, $"Reusing existing shared environment (instance #{count})");
            return _sharedEnvironment;
        }

        await _environmentInitLock.WaitAsync(cancellationToken);
        
        try
        {
            if (_sharedEnvironment != null)
            {
                var count = Interlocked.Increment(ref _environmentInitCount);
                Logger.LogInfo(LogCategory, $"Reusing existing shared environment (instance #{count})");
                return _sharedEnvironment;
            }

            Logger.LogInfo(LogCategory, "Creating new shared environment...");
            
            var envOptions = new CoreWebView2EnvironmentOptions
            {
                AllowSingleSignOnUsingOSPrimaryAccount = false
            };
            
            var proxyUrl = UserSettingsService.Instance.ProxyUrl;
            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                envOptions.AdditionalBrowserArguments = $"--proxy-server={proxyUrl}";
                Logger.LogInfo(LogCategory, $"Proxy configured: {proxyUrl}");
            }
            
            var userDataFolder = GetWebView2UserDataFolder();
            Logger.LogInfo(LogCategory, $"Shared environment userDataFolder: {userDataFolder}");
            
            _sharedEnvironment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder,
                options: envOptions);
            
            var initCount = Interlocked.Increment(ref _environmentInitCount);
            Logger.LogInfo(LogCategory, $"Shared environment created successfully (instance #{initCount})");
            
            return _sharedEnvironment;
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"Failed to create shared environment: {ex.Message}");
            return null;
        }
        finally
        {
            _environmentInitLock.Release();
        }
    }

    private void OnCoreWebView2SourceChanged(object? s, CoreWebView2SourceChangedEventArgs args)
    {
        Source = _coreWebView2.Source;
        SourceChanged?.Invoke(this, EventArgs.Empty);
        Logger.LogInfo(LogCategory, $"Source changed: {Source}");
    }