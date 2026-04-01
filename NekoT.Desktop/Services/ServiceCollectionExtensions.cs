using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NekoT.Core.Billing;
using NekoT.Core.Browser;
using NekoT.Core.Contracts;
using NekoT.Core.Forwarding;
using NekoT.Core.Http;
using NekoT.Core.LlmProviders;
using NekoT.Core.Security;
using NekoT.Core.TokenManagement;
using NekoT.Core.Versioning;
using NekoT.Desktop.Services.Settings;
using NekoT.Desktop.Update;
using NekoT.Desktop.ViewModels;
using NekoT.Desktop.Views;

namespace NekoT.Desktop.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNekoTServices(this IServiceCollection services)
    {
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ILlmProviderManager>(sp => LlmProviderManager.Instance);
        services.AddSingleton<ISecureStorage, SecureStorage>();
        services.AddSingleton<CostEstimator>();
        services.AddSingleton<HttpClient>(sp => HttpClientManager.GetSharedClient());
        services.AddSingleton<BrowserTabManager>();
        services.AddSingleton<TabNavigationService>();
        services.AddTransient<ForwardingService>();
        services.AddTransient<ChatForwardingService>();
        services.AddTransient<LocalProxyService>();
        
        services.AddSingleton<IVersionService, SquirrelUpdateService>();
        services.AddSingleton<ForceUpdateChecker>();
        services.AddSingleton<UpdateCheckScheduler>();

        services.AddSingleton<ISettingsValidator, SettingsValidator>();
        services.AddSingleton<IAuditLogger, AuditLogger>();

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<SidePanelViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<BrowserTabViewModel>();
        services.AddTransient<TabItemViewModel>();

        return services;
    }

    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<ComplianceDialog>();

        return services;
    }
}
