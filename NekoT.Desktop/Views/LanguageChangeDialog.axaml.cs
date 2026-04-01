using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NekoT.Desktop.Constants;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.Views;

public partial class LanguageChangeDialog : Window
{
    private readonly string _newLanguage;

    public LanguageChangeDialog()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);
        _newLanguage = LanguageConstants.Chinese;
    }

    public LanguageChangeDialog(string newLanguage) : this()
    {
        _newLanguage = newLanguage;
    }

    private void OnRestartClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UserSettingsService.Instance.Language = _newLanguage;
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
