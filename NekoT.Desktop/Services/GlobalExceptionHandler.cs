using System;
using NekoT.Desktop.Services.Logging;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using NekoT.Desktop.Views;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.ViewModels;
using Res = NekoT.Desktop.Resources.Strings;

namespace NekoT.Desktop.Services;

public class GlobalExceptionHandler
{
    private static GlobalExceptionHandler? _instance;
    public static GlobalExceptionHandler Instance => _instance ??= new GlobalExceptionHandler();
    private GlobalExceptionHandler() { }

    public void InitializeEarly()
    {
        TaskScheduler.UnobservedTaskException += OnTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        LoggerService.Instance.LogInfo("ExceptionHandler", "Early exception handlers initialized");
    }

    public void InitializeUIThread()
    {
        if (Dispatcher.UIThread != null)
        {
            Dispatcher.UIThread.UnhandledException += OnUIThreadException;
            LoggerService.Instance.LogInfo("ExceptionHandler", "UI thread exception handler initialized");
        }
    }

    public void Initialize() { InitializeEarly(); InitializeUIThread(); }

    private void OnUIThreadException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggerService.Instance.LogError("UI", "UI thread exception", e.Exception);
        ShowErrorDialog(Res.Common_Error, e.Exception?.Message ?? "Unknown error", e.Exception, ErrorSeverity.Error);
        e.Handled = true;
    }

    private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggerService.Instance.LogError("Task", "Unobserved task exception", e.Exception);
        Dispatcher.UIThread.Post(() => ShowErrorDialog(Res.Common_Error, e.Exception?.Message ?? "Unknown error", e.Exception, ErrorSeverity.Warning));
        e.SetObserved();
    }

    private void OnDomainException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LoggerService.Instance.LogError("Domain", "Unhandled exception", exception);
        if (e.IsTerminating)
        {
            try { CleanupForwardingService(); } catch { }
            try { SaveCrashLog(exception); } catch { }
        }
    }

    private void CleanupForwardingService()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                var forwardingService = mainViewModel.ForwardingService;
                if (forwardingService != null) forwardingService.Dispose();
            }
        }
        catch { }
    }

    private void SaveCrashLog(Exception? exception)
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NekoT", "crash.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var crashInfo = new StringBuilder();
            crashInfo.AppendLine($"Crash Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            crashInfo.AppendLine($"Exception: {exception?.GetType().FullName}");
            crashInfo.AppendLine($"Message: {exception?.Message}");
            crashInfo.AppendLine($"Stack Trace: {exception?.StackTrace}");
            File.AppendAllText(logPath, crashInfo.ToString());
        }
        catch { }
    }

    private void ShowErrorDialog(string title, string message, Exception? exception, ErrorSeverity severity)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                ErrorDialog.ShowErrorNonModal(desktop.MainWindow, title, message, exception, severity);
        }
        catch { }
    }
}