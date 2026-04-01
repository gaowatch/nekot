using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace NekoT.Desktop.Services.WebView2;

public enum WebView2InitializationState { NotCreated, Creating, Ready, Failed }

public interface IWebView2InitializationService
{
    WebView2InitializationState State { get; }
    bool IsInitialized { get; }
    bool IsInitializationSuccessful { get; }
    string? InitializationError { get; }
    event EventHandler<WebView2InitializationState>? StateChanged;
    Task<CoreWebView2Environment?> InitializeAsync(CancellationToken cancellationToken = default);
    CoreWebView2Environment? GetEnvironment();
    void Reset();
}