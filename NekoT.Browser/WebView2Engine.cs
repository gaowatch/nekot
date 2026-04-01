using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using NekoT.Core.Browser;

namespace NekoT.Browser;

public class WebView2Engine : IBrowserEngine
{
    private readonly WebView2 _webView;

    public WebView2Engine(WebView2 webView)
    {
        _webView = webView;
    }

    public string CurrentUrl => _webView.Source?.ToString() ?? string.Empty;
    public bool CanGoBack => _webView.CanGoBack;
    public bool CanGoForward => _webView.CanGoForward;

    public async Task<bool> NavigateAsync(string url)
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.Navigate(url);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task GoBackAsync()
    {
        if (_webView.CanGoBack)
            _webView.GoBack();
        return Task.CompletedTask;
    }

    public Task GoForwardAsync()
    {
        if (_webView.CanGoForward)
            _webView.GoForward();
        return Task.CompletedTask;
    }

    public Task ReloadAsync()
    {
        _webView.Reload();
        return Task.CompletedTask;
    }
}