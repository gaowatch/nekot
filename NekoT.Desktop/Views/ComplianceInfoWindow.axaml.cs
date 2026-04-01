using Avalonia.Controls;
using Avalonia.Interactivity;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public partial class ComplianceInfoWindow : Window
{
    public ComplianceInfoWindow()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
    }

    private void OnClose(object? sender, RoutedEventArgs e) { Close(); }
}