using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web;
using System.Windows.Input;
using NekoT.Desktop.Controls;
using NekoT.Desktop.NetworkMonitoring;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Services.Logging;

namespace NekoT.Desktop.ViewModels;

public class BrowserTabViewModel : ViewModelBase, IDisposable
{
    private static readonly ILoggerService Logger = LoggerService.Instance;
    private const string LogCategory = "BrowserTab";
    private WebView2Control? _webView;
    private WebView2NetworkMonitor? _monitor;
    private string _url = "about:blank";
    private string _title = Strings.Tab_NewTab;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _isLoading;
    private bool _isSecure;
    private string _domainName = "";
    private bool _disposed;
    private string? _pendingUrl;

    public event EventHandler<TokenExtractedEventArgs>? TokenDetected;
    public event EventHandler<TrafficStatsEventArgs>? TrafficDetected;
    public event EventHandler? CloseRequested;

    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CloseTabCommand { get; }

    public BrowserTabViewModel()
    {
        GoBackCommand = new RelayCommand(_ => GoBack(), _ => CanGoBack);
        GoForwardCommand = new RelayCommand(_ => GoForward(), _ => CanGoForward);
        RefreshCommand = new RelayCommand(_ => Refresh());
        CloseTabCommand = new RelayCommand(_ => CloseTab());
    }

    public string Url
    {
        get => _url;
        set
        {
            if (SetField(ref _url, value))
            {
                UpdateDomainInfo();
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public bool CanGoBack
    {
        get => _canGoBack;
        private set => SetField(ref _canGoBack, value);
    }

    public bool CanGoForward
    {
        get => _canGoForward;
        private set => SetField(ref _canGoForward, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public bool IsSecure
    {
        get => _isSecure;
        private set => SetField(ref _isSecure, value);
    }

    public string DomainName
    {
        get => _domainName;
        private set => SetField(ref _domainName, value);
    }

    public WebView2NetworkMonitor? Monitor => _monitor;

    public void SetWebViewControl(WebView2Control webView)
    {
        Logger.LogInfo(LogCategory, $"BrowserTab: SetWebViewControl called, webView={webView != null}, _webView={_webView != null}");
        
        if (ReferenceEquals(_webView, webView))
        {
            Logger.LogInfo(LogCategory, $"BrowserTab: Same WebView instance already set, skipping");
            return;
        }

        var oldWebView = _webView;
        var oldMonitor = _monitor;
        
        _webView = null;
        _monitor = null;
        
        if (oldMonitor != null)
        {
            try
            {
                oldMonitor.TokenExtracted -= OnTokenExtracted;
                oldMonitor.StopMonitoring();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserTab] Error stopping old monitor: {ex.Message}");
            }
        }
        
        if (oldWebView != null)
        {
            try
            {
                oldWebView.NavigationCompleted -= OnNavigationCompleted;
                oldWebView.SourceChanged -= OnSourceChanged;
                oldWebView.Ready -= OnReady;
                oldWebView.DocumentTitleChanged -= OnDocumentTitleChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserTab] Error unsubscribing old WebView: {ex.Message}");
            }
        }

        _webView = webView;

        if (_webView != null)
        {
            _webView.NavigationCompleted += OnNavigationCompleted;
            _webView.SourceChanged += OnSourceChanged;
            _webView.Ready += OnReady;
            _webView.DocumentTitleChanged += OnDocumentTitleChanged;

            if (_webView.IsReady)
            {
                StartMonitoring();
                var targetUrl = _pendingUrl ?? Url;
                if (!string.IsNullOrEmpty(targetUrl) && targetUrl != "about:blank")
                {
                    _webView.Navigate(targetUrl);
                    if (_pendingUrl != null)
                    {
                        Url = _pendingUrl;
                        _pendingUrl = null;
                    }
                }
            }
        }
    }

    private void OnReady(object? sender, EventArgs e)
    {
        StartMonitoring();
        var targetUrl = _pendingUrl ?? Url;
        if (_webView != null && !string.IsNullOrEmpty(targetUrl) && targetUrl != "about:blank")
        {
            _webView.Navigate(targetUrl);
            if (_pendingUrl != null)
            {
                Url = _pendingUrl;
                _pendingUrl = null;
            }
        }
    }

    private void StartMonitoring()
    {
        Logger.LogInfo(LogCategory, $"BrowserTab: StartMonitoring called, _webView={_webView != null}, CoreWebView2={_webView?.CoreWebView2 != null}");
        
        if (_webView?.CoreWebView2 == null)
        {
            Logger.LogInfo(LogCategory, "BrowserTab: StartMonitoring ABORTED: CoreWebView2 is null");
            return;
        }

        _monitor = new WebView2NetworkMonitor();
        _monitor.TokenExtracted += OnTokenExtracted;
        _monitor.TrafficUpdated += OnTrafficUpdated;

        try
        {
            _monitor.StartMonitoring(_webView.CoreWebView2);
            Logger.LogInfo(LogCategory, "BrowserTab: CDP monitoring started successfully");
        }
        catch (Exception ex)
        {
            Logger.LogInfo(LogCategory, $"BrowserTab: Failed to start monitoring: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void OnTokenExtracted(object? sender, TokenExtractedEventArgs e)
    {
        TokenDetected?.Invoke(this, e);
    }
    
    private void OnTrafficUpdated(object? sender, TrafficStatsEventArgs e)
    {
        TrafficDetected?.Invoke(this, e);
    }
    
    private void OnNavigationCompleted(object? sender, EventArgs e)
    {
        Logger.LogInfo(LogCategory, $"BrowserTab: OnNavigationCompleted, URL={_webView?.Source}");
        IsLoading = false;
        UpdateNavigationState();
    }

    private void OnSourceChanged(object? sender, EventArgs e)
    {
        if (_webView != null)
        {
            Url = _webView.Source ?? string.Empty;
        }
        UpdateNavigationState();
    }

    private void OnDocumentTitleChanged(object? sender, string title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            Title = title;
        }
    }

    private void UpdateNavigationState()
    {
        if (_webView != null)
        {
            CanGoBack = _webView.CanGoBack;
            CanGoForward = _webView.CanGoForward;
        }
    }

    private void UpdateDomainInfo()
    {
        try
        {
            if (Uri.TryCreate(_url, UriKind.Absolute, out var uri))
            {
                IsSecure = uri.Scheme == "https";
                DomainName = uri.Host;
            }
            else
            {
                IsSecure = false;
                DomainName = "";
            }
        }
        catch
        {
            IsSecure = false;
            DomainName = "";
        }
    }

    public void Navigate()
    {
        if (_webView != null && !string.IsNullOrWhiteSpace(Url))
        {
            var url = Url.Trim();
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
                Url = url;
            }
            _webView.Navigate(url);
        }
    }

    public void GoBack()
    {
        _webView?.GoBack();
    }

    public void GoForward()
    {
        _webView?.GoForward();
    }

    public void Refresh()
    {
        _webView?.Reload();
    }

    public void CloseTab()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void NavigateTo(string url)
    {
        Url = url;
        
        if (_webView != null && _webView.IsReady)
        {
            Navigate();
        }
        else
        {
            _pendingUrl = url;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_monitor != null)
        {
            _monitor.TokenExtracted -= OnTokenExtracted;
            _monitor.TrafficUpdated -= OnTrafficUpdated;
            _monitor.StopMonitoring();
            _monitor.Dispose();
            _monitor = null;
        }

        if (_webView != null)
        {
            _webView.NavigationCompleted -= OnNavigationCompleted;
            _webView.SourceChanged -= OnSourceChanged;
            _webView.Ready -= OnReady;
            _webView.DocumentTitleChanged -= OnDocumentTitleChanged;
            _webView = null;
        }

        _disposed = true;
    }
}