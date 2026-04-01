using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NekoT.Core.Configuration;
using NekoT.Core.Contracts;
using NekoT.Core.Versioning;
using NekoT.Desktop.Services;
using NekoT.Desktop.Update;
using NekoT.Desktop.Views;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    public override void Initialize()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        var envVarValue = Environment.GetEnvironmentVariable(AppConstants.WebView2Theme.DefaultEnvironmentVariableName);
        if (string.IsNullOrEmpty(envVarValue))
        {
            var configColor = Configuration["WebView2:BackgroundColor"];
            if (!string.IsNullOrEmpty(configColor))
            {
                var argbColor = ConvertHexToArgb(configColor);
                Environment.SetEnvironmentVariable(
                    AppConstants.WebView2Theme.DefaultEnvironmentVariableName, 
                    argbColor);
            }
            else
            {
                Environment.SetEnvironmentVariable(
                    AppConstants.WebView2Theme.DefaultEnvironmentVariableName, 
                    AppConstants.WebView2Theme.LightBackgroundColorArgb);
            }
        }
        
        AvaloniaXamlLoader.Load(this);
        
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddNekoTServices();
        services.AddViewModels();
        services.AddViews();
        Services = services.BuildServiceProvider();
    }

    private static string ConvertHexToArgb(string hexColor)
    {
        try
        {
            hexColor = hexColor.Trim();
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.Substring(1);
            }

            if (hexColor.Length == 6)
            {
                return "0xFF" + hexColor.ToUpperInvariant();
            }
            else if (hexColor.Length == 8)
            {
                return "0x" + hexColor.ToUpperInvariant();
            }
        }
        catch
        {
        }
        
        return AppConstants.WebView2Theme.LightBackgroundColorArgb;
    }

    private const bool FORCE_SHOW_DISCLAIMER_ON_EVERY_START = false;

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                NekoT.Desktop.Services.GlobalExceptionHandler.Instance.InitializeUIThread();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Failed to initialize UI thread exception handler: {ex.Message}");
            }
            
            desktop.Exit += OnApplicationExit;

            var forceUpdateRequired = await CheckForceUpdateAsync();
            
            if (forceUpdateRequired)
            {
                return;
            }

            bool shouldShowDisclaimer = FORCE_SHOW_DISCLAIMER_ON_EVERY_START 
                || !UserSettingsService.Instance.HasAcceptedDisclaimer;

            if (shouldShowDisclaimer)
            {
                var disclaimerDialog = new ComplianceDialog();
                disclaimerDialog.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen;
                desktop.MainWindow = disclaimerDialog;
                
                disclaimerDialog.Closed += (s, e) =>
                {
                    if (UserSettingsService.Instance.HasAcceptedDisclaimer)
                    {
                        var mainWindow = Services.GetRequiredService<MainWindow>();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                        
                        StartUpdateChecker();
                    }
                    else
                    {
                        desktop.Shutdown();
                    }
                };
                
                disclaimerDialog.Show();
            }
            else
            {
                var mainWindow = Services.GetRequiredService<MainWindow>();
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                StartUpdateChecker();
            }
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private async Task<bool> CheckForceUpdateAsync()
    {
        try
        {
            var forceUpdateChecker = Services.GetRequiredService<ForceUpdateChecker>();
            var versionService = Services.GetRequiredService<IVersionService>();
            
            var currentVersion = versionService.GetCurrentVersion();
            var result = await forceUpdateChecker.CheckForceUpdateAsync(currentVersion);
            
            if (result.HasUpdate && result.IsForceUpdate)
            {
                var forceUpdateDialog = new ForceUpdateDialog();
                forceUpdateDialog.SetUpdateInfo(result);
                
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = forceUpdateDialog;
                }
                
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] 强制更新检查失败: {ex.Message}");
        }
        
        return false;
    }

    private void StartUpdateChecker()
    {
        try
        {
            var updateScheduler = Services.GetRequiredService<UpdateCheckScheduler>();
            updateScheduler.UpdateAvailable += OnUpdateAvailable;
            updateScheduler.StartPeriodicCheck();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] 启动更新检查器失败: {ex.Message}");
        }
    }

    private void OnUpdateAvailable(object? sender, Models.Versioning.UpdateCheckResult e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var updateDialog = new UpdateAvailableDialog();
            updateDialog.SetUpdateInfo(e);
            updateDialog.Show();
        });
    }

    private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    var forwardingService = mainViewModel.ForwardingService;
                    if (forwardingService != null && !forwardingService.IsDisposed)
                    {
                        forwardingService.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Error during cleanup: {ex.Message}");
        }
    }
}