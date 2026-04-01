using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NekoT.Desktop.Views;

public partial class CloseConfirmationDialog : Window
{
    public CloseConfirmationResult Result { get; private set; } = CloseConfirmationResult.Cancel;
    public int ActiveTaskCount { get; set; }

    public CloseConfirmationDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        this.FindControl<TextBlock>("ActiveTaskCountText")!.Text = ActiveTaskCount.ToString();
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        Result = CloseConfirmationResult.Confirm;
        Close();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Result = CloseConfirmationResult.Cancel;
        Close();
    }

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e)
    {
        Result = CloseConfirmationResult.MinimizeToTray;
        Close();
    }
}

public enum CloseConfirmationResult
{
    Cancel,
    Confirm,
    MinimizeToTray
}