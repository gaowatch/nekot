using Microsoft.Extensions.DependencyInjection;
using NekoT.Desktop.Services.WebView2;

namespace NekoT.Desktop.Services;

public static class WebView2ServiceExtensions
{
    public static IServiceCollection AddWebView2Services(this IServiceCollection services)
    {
        services.AddSingleton<IWebView2InitializationService, WebView2InitializationService>();
        services.AddTransient<StealthScriptBuilder>();
        return services;
    }
}