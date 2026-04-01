using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public partial class ComplianceDialog : Window
{
    public ComplianceDialog()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
    }

    private void OnAgree(object? sender, RoutedEventArgs e)
    {
        if (AgreeCheckBox.IsChecked == true)
        {
            UserSettingsService.Instance.HasAcceptedDisclaimer = true;
            Close(true);
        }
    }

    private void OnShowFullCompliance(object? sender, RoutedEventArgs e)
    {
        var complianceWindow = new ComplianceInfoWindow();
        complianceWindow.Show();
    }
}