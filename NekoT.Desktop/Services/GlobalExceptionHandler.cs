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

    public void Initialize()
    {
        InitializeEarly();
        InitializeUIThread();
    }

    private void OnUIThreadException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggerService.Instance.LogError("UI", "UI thread exception", e.Exception);
        ShowErrorDialog(Res.Exception_OperationFailed, GetUserFriendlyMessage(e.Exception), e.Exception, ErrorSeverity.Error);
        e.Handled = true;
    }

    private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggerService.Instance.LogError("Task", "Unobserved task exception", e.Exception);
        e.SetObserved();
    }

    private void OnDomainException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LoggerService.Instance.LogError("Domain", "Unhandled exception", exception);
    }

    private void ShowErrorDialog(string title, string message, Exception? ex, ErrorSeverity severity) { }
    private string GetUserFriendlyMessage(Exception ex) => ex.Message;
}
