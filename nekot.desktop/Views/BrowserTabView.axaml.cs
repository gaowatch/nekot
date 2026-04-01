using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using NekoT.Desktop.Controls;
using NekoT.Desktop.ViewModels;
using System.IO;
using Avalonia;

namespace NekoT.Desktop.Views;

public partial class BrowserTabView : UserControl
{
    private static readonly string LogFile = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "wv2_debug.log");
    private static readonly object LogLock = new object();

    public BrowserTabViewModel? ViewModel => DataContext as BrowserTabViewModel;

    private bool _isInitialized;
    private WebView2Control? _attachedWebView;
    private bool _isWebViewReady;

    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try
        {
            lock (LogLock)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine, System.Text.Encoding.UTF8);
            }
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogError] IO: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogError] Access: {ex.Message}");
        }
        System.Diagnostics.Debug.WriteLine(msg);
    }

    public BrowserTabView()
    {
        Log("[BrowserTabView] Constructor START");
        try
        {
            InitializeComponent();
            Log("[BrowserTabView] XAML loaded successfully");
        }
        catch (Exception ex)
        {
            Log($"[BrowserTabView] XAML load FAILED: {ex.Message}");
            Log($"[BrowserTabView] Stack: {ex.StackTrace}");
            throw;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Log($"[BrowserTabView] OnAttachedToVisualTree, DataContext={DataContext?.GetType().Name}, _isInitialized={_isInitialized}");

        if (_isInitialized && _attachedWebView != null && _isWebViewReady)
        {
            Log($"[BrowserTabView] Already initialized and ready, skipping full setup");
            return;
        }

        if (_isInitialized && _attachedWebView != null && !_isWebViewReady)
        {
            Log($"[BrowserTabView] WebView not ready, resetting state");
            _isInitialized = false;
            _attachedWebView = null;
        }

        WebView = this.FindControl<WebView2Control>("WebView");
        Log($"[BrowserTabView] FindControl result: WebView={WebView}");

        if (DataContext is BrowserTabViewModel viewModel && WebView != null)
        {
            Log($"[BrowserTabView] Setting WebView control to ViewModel");
            viewModel.SetWebViewControl(WebView);
            
            WebView.Ready += OnWebViewReady;
            WebView.InitializationFailed += OnWebViewInitializationFailed;
            
            _isInitialized = true;
            _attachedWebView = WebView;
        }
        else
        {
            Log($"[BrowserTabView] DataContext is NOT BrowserTabViewModel or WebView is null!");
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Log($"[BrowserTabView] OnDetachedFromVisualTree, _attachedWebView={_attachedWebView != null}");

        if (_attachedWebView != null)
        {
            try
            {
                _attachedWebView.Ready -= OnWebViewReady;
                _attachedWebView.InitializationFailed -= OnWebViewInitializationFailed;
            }
            catch (Exception ex)
            {
                Log($"[BrowserTabView] Error unsubscribing events: {ex.Message}");
            }
            
            _attachedWebView = null;
        }
        
        _isInitialized = false;
        _isWebViewReady = false;
    }

    private void OnWebViewReady(object? sender, EventArgs e)
    {
        Log("[BrowserTabView] WebView Ready");
        _isWebViewReady = true;
    }

    private void OnWebViewInitializationFailed(object? sender, string reason)
    {
        Log($"[BrowserTabView] WebView initialization failed: {reason}");
        _isWebViewReady = false;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        Log($"[BrowserTabView] OnDataContextChanged, DataContext={DataContext?.GetType().Name}");

        WebView = this.FindControl<WebView2Control>("WebView") ?? WebView;
        
        if (DataContext is BrowserTabViewModel viewModel && WebView != null)
        {
            Log($"[BrowserTabView] Setting WebView control to ViewModel (DataContextChanged)");
            viewModel.SetWebViewControl(WebView);
        }
    }

    private void OnUrlKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is BrowserTabViewModel viewModel)
        {
            viewModel.Navigate();
            e.Handled = true;
        }
    }

    private void OnNavigateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is BrowserTabViewModel viewModel)
        {
            viewModel.Navigate();
        }
    }
}