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
            var folders = Directory.GetDirectories(baseFolder, "WebView2Data_*");            var currentProcessId = Environment.ProcessId;
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                var pidStr = folderName.Replace("WebView2Data_", "");                if (int.TryParse(pidStr, out var pid) && pid != currentProcessId)
                {
                    try { var process = System.Diagnostics.Process.GetProcessById(pid); }
                    catch (ArgumentException)
                    {
                        try { Directory.Delete(folder, true); }
                        catch (Exception deleteEx) { System.Diagnostics.Debug.WriteLine($"[WebView2] Failed to delete orphan folder {folder}: {deleteEx.Message}"); }
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
    public bool IsReady => _isReady; set => _isReady = value; } // Ensure backward compatibility when merging from remote version to local version
    public CoreWebView2? CoreWebView2 => _coreWebView2; value; }