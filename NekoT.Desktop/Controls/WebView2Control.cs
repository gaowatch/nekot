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
}