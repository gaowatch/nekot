using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace NekoT.Desktop.Services.WebView2;

public class WebView2IntegrationAdapter
{
    private readonly IWebView2InitializationService _initializationService;
    private readonly StealthScriptBuilder _scriptBuilder;

    public WebView2IntegrationAdapter(
        IWebView2InitializationService initializationService,
        StealthScriptBuilder scriptBuilder)
    {
        _initializationService = initializationService;
        _scriptBuilder = scriptBuilder;
    }

    public WebView2InitializationState State => _initializationService.State;
    public bool IsReady => _initializationService.IsInitializationSuccessful;
    public string? Error => _initializationService.InitializationError;

    public event EventHandler<WebView2InitializationState>? StateChanged
    {
        add => _initializationService.StateChanged += value;
        remove => _initializationService.StateChanged -= value;
    }

    public async Task<CoreWebView2Environment?> GetOrCreateEnvironmentAsync(
        CancellationToken cancellationToken = default)
    {
        return await _initializationService.InitializeAsync(cancellationToken);
    }

    public CoreWebView2Environment? GetEnvironment()
    {
        return _initializationService.GetEnvironment();
    }

    public string BuildStealthScript(bool blockTracking, bool blockAds)
    {
        _scriptBuilder.IncludeTrackingProtection = blockTracking;
        _scriptBuilder.IncludeAdBlocking = blockAds;
        return _scriptBuilder.Build();
    }

    public int GetEstimatedScriptSize(bool blockTracking, bool blockAds)
    {
        _scriptBuilder.IncludeTrackingProtection = blockTracking;
        _scriptBuilder.IncludeAdBlocking = blockAds;
        return _scriptBuilder.GetEstimatedSize();
    }

    public void Reset()
    {
        _initializationService.Reset();
    }
}