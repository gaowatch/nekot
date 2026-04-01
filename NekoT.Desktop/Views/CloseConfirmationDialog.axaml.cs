using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public enum CloseConfirmationResult { Cancel, Exit }

public partial class CloseConfirmationDialog : Window
{
    public CloseConfirmationResult Result { get; private set; } = CloseConfirmationResult.Cancel;
    public int ActiveTaskCount { get; set; }

    public CloseConfirmationDialog()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
        UpdateTaskCountText();
    }

    private void UpdateTaskCountText()
    {
        if (ActiveTaskCount > 0)
        {
            TaskCountText.Text = $"{Strings.CloseConfirm_TaskCount}: {ActiveTaskCount}";
            TaskCountText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF9800"));
        }
        else
        {
            TaskCountText.Text = Strings.CloseConfirm_TaskCountNone;
            TaskCountText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4CAF50"));
        }
    }

    public static CloseConfirmationResult Show(Window owner, int activeTaskCount = 0)
    {
        var dialog = new CloseConfirmationDialog { ActiveTaskCount = activeTaskCount };
        dialog.ShowDialog(owner);
        return dialog.Result;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) { Result = CloseConfirmationResult.Cancel; Close(); }
    private void OnExitClick(object? sender, RoutedEventArgs e) { Result = CloseConfirmationResult.Exit; Close(); }
}