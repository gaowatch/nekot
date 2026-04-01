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

    private GlobalExceptionHandler()
    {
    }

    public void InitializeEarly()
    {
        TaskScheduler.UnobservedTaskException += OnTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        LoggerService.Instance.LogInfo("ExceptionHandler", "Early exception handlers initialized (TaskScheduler, AppDomain)");
    }

    public void InitializeUIThread()
    {
        if (Dispatcher.UIThread != null)
        {
            Dispatcher.UIThread.UnhandledException += OnUIThreadException;
            LoggerService.Instance.LogInfo("ExceptionHandler", "UI thread exception handler initialized");
        }
        else
        {
            LoggerService.Instance.LogInfo("ExceptionHandler", "WARNING: Dispatcher.UIThread not available yet, UI thread exception handler not registered");
        }
    }

    public void Initialize()
    {
        InitializeEarly();
        InitializeUIThread();
    }

    private void OnUIThreadException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggerService.Instance.LogError("UI", "UI thread exception", e.Exception);

        if (IsNonCriticalException(e.Exception))
        {
            ShowErrorDialog(Res.Exception_OperationFailed, GetUserFriendlyMessage(e.Exception),
                e.Exception, ErrorSeverity.Error);
            e.Handled = true;
        }
        else
        {
            ShowErrorDialog(Res.Exception_AppError, Res.Exception_AppMayNeedRestart,
                e.Exception, ErrorSeverity.Critical);
            e.Handled = true;
        }
    }

    private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggerService.Instance.LogError("Task", "Unobserved task exception", e.Exception);
        Dispatcher.UIThread.Post(() =>
        {
            ShowErrorDialog(Res.Exception_BackgroundTaskError, GetUserFriendlyMessage(e.Exception),
                e.Exception, ErrorSeverity.Warning);
        });
        e.SetObserved();
    }

    private void OnDomainException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LoggerService.Instance.LogError("Domain", "Unhandled exception", exception);

        if (e.IsTerminating)
        {
            LoggerService.Instance.LogError("Domain", "Application is terminating", exception);
            try { CleanupForwardingService(); } catch { }
            try { SaveCrashLog(exception); } catch { }
            Task.Delay(100).ContinueWith(_ => Environment.Exit(1));
        }
    }

    private void CleanupForwardingService()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                var forwardingService = mainViewModel.ForwardingService;
                if (forwardingService != null)
                {
                    LoggerService.Instance.LogInfo("ExceptionHandler", "Cleaning up ForwardingService due to crash...");
                    forwardingService.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.Instance.LogError("ExceptionHandler", "Failed to cleanup ForwardingService", ex);
        }
    }

    private void SaveCrashLog(Exception? exception)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NekoT", "crash.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (logDir != null && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            var crashInfo = new StringBuilder();
            crashInfo.AppendLine($"Crash Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            crashInfo.AppendLine($"Exception: {exception?.GetType().FullName}");
            crashInfo.AppendLine($"Message: {exception?.Message}");
            crashInfo.AppendLine($"Stack Trace:");
            crashInfo.AppendLine(exception?.StackTrace);
            File.AppendAllText(logPath, crashInfo.ToString(), System.Text.Encoding.UTF8);
        }
        catch { }
    }

    private void ShowErrorDialog(string title, string message, Exception? exception, ErrorSeverity severity)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                ErrorDialog.ShowErrorNonModal(desktop.MainWindow, title, message, exception, severity);
            }
        }
        catch (Exception ex)
        {
            LoggerService.Instance.LogError("ExceptionHandler", "Failed to show error dialog", ex);
        }
    }

    private bool IsNonCriticalException(Exception? exception)
    {
        return exception switch
        {
            ArgumentNullException => false,
            OutOfMemoryException => false,
            AccessViolationException => false,
            _ => true
        };
    }

    private string GetUserFriendlyMessage(Exception? exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => Res.Exception_AccessDenied,
            System.Net.Http.HttpRequestException => Res.Network_CannotConnect,
            TimeoutException => Res.Network_ConnectionTimeout,
            System.IO.IOException => string.Format(Res.Exception_IOException, exception.Message),
            ArgumentException ex => $"{Res.Common_Error}: {ex.Message}",
            InvalidOperationException ex => $"{Res.Exception_OperationFailed}: {ex.Message}",
            _ => exception?.Message ?? Res.Exception_AppMayNeedRestart
        };
    }
}