using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public partial class GuideWindow : Window
{
    public GuideWindow()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        if (DontShowAgainCheckBox.IsChecked == true)
        {
            UserSettingsService.Instance.HasCompletedOnboarding = true;
        }
        Close();
    }
}