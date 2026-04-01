using System;
using Microsoft.Extensions.DependencyInjection;
using NekoT.Core.Contracts;
using NekoT.Core.Pricing;
using NekoT.Core.Security;
using NekoT.Core.Versioning;
using NekoT.Desktop.Services;
using NekoT.Desktop.ViewModels;
using NekoT.Desktop.Views;

namespace NekoT.Desktop;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNekoTServices(this IServiceCollection services)
    {
        services.AddSingleton<SecureStorage>();
        services.AddSingleton<SecureKeyManager>();
        services.AddSingleton<IChatHistoryStorage, ChatHistoryStorage>();
        services.AddSingleton<IPricingStorage, PricingStorage>();
        services.AddSingleton<UserSettingsService>(sp => UserSettingsService.Instance);
        services.AddSingleton<LanguageService>(sp => LanguageService.Instance);
        services.AddSingleton<GlobalExceptionHandler>(sp => GlobalExceptionHandler.Instance);
        services.AddSingleton<IVersionService, SquirrelUpdateService>();
        services.AddSingleton<ForceUpdateChecker>();
        services.AddSingleton<UpdateCheckScheduler>();
        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SidePanelViewModel>();
        services.AddTransient<BrowserTabViewModel>();
        return services;
    }

    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<HomeView>();
        services.AddTransient<ChatTabView>();
        services.AddTransient<BrowserTabView>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<GuideWindow>();
        services.AddTransient<ErrorDialog>();
        return services;
    }
}