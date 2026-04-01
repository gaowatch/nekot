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

public partial class WebView2Control : NativeControlHost
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
            if (!Directory.Exists(baseFolder)) return;
            var folders = Directory.GetDirectories(baseFolder, "WebView2Data_*");
            var currentProcessId = Environment.ProcessId;
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                var pidStr = folderName.Replace("WebView2Data_", "");
                if (int.TryParse(pidStr, out var pid) && pid != currentProcessId)
                {
                    try { var process = System.Diagnostics.Process.GetProcessById(pid); }
                    catch (ArgumentException)
                    {
                        try { Directory.Delete(folder, true); }
                        catch (Exception deleteEx) { System.Diagnostics.Debug.WriteLine($"[WebView2] Failed to delete orphan folder {folder}: {deleteEx.Message}"); }
                    }
                }
            }
        }
        catch (Exception ex) { Logger.LogError(LogCategory, "Cleanup failed", ex); }
    }
    
    public WebView2Control() { Logger.LogInfo(LogCategory, "Constructor called"); }

    private CoreWebView2Controller? _controller;
    private CoreWebView2? _coreWebView2;
    private IntPtr _hwnd;
    private string? _pendingUrl;
    private bool _isReady;
    private string? _lastNavigatedUrl;
    private bool _isShowingErrorPage;
    private int _errorPageRetryCount;
    private const int MaxErrorPageRetries = 3;
    
    private enum WebViewState { NotInitialized, Initializing, Ready, Failed, Disposed }
    
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
            _hwnd = CreateWindowExW(0, "STATIC", "", WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS, 0, 0, 800, 600, parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            Logger.LogInfo(LogCategory, $"Created host HWND={_hwnd:X}");
            _wndProcDelegate = CustomWndProc;
            _originalWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
            Logger.LogInfo(LogCategory, $"Subclassed window, original WndProc={_originalWndProc:X}");
            ShowWindow(_hwnd, SW_SHOW);
            return new PlatformHandle(_hwnd, "HWND");
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"CreateNativeControlCore FAILED: {ex.Message}"); throw; }
    }
    
    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) => CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Logger.LogInfo(LogCategory, $"OnAttachedToVisualTree, State={_state}");
        if (_state == WebViewState.NotInitialized) StartInitialization();
    }

    private async Task InitializeWebView2Async(CancellationToken cancellationToken)
    {
        if (_state == WebViewState.Disposed) { Logger.LogInfo(LogCategory, "InitializeWebView2 ABORTED: Control is disposed"); return; }
        if (_state == WebViewState.Initializing) { Logger.LogInfo(LogCategory, "InitializeWebView2 SKIPPED: Already initializing"); return; }
        _state = WebViewState.Initializing;
        _initCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            if (_hwnd == IntPtr.Zero) { Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: HWND is Zero"); SetInitFailed("窗口句柄无效"); return; }
            if (!IsWindow(_hwnd)) { Logger.LogInfo(LogCategory, $"InitializeWebView2 FAILED: HWND {_hwnd:X} is not a valid window"); SetInitFailed("窗口已销毁"); return; }
            Logger.LogInfo(LogCategory, $"InitializeWebView2 START, HWND={_hwnd:X}, RetryCount={_initRetryCount}");
            var env = await GetOrCreateSharedEnvironmentAsync(cancellationToken);
            if (env == null) { Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: Failed to create shared environment"); SetInitFailed("WebView2 环境创建失败"); return; }
            if (cancellationToken.IsCancellationRequested) { Logger.LogInfo(LogCategory, "InitializeWebView2 CANCELLED after environment creation"); SetInitFailed("初始化已取消"); return; }
            Logger.LogInfo(LogCategory, "Shared environment ready");
            if (_hwnd == IntPtr.Zero || !IsWindow(_hwnd)) { Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: HWND became invalid during async operation"); SetInitFailed("窗口在初始化过程中失效"); return; }
            CoreWebView2Controller? controller = null;
            controller = await env.CreateCoreWebView2ControllerAsync(_hwnd);
            if (cancellationToken.IsCancellationRequested) { Logger.LogInfo(LogCategory, "InitializeWebView2 CANCELLED after controller creation"); controller?.Close(); SetInitFailed("初始化已取消"); return; }
            Logger.LogInfo(LogCategory, "Controller created");
            if (controller == null) { Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: Controller is null"); SetInitFailed("WebView2 控制器创建失败"); return; }
            _controller = controller;
            _coreWebView2 = controller.CoreWebView2;
            if (_coreWebView2 == null) { Logger.LogInfo(LogCategory, "InitializeWebView2 FAILED: CoreWebView2 is null"); SetInitFailed("WebView2 核心对象初始化失败"); _controller = null; return; }
            _controller.DefaultBackgroundColor = System.Drawing.Color.FromArgb(AppConstants.WebView2Theme.LightBackgroundColorR, AppConstants.WebView2Theme.LightBackgroundColorG, AppConstants.WebView2Theme.LightBackgroundColorB);
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
            if (!string.IsNullOrEmpty(_pendingUrl) && _pendingUrl != "about:blank") { var url = _pendingUrl; _pendingUrl = null; Navigate(url); }
            Ready?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { Logger.LogInfo(LogCategory, "InitializeWebView2 was cancelled"); SetInitFailed("初始化已取消"); }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"InitializeWebView2 FAILED: {ex.Message}"); Logger.LogInfo(LogCategory, $"Stack: {ex.StackTrace}"); SetInitFailed($"WebView2 初始化失败: {ex.Message}"); }
    }
    
    private void SetInitFailed(string reason) { _state = WebViewState.Failed; _isReady = false; _initFailureReason = reason; _initCompletionSource?.TrySetResult(false); ErrorOccurred?.Invoke(this, reason); InitializationFailed?.Invoke(this, reason); }
    
    private static string GetWebView2UserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseFolder = Path.Combine(localAppData, "NekoT");
        var userDataFolder = Path.Combine(baseFolder, $"WebView2Data_{Environment.ProcessId}");
        if (!Directory.Exists(userDataFolder))
        {
            try { Directory.CreateDirectory(userDataFolder); Logger.LogInfo(LogCategory, $"Created unique userDataFolder: {userDataFolder}"); }
            catch (Exception ex) { Logger.LogInfo(LogCategory, $"Failed to create userDataFolder: {ex.Message}"); userDataFolder = Path.Combine(Path.GetTempPath(), "NekoT_WebView2", Guid.NewGuid().ToString()); Directory.CreateDirectory(userDataFolder); }
        }
        return userDataFolder;
    }

    private static async Task<CoreWebView2Environment?> GetOrCreateSharedEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (_sharedEnvironment != null) { var count = Interlocked.Increment(ref _environmentInitCount); Logger.LogInfo(LogCategory, $"Reusing existing shared environment (instance #{count})"); return _sharedEnvironment; }
        await _environmentInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_sharedEnvironment != null) { var count = Interlocked.Increment(ref _environmentInitCount); Logger.LogInfo(LogCategory, $"Reusing existing shared environment (instance #{count})"); return _sharedEnvironment; }
            Logger.LogInfo(LogCategory, "Creating new shared environment...");
            var envOptions = new CoreWebView2EnvironmentOptions { AllowSingleSignOnUsingOSPrimaryAccount = false };
            var proxyUrl = UserSettingsService.Instance.ProxyUrl;
            if (!string.IsNullOrWhiteSpace(proxyUrl)) { envOptions.AdditionalBrowserArguments = $"--proxy-server={proxyUrl}"; Logger.LogInfo(LogCategory, $"Proxy configured: {proxyUrl}"); }
            var userDataFolder = GetWebView2UserDataFolder();
            Logger.LogInfo(LogCategory, $"Shared environment userDataFolder: {userDataFolder}");
            _sharedEnvironment = await CoreWebView2Environment.CreateAsync(browserExecutableFolder: null, userDataFolder: userDataFolder, options: envOptions);
            var initCount = Interlocked.Increment(ref _environmentInitCount);
            Logger.LogInfo(LogCategory, $"Shared environment created successfully (instance #{initCount})");
            return _sharedEnvironment;
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"Failed to create shared environment: {ex.Message}"); return null; }
        finally { _environmentInitLock.Release(); }
    }

    private void OnCoreWebView2SourceChanged(object? s, CoreWebView2SourceChangedEventArgs args) { Source = _coreWebView2.Source; SourceChanged?.Invoke(this, EventArgs.Empty); Logger.LogInfo(LogCategory, $"Source changed: {Source}"); }
    private void OnCoreWebView2DocumentTitleChanged(object? s, object args) { if (_coreWebView2 != null) { var title = _coreWebView2.DocumentTitle; DocumentTitleChanged?.Invoke(this, title); Logger.LogInfo(LogCategory, $"Document title changed: {title}"); } }
    private void OnCoreWebView2NavigationStarting(object? s, CoreWebView2NavigationStartingEventArgs args) { if (_controller != null) { _controller.DefaultBackgroundColor = System.Drawing.Color.FromArgb(AppConstants.WebView2Theme.LightBackgroundColorR, AppConstants.WebView2Theme.LightBackgroundColorG, AppConstants.WebView2Theme.LightBackgroundColorB); } Logger.LogInfo(LogCategory, $"Navigation starting to: {args.Uri}"); NavigationStarting?.Invoke(this, EventArgs.Empty); }
    private void OnCoreWebView2ContentLoading(object? s, CoreWebView2ContentLoadingEventArgs args) { Logger.LogInfo(LogCategory, $"Content loading, IsErrorPage={args.IsErrorPage}"); }
    private void OnCoreWebView2NavigationCompleted(object? s, CoreWebView2NavigationCompletedEventArgs args)
    {
        NavigationCompleted?.Invoke(this, EventArgs.Empty);
        Logger.LogInfo(LogCategory, $"Navigation completed: IsSuccess={args.IsSuccess}, ErrorStatus={args.WebErrorStatus}, HttpStatusCode={args.HttpStatusCode}");
        if (args.IsSuccess) { _isShowingErrorPage = false; _errorPageRetryCount = 0; return; }
        if (_isShowingErrorPage) { Logger.LogInfo(LogCategory, "NavigationCompleted while showing error page, skipping to prevent infinite loop"); return; }
        if (!args.IsSuccess)
        {
            if (_errorPageRetryCount >= MaxErrorPageRetries) { Logger.LogInfo(LogCategory, $"Max error page retries ({MaxErrorPageRetries}) reached, stopping"); return; }
            _errorPageRetryCount++;
            var errorMessage = GetErrorMessage(args.WebErrorStatus);
            Logger.LogInfo(LogCategory, $"Navigation failed (network error): {errorMessage}, calling ShowErrorPage (attempt {_errorPageRetryCount})");
            ShowErrorPage(errorMessage, _lastNavigatedUrl ?? Source ?? "");
            ErrorOccurred?.Invoke(this, errorMessage);
        }
        else if (args.HttpStatusCode >= 400)
        {
            if (_errorPageRetryCount >= MaxErrorPageRetries) { Logger.LogInfo(LogCategory, $"Max error page retries ({MaxErrorPageRetries}) reached, stopping"); return; }
            _errorPageRetryCount++;
            var errorMessage = GetHttpErrorMessage(args.HttpStatusCode);
            Logger.LogInfo(LogCategory, $"Navigation failed (HTTP {args.HttpStatusCode}): {errorMessage}, calling ShowErrorPage (attempt {_errorPageRetryCount})");
            ShowErrorPage(errorMessage, _lastNavigatedUrl ?? Source ?? "");
            ErrorOccurred?.Invoke(this, errorMessage);
        }
    }
    
    private void OnCoreWebView2NewWindowRequested(object? s, CoreWebView2NewWindowRequestedEventArgs args)
    {
        Logger.LogInfo(LogCategory, $"NewWindowRequested: Uri={args.Uri}, IsUserInitiated={args.IsUserInitiated}");
        args.Handled = true;
        if (string.IsNullOrWhiteSpace(args.Uri)) { Logger.LogInfo(LogCategory, "NewWindowRequested: URI is empty, cancelling"); return; }
        if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri)) { Logger.LogInfo(LogCategory, $"NewWindowRequested: Invalid URI format: {args.Uri}"); return; }
        var allowedSchemes = new[] { "http", "https" };
        if (!allowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase)) { Logger.LogInfo(LogCategory, $"NewWindowRequested: Blocked disallowed scheme '{uri.Scheme}' for URI: {args.Uri}"); return; }
        Logger.LogInfo(LogCategory, $"NewWindowRequested: Navigating to {args.Uri} in current window");
        Navigate(args.Uri);
    }
    
    private async Task ApplyStealthModeAsync(CancellationToken cancellationToken)
    {
        if (_coreWebView2 == null) return;
        try
        {
            var settings = UserSettingsService.Instance;
            if (!settings.EnableStealthMode) { Logger.LogInfo(LogCategory, "Stealth mode disabled by user"); return; }
            var userAgent = settings.UserAgent;
            if (!string.IsNullOrEmpty(userAgent)) { _coreWebView2.Settings.UserAgent = userAgent; Logger.LogInfo(LogCategory, $"User-Agent set: {userAgent[..Math.Min(50, userAgent.Length)]}..."); }
            else { _coreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"; Logger.LogInfo(LogCategory, "User-Agent set to default Chrome"); }
            _coreWebView2.Settings.AreDevToolsEnabled = !settings.DisableDevTools;
            _coreWebView2.Settings.AreDefaultContextMenusEnabled = !settings.DisableDevTools;
            _coreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            _coreWebView2.Settings.IsStatusBarEnabled = false;
            if (cancellationToken.IsCancellationRequested) { Logger.LogInfo(LogCategory, "ApplyStealthModeAsync CANCELLED before script injection"); return; }
            var stealthScript = GetStealthScript();
            await _coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(stealthScript);
            if (settings.BlockTracking) { var trackingProtectionScript = GetTrackingProtectionScript(); await _coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(trackingProtectionScript); Logger.LogInfo(LogCategory, "Tracking protection script injected"); }
            if (settings.BlockAds) { var adBlockScript = GetAdBlockScript(); await _coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(adBlockScript); Logger.LogInfo(LogCategory, "Ad blocking script injected"); }
            var lightBackgroundCss = $"html, body {{ background-color: {AppConstants.WebView2Theme.LightBackgroundColorHex} !important; background: {AppConstants.WebView2Theme.LightBackgroundColorHex} !important; }}";
            await _coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($"var style = document.createElement('style'); style.textContent = `{lightBackgroundCss}`; document.head ? document.head.appendChild(style) : document.documentElement.appendChild(style);");
            Logger.LogInfo(LogCategory, "Stealth mode applied successfully");
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"ApplyStealthModeAsync failed: {ex.Message}"); }
    }

    private static string GetTrackingProtectionScript() => @"(function() { 'use strict'; try { var blockedDomains = ['google-analytics.com','googletagmanager.com','analytics.google.com','facebook.com/tr','connect.facebook.net','ads.twitter.com','analytics.twitter.com','scorecardresearch.com','quantserve.com','newrelic.com','hotjar.com','fullstory.com','mixpanel.com','amplitude.com','segment.com']; var originalCreateElement = document.createElement; document.createElement = function(tag) { var element = originalCreateElement.call(document, tag); if (tag.toLowerCase() === 'script') { var originalSetAttribute = element.setAttribute; element.setAttribute = function(name, value) { if (name === 'src' && typeof value === 'string') { for (var i = 0; i < blockedDomains.length; i++) { if (value.indexOf(blockedDomains[i]) !== -1) { console.log('[NekoT] Blocked tracking script:', value); return; } } } return originalSetAttribute.call(this, name, value); }; } return element; }; var originalAppendChild = Element.prototype.appendChild; Element.prototype.appendChild = function(child) { if (child && child.tagName === 'SCRIPT' && child.src) { for (var i = 0; i < blockedDomains.length; i++) { if (child.src.indexOf(blockedDomains[i]) !== -1) { console.log('[NekoT] Blocked tracking script append:', child.src); return child; } } } return originalAppendChild.call(this, child); }; var imgProto = HTMLImageElement.prototype; var originalSrcSetter = Object.getOwnPropertyDescriptor(imgProto, 'src').set; Object.defineProperty(imgProto, 'src', { set: function(value) { if (typeof value === 'string') { for (var i = 0; i < blockedDomains.length; i++) { if (value.indexOf(blockedDomains[i]) !== -1) { console.log('[NekoT] Blocked tracking pixel:', value); return; } } } return originalSrcSetter.call(this, value); } }); var originalXHROpen = XMLHttpRequest.prototype.open; XMLHttpRequest.prototype.open = function(method, url) { for (var i = 0; i < blockedDomains.length; i++) { if (url.indexOf(blockedDomains[i]) !== -1) { console.log('[NekoT] Blocked tracking XHR:', url); this._blocked = true; return; } } return originalXHROpen.apply(this, arguments); }; var originalXHRSend = XMLHttpRequest.prototype.send; XMLHttpRequest.prototype.send = function() { if (this._blocked) { return; } return originalXHRSend.apply(this, arguments); }; var originalFetch = window.fetch; window.fetch = function(url) { var urlStr = typeof url === 'string' ? url : url.url; for (var i = 0; i < blockedDomains.length; i++) { if (urlStr && urlStr.indexOf(blockedDomains[i]) !== -1) { console.log('[NekoT] Blocked tracking fetch:', urlStr); return Promise.resolve(new Response('', { status: 200 })); } } return originalFetch.apply(this, arguments); }; console.log('[NekoT] Tracking protection enabled'); } catch (e) { console.error('[NekoT] Tracking protection error:', e); } })();";

    private static string GetAdBlockScript() => @"(function() { 'use strict'; try { var adDomains = ['googleads.g.doubleclick.net','pagead2.googlesyndication.com','adservice.google.com','ads.google.com','doubleclick.net','googlesyndication.com','facebook.com/tr','facebook.net/en_US/fbevents.js','ads.twitter.com','ads.yahoo.com','amazon-adsystem.com','ads.youtube.com']; var hideAdsCss = `[class*='ad-'], [id*='ad-'], [class*='ads-'], [id*='ads-'], [class*='advert'], [id*='advert'], [class*='banner'], [class*='sponsor'], [class*='promo'], [class*='commercial'], [data-ad], [data-ads], iframe[src*='doubleclick'], iframe[src*='googlesyndication'], iframe[src*='googleads'], ins.adsbygoogle, .adsbygoogle, div[class*='google-ad'], div[id*='google-ad'], [aria-label*='advertisement'], [aria-label*='Ad'], a[href*='click'] img, a[href*='track'] img { display: none !important; visibility: hidden !important; height: 0 !important; width: 0 !important; }`; var style = document.createElement('style'); style.textContent = hideAdsCss; (document.head || document.documentElement).appendChild(style); var originalCreateElement = document.createElement; document.createElement = function(tag) { var element = originalCreateElement.call(document, tag); if (tag.toLowerCase() === 'script' || tag.toLowerCase() === 'iframe') { var originalSetAttribute = element.setAttribute; element.setAttribute = function(name, value) { if (name === 'src' && typeof value === 'string') { for (var i = 0; i < adDomains.length; i++) { if (value.indexOf(adDomains[i]) !== -1) { console.log('[NekoT] Blocked ad:', value); return; } } } return originalSetAttribute.call(this, name, value); }; } return element; }; var originalAppendChild = Element.prototype.appendChild; Element.prototype.appendChild = function(child) { if (child && (child.tagName === 'SCRIPT' || child.tagName === 'IFRAME') && child.src) { for (var i = 0; i < adDomains.length; i++) { if (child.src.indexOf(adDomains[i]) !== -1) { console.log('[NekoT] Blocked ad element:', child.src); return child; } } } return originalAppendChild.call(this, child); }; console.log('[NekoT] Ad blocking enabled'); } catch (e) { console.error('[NekoT] Ad blocking error:', e); } })();";

    private static string GetStealthScript() => @"(function() { 'use strict'; if (navigator.webdriver !== undefined) { Object.defineProperty(navigator, 'webdriver', { get: function() { return undefined; }, configurable: true }); } Object.defineProperty(navigator, 'languages', { get: function() { return ['zh-CN', 'zh', 'en-US', 'en']; } }); Object.defineProperty(navigator, 'platform', { get: function() { return 'Win32'; } }); Object.defineProperty(navigator, 'hardwareConcurrency', { get: function() { return 8; } }); Object.defineProperty(navigator, 'deviceMemory', { get: function() { return 8; } }); window.chrome = { app: { isInstalled: false }, webstore: {}, runtime: {} }; var getParameter = WebGLRenderingContext.prototype.getParameter; WebGLRenderingContext.prototype.getParameter = function(parameter) { if (parameter === 37445) return 'Intel Inc.'; if (parameter === 37446) return 'Intel Iris OpenGL Engine'; return getParameter.call(this, parameter); }; var originalToDataURL = HTMLCanvasElement.prototype.toDataURL; HTMLCanvasElement.prototype.toDataURL = function() { if (this.width === 220 && this.height === 30) { var context = this.getContext('2d'); if (context) { var imageData = context.getImageData(0, 0, this.width, this.height); for (var i = 0; i < imageData.data.length; i += 4) { imageData.data[i] ^= (Math.random() * 2) | 0; } context.putImageData(imageData, 0, 0); } } return originalToDataURL.apply(this, arguments); }; var originalGetTimezoneOffset = Date.prototype.getTimezoneOffset; Date.prototype.getTimezoneOffset = function() { return -480; }; })();";

    protected override void OnSizeChanged(SizeChangedEventArgs e) { base.OnSizeChanged(e); UpdateBounds(); }

    private void UpdateBounds()
    {
        if (_controller != null)
        {
            var bounds = Bounds;
            var width = Math.Max(1, (int)bounds.Width);
            var height = Math.Max(1, (int)bounds.Height);
            _controller.Bounds = new System.Drawing.Rectangle(0, 0, width, height);
            _controller.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
            Logger.LogInfo(LogCategory, $"Bounds updated: {width}x{height}");
        }
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        Logger.LogInfo(LogCategory, $"DestroyNativeControlCore called, State={_state}");
        if (_state == WebViewState.Initializing) { Logger.LogInfo(LogCategory, "DestroyNativeControlCore: WebView2 is initializing, cancel it!"); _initCancellationTokenSource?.Cancel(); }
        if (_hwnd != IntPtr.Zero)
        {
            if (_originalWndProc != IntPtr.Zero) { SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _originalWndProc); _originalWndProc = IntPtr.Zero; }
            _wndProcDelegate = null;
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
        if (_controller != null)
        {
            try { var closeTask = Task.Run(() => _controller.Close()); if (!closeTask.Wait(TimeSpan.FromSeconds(3))) { Logger.LogInfo(LogCategory, "Controller.Close() timeout after 3 seconds, forcing cleanup"); } else { Logger.LogInfo(LogCategory, "Controller.Close() completed successfully"); } }
            catch (Exception ex) { Logger.LogInfo(LogCategory, $"Error closing controller: {ex.Message}"); }
            _controller = null;
        }
        if (_coreWebView2 != null)
        {
            _coreWebView2.SourceChanged -= OnCoreWebView2SourceChanged;
            _coreWebView2.DocumentTitleChanged -= OnCoreWebView2DocumentTitleChanged;
            _coreWebView2.NavigationStarting -= OnCoreWebView2NavigationStarting;
            _coreWebView2.ContentLoading -= OnCoreWebView2ContentLoading;
            _coreWebView2.NavigationCompleted -= OnCoreWebView2NavigationCompleted;
            _coreWebView2.NewWindowRequested -= OnCoreWebView2NewWindowRequested;
            _coreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _coreWebView2 = null;
        }
        _state = WebViewState.Disposed;
    }

    public void Navigate(string url)
    {
        Logger.LogInfo(LogCategory, $"Navigate: {url}, State={_state}");
        if (string.IsNullOrWhiteSpace(url)) { Logger.LogInfo(LogCategory, "Navigate FAILED: URL is null or empty"); ErrorOccurred?.Invoke(this, "URL 不能为空"); return; }
        _isShowingErrorPage = false;
        _errorPageRetryCount = 0;
        switch (_state)
        {
            case WebViewState.Ready: NavigateInternal(url); break;
            case WebViewState.Initializing: _pendingUrl = url; Logger.LogInfo(LogCategory, "Navigate DEFERRED: WebView2 is initializing, saved pending URL"); break;
            case WebViewState.Failed: _pendingUrl = url; TryReinitialize(); break;
            case WebViewState.NotInitialized: _pendingUrl = url; StartInitialization(); break;
            case WebViewState.Disposed: Logger.LogInfo(LogCategory, "Navigate FAILED: Control is disposed"); ErrorOccurred?.Invoke(this, "控件已销毁"); break;
        }
    }
    
    public async Task<bool> NavigateAsync(string url, CancellationToken cancellationToken = default)
    {
        Logger.LogInfo(LogCategory, $"NavigateAsync: {url}, State={_state}");
        if (string.IsNullOrWhiteSpace(url)) { Logger.LogInfo(LogCategory, "NavigateAsync FAILED: URL is null or empty"); ErrorOccurred?.Invoke(this, "URL 不能为空"); return false; }
        if (_state == WebViewState.Disposed) { Logger.LogInfo(LogCategory, "NavigateAsync FAILED: Control is disposed"); ErrorOccurred?.Invoke(this, "控件已销毁"); return false; }
        _isShowingErrorPage = false;
        _errorPageRetryCount = 0;
        try
        {
            if (_state == WebViewState.Initializing && _initCompletionSource != null)
            {
                Logger.LogInfo(LogCategory, "NavigateAsync: Waiting for initialization to complete...");
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _initCancellationTokenSource?.Token ?? CancellationToken.None);
                var completed = await _initCompletionSource.Task.WaitAsync(linkedCts.Token);
                if (!completed || _state != WebViewState.Ready) { Logger.LogInfo(LogCategory, $"NavigateAsync FAILED: Initialization did not complete successfully, State={_state}"); return false; }
            }
            else if (_state == WebViewState.Failed || _state == WebViewState.NotInitialized)
            {
                Logger.LogInfo(LogCategory, "NavigateAsync: Triggering initialization...");
                if (!StartInitialization()) { Logger.LogInfo(LogCategory, "NavigateAsync FAILED: Cannot start initialization"); return false; }
                if (_initCompletionSource == null) { Logger.LogInfo(LogCategory, "NavigateAsync FAILED: InitCompletionSource is null"); return false; }
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _initCancellationTokenSource?.Token ?? CancellationToken.None);
                var completed = await _initCompletionSource.Task.WaitAsync(linkedCts.Token);
                if (!completed || _state != WebViewState.Ready) { Logger.LogInfo(LogCategory, $"NavigateAsync FAILED: Initialization failed, State={_state}"); return false; }
            }
            NavigateInternal(url);
            return true;
        }
        catch (OperationCanceledException) { Logger.LogInfo(LogCategory, "NavigateAsync CANCELLED"); return false; }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"NavigateAsync EXCEPTION: {ex.Message}"); return false; }
    }
    
    private void NavigateInternal(string url)
    {
        if (_coreWebView2 == null) { Logger.LogInfo(LogCategory, "NavigateInternal FAILED: CoreWebView2 is null"); ErrorOccurred?.Invoke(this, "WebView2 未初始化"); return; }
        try
        {
            if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)) { Logger.LogInfo(LogCategory, "NavigateInternal BLOCKED: Dangerous protocol in URL"); ErrorOccurred?.Invoke(this, "不允许的协议"); return; }
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !url.Equals("about:blank", StringComparison.OrdinalIgnoreCase)) { url = "https://" + url; }
            _lastNavigatedUrl = url;
            _coreWebView2.Navigate(url);
            Logger.LogInfo(LogCategory, $"Navigate SUCCESS: {url}");
        }
        catch (Exception ex) { Logger.LogInfo(LogCategory, $"Navigate FAILED: {ex.Message}"); ErrorOccurred?.Invoke(this, $"导航失败: {ex.Message}"); }
    }
    
    private bool StartInitialization()
    {
        if (_state == WebViewState.Disposed) { Logger.LogInfo(LogCategory, "StartInitialization FAILED: Control is disposed"); return false; }
        if (_state == WebViewState.Initializing) { Logger.LogInfo(LogCategory, "StartInitialization SKIPPED: Already initializing"); return true; }
        if (_hwnd == IntPtr.Zero) { Logger.LogInfo(LogCategory, "StartInitialization FAILED: HWND is zero"); return false; }
        _initCancellationTokenSource?.Cancel();
        _initCancellationTokenSource = new CancellationTokenSource();
        _ = InitializeWebView2Async(_initCancellationTokenSource.Token).ContinueWith(task => { if (task.Exception != null) { Logger.LogInfo(LogCategory, $"InitializeWebView2 unhandled exception: {task.Exception}"); } }, TaskContinuationOptions.OnlyOnFaulted);
        return true;
    }
    
    private void TryReinitialize()
    {
        if (_initRetryCount >= MaxInitRetries) { Logger.LogInfo(LogCategory, $"TryReinitialize FAILED: Max retries ({MaxInitRetries}) reached"); ErrorOccurred?.Invoke(this, $"初始化失败次数过多，请重启应用。原因: {_initFailureReason}"); return; }
        _initRetryCount++;
        Logger.LogInfo(LogCategory, $"TryReinitialize: Attempt {_initRetryCount}/{MaxInitRetries}, Previous failure: {_initFailureReason}");
        _ = Task.Run(async () => { await Task.Delay(InitRetryDelayMs); if (_state == WebViewState.Failed || _state == WebViewState.NotInitialized) { StartInitialization(); } });
    }

    private static string GetErrorMessage(CoreWebView2WebErrorStatus errorStatus)
    {
        var code = (int)errorStatus;
        return errorStatus switch
        {
            CoreWebView2WebErrorStatus.ConnectionAborted => "连接已中止",
            CoreWebView2WebErrorStatus.ConnectionReset => "连接已重置",
            CoreWebView2WebErrorStatus.Disconnected => "连接已断开",
            CoreWebView2WebErrorStatus.CannotConnect => "无法连接到服务器",
            CoreWebView2WebErrorStatus.HostNameNotResolved => "无法解析域名",
            CoreWebView2WebErrorStatus.OperationCanceled => "操作已取消",
            CoreWebView2WebErrorStatus.RedirectFailed => "重定向失败",
            CoreWebView2WebErrorStatus.UnexpectedError => "发生意外错误",
            CoreWebView2WebErrorStatus.ValidAuthenticationCredentialsRequired => "需要身份验证",
            CoreWebView2WebErrorStatus.ValidProxyAuthenticationRequired => "需要代理身份验证",
            _ when code == 7 => "连接超时",
            _ when code == 13 => "网络连接已断开",
            _ when code == 14 => "SSL 证书错误",
            _ when code == 15 => "连接已关闭",
            _ => $"网络错误 ({code})"
        };
    }

    private static string GetHttpErrorMessage(int httpStatusCode) => httpStatusCode switch { 400 => "请求无效", 401 => "未授权访问", 403 => "禁止访问", 404 => "页面不存在", 405 => "方法不允许", 408 => "请求超时", 409 => "请求冲突", 410 => "资源已删除", 413 => "请求实体过大", 414 => "请求URI过长", 429 => "请求过于频繁", 500 => "服务器内部错误", 501 => "功能未实现", 502 => "网关错误", 503 => "服务不可用", 504 => "网关超时", _ when httpStatusCode >= 400 && httpStatusCode < 500 => $"客户端错误 ({httpStatusCode})", _ when httpStatusCode >= 500 => $"服务器错误 ({httpStatusCode})", _ => $"HTTP错误 ({httpStatusCode})" };

    private void ShowErrorPage(string errorMessage, string url)
    {
        Logger.LogInfo(LogCategory, $"ShowErrorPage called: message={errorMessage}, url={url}");
        if (_coreWebView2 == null) { Logger.LogInfo(LogCategory, "ShowErrorPage ABORTED: _coreWebView2 is null"); return; }
        _isShowingErrorPage = true;
        var safeMessage = System.Net.WebUtility.HtmlEncode(errorMessage);
        var safeUrl = System.Net.WebUtility.HtmlEncode(url);
        Logger.LogInfo(LogCategory, $"ShowErrorPage: preparing HTML, safeMessage={safeMessage}");
        var errorHtml = $"<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>加载失败</title><style>:root {{--bg: {AppConstants.WebView2Theme.LightBackgroundColorHex};--fg: #000000;--muted: #666666;--accent: #0078D4;--card: #F5F5F5;--border: #E0E0E0;}}*{{margin:0;padding:0;box-sizing:border-box;}}body{{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--bg);min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px;color:var(--fg);}}.container{{max-width:420px;width:100%;}}.brand{{font-size:13px;font-weight:600;letter-spacing:0.08em;text-transform:uppercase;color:var(--muted);margin-bottom:48px;}}.error-code{{font-family:'SF Mono','Fira Code',monospace;font-size:72px;font-weight:200;color:var(--accent);line-height:1;margin-bottom:8px;}}.error-title{{font-size:20px;font-weight:600;color:var(--fg);margin-bottom:12px;}}.error-message{{font-size:15px;color:var(--muted);line-height:1.6;margin-bottom:24px;}}.url-box{{background:var(--card);border:1px solid var(--border);border-radius:6px;padding:12px 16px;margin-bottom:32px;font-size:13px;color:var(--muted);word-break:break-all;font-family:'SF Mono','Fira Code',monospace;}}.help-text{{font-size:14px;color:var(--muted);text-align:center;line-height:1.8;}}.help-text .highlight{{color:var(--fg);font-weight:500;}}</style></head><body><div class=\"container\"><div class=\"brand\">NekoT</div><div class=\"error-code\">://</div><div class=\"error-title\">无法访问此页面</div><div class=\"error-message\">{safeMessage}</div><div class=\"url-box\">{safeUrl}</div><div class=\"help-text\">点击左上角的 <span class=\"highlight\">刷新按钮</span> 重新加载</div></div></body></html>";
        _coreWebView2.WebMessageReceived -= OnWebMessageReceived;
        _coreWebView2.WebMessageReceived += OnWebMessageReceived;
        _coreWebView2.NavigateToString(errorHtml);
        Logger.LogInfo(LogCategory, "ShowErrorPage: NavigateToString called successfully");
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) { var message = e.TryGetWebMessageAsString(); Logger.LogInfo(LogCategory, $"WebMessage received: {message}"); switch (message) { case "reload": if (!string.IsNullOrEmpty(_lastNavigatedUrl)) { Navigate(_lastNavigatedUrl); } break; case "home": Navigate("about:blank"); break; } }

    public void GoBack() => _coreWebView2?.GoBack();
    public void GoForward() => _coreWebView2?.GoForward();
    public void Reload() => _coreWebView2?.Reload();
    public void Stop() => _coreWebView2?.Stop();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    [DllImport("user32.dll")] private static extern bool UpdateWindow(IntPtr hWnd);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint crColor);
    private const int SW_SHOW = 5;
    private const int GCLP_HBRBACKGROUND = -10;
    private const int WM_ERASEBKGND = 0x0014;
    [DllImport("user32.dll")] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    private class PlatformHandle : IPlatformHandle { public IntPtr Handle { get; } public string HandleDescriptor { get; } public PlatformHandle(IntPtr handle, string descriptor) { Handle = handle; HandleDescriptor = descriptor; } }
}