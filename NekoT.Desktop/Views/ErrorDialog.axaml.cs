using System;
using NekoT.Desktop.Services.Logging;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.Views;

public enum ErrorSeverity { Info, Warning, Error, Critical }

public partial class ErrorDialog : Window
{
    private string _fullErrorDetails = "";

    public ErrorDialog()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
    }

    public static async void ShowError(Window owner, string title, string message, Exception? ex = null, ErrorSeverity severity = ErrorSeverity.Error)
    {
        var dialog = new ErrorDialog();
        dialog.SetupDialog(title, message, ex, severity);
        await dialog.ShowDialog(owner);
    }

    public static void ShowErrorNonModal(Window? owner, string title, string message, Exception? ex = null, ErrorSeverity severity = ErrorSeverity.Error)
    {
        var dialog = new ErrorDialog();
        dialog.SetupDialog(title, message, ex, severity);
        owner != null ? dialog.Show(owner) : dialog.Show();
    }

    private void SetupDialog(string title, string message, Exception? ex, ErrorSeverity severity)
    {
        ErrorTitle.Text = title;
        ErrorSummary.Text = message;
        SetupSeverityStyle(severity);
        var details = new StringBuilder();
        details.AppendLine($"{Strings.Error_Time}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        details.AppendLine($"{Strings.Error_Level}: {severity}");
        details.AppendLine($"{Strings.Error_Message}: {message}");
        if (ex != null)
        {
            details.AppendLine();
            details.AppendLine($"{Strings.Error_ExceptionType}: {ex.GetType().FullName}");
            details.AppendLine($"{Strings.Error_ExceptionMessage}: {ex.Message}");
            details.AppendLine($"{Strings.Error_StackTrace}:\n{ex.StackTrace}");
            if (ex.InnerException != null) details.AppendLine($"{Strings.Error_InnerException}: {ex.InnerException.Message}");
        }
        _fullErrorDetails = details.ToString();
        ErrorDetails.Text = _fullErrorDetails;
        LoggerService.Instance.LogError("ErrorDialog", $"{title}: {message}", ex);
    }

    private void SetupSeverityStyle(ErrorSeverity severity)
    {
        var color = severity switch { ErrorSeverity.Info => "#2196F3", ErrorSeverity.Warning => "#FF9800", ErrorSeverity.Error => "#F44336", ErrorSeverity.Critical => "#B71C1C", _ => "#F44336" };
        ErrorIcon.Foreground = new SolidColorBrush(Color.Parse(color));
        this.Title = severity switch { ErrorSeverity.Info => Strings.Error_Info, ErrorSeverity.Warning => Strings.Error_Warning, ErrorSeverity.Error => Strings.Error_Error, ErrorSeverity.Critical => Strings.Error_Critical, _ => Strings.Error_Error };
    }

    private void OnCopy(object? sender, RoutedEventArgs e)
    {
        try { Clipboard?.SetTextAsync(_fullErrorDetails); CopyButton.Content = Strings.Error_Copied; Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(2)); Avalonia.Threading.Dispatcher.UIThread.Post(() => { CopyButton.Content = Strings.Error_Copy; }); }); }
        catch (Exception ex) { LoggerService.Instance.LogError("ErrorDialog", "Copy failed", ex); }
    }

    private void OnClose(object? sender, RoutedEventArgs e) { Close(); }
}