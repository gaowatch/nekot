using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using NekoT.Desktop.Constants;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.ViewModels;
using Res = NekoT.Desktop.Resources.Strings;

namespace NekoT.Desktop.Views;

public partial class SettingsWindow : Window
{
    private MainViewModel? _mainViewModel;
    private readonly Dictionary<string, Action<bool>> _panelVisibilityActions;
    private bool _languageChanged;

    public SettingsWindow()
    {
        InitializeComponent();
        WindowIconHelper.RemoveIcon(this);

        _panelVisibilityActions = new Dictionary<string, Action<bool>>
        {
            { "general", visible => GeneralPanel.IsVisible = visible },
            { "security", visible => SecurityPanel.IsVisible = visible },
            { "about", visible => AboutPanel.IsVisible = visible },
            { "donate", visible => DonatePanel.IsVisible = visible }
        };

        LoadSettings();
        UpdateSystemInfo();
    }

    public void SetMainViewModel(MainViewModel viewModel) { _mainViewModel = viewModel; }

    private void LoadSettings()
    {
        var settings = UserSettingsService.Instance;
        LanguageComboBox.SelectedIndex = settings.Language == "en" ? 1 : 0;
        StartWithWindows.IsChecked = settings.StartWithWindows;
        MinimizeToTray.IsChecked = settings.MinimizeToTray;
        StartMinimized.IsChecked = settings.StartMinimized;
        StartMaximized.IsChecked = settings.StartMaximized;
        PreferredPanelComboBox.SelectedIndex = settings.StartupPanel == "token-monitor" ? 1 : 0;
        HomePage.Text = settings.HomePage ?? "about:blank";
        ShowTokenMonitor.IsChecked = settings.ShowTokenMonitor;
        UserAgentTextBox.Text = settings.UserAgent ?? "";
        EnableStealthMode.IsChecked = settings.EnableStealthMode;
        DisableDevTools.IsChecked = settings.DisableDevTools;
        BlockTracking.IsChecked = settings.BlockTracking;
        BlockAds.IsChecked = settings.BlockAds;
        ProxySettings.Text = settings.ProxyUrl ?? "";
        UpdateVersionInfo();
    }

    private void SaveSettings()
    {
        var settings = UserSettingsService.Instance;
        settings.Language = LanguageComboBox.SelectedIndex == 1 ? "en" : "zh-CN";
        settings.StartWithWindows = StartWithWindows.IsChecked ?? false;
        settings.MinimizeToTray = MinimizeToTray.IsChecked ?? true;
        settings.StartMinimized = StartMinimized.IsChecked ?? false;
        settings.StartMaximized = StartMaximized.IsChecked ?? true;
        settings.StartupPanel = PreferredPanelComboBox.SelectedIndex == 1 ? "token-monitor" : "browser";
        settings.HomePage = HomePage.Text ?? "about:blank";
        settings.ShowTokenMonitor = ShowTokenMonitor.IsChecked ?? false;
        settings.UserAgent = UserAgentTextBox.Text;
        settings.EnableStealthMode = EnableStealthMode.IsChecked ?? true;
        settings.DisableDevTools = DisableDevTools.IsChecked ?? true;
        settings.BlockTracking = BlockTracking.IsChecked ?? true;
        settings.BlockAds = BlockAds.IsChecked ?? false;
        settings.ProxyUrl = ProxySettings.Text;
        SystemFeaturesHelper.ApplyStartupSettings();
    }

    private void OnNavClick(object? sender, RoutedEventArgs e) { if (sender is RadioButton rb && rb.Tag is string panel) SwitchPanel(panel); }
    private void SwitchPanel(string panel) { foreach (var (k, v) in _panelVisibilityActions) v(k == panel); }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_languageChanged) { UserSettingsService.Instance.Language = LanguageComboBox.SelectedIndex == 1 ? "en" : "zh-CN"; }
        SaveSettings();
        if (_languageChanged)
        {
            var result = await ShowConfirmAsync(Res.LanguageDialog_Title, Res.LanguageDialog_Message);
            if (result)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath)) { Process.Start(new ProcessStartInfo { FileName = exePath, Arguments = "--restart", UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exePath) }); Thread.Sleep(1000); }
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) { desktop.Shutdown(); return; }
            }
        }
        await ShowMessageAsync(Res.Common_Success, Res.Settings_Saved); Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) { Close(); }
    private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e) { if (LanguageComboBox?.SelectedIndex >= 0) _languageChanged = LanguageComboBox.SelectedIndex == 1 ? "en" != UserSettingsService.Instance.Language : "zh-CN" != UserSettingsService.Instance.Language; }

    private async void OnExportChatHistory(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = Res.Export_ChatHistory, SuggestedFileName = $"chat_history_{DateTime.Now:yyyyMMdd_HHmmss}.json", FileTypeChoices = new[] { new FilePickerFileType(Res.Export_JSON) { Patterns = new[] { "*.json" } } } });
        if (file != null) { await ExportChatHistoryAsync(file.Path.LocalPath); await ShowMessageAsync(Res.Common_Success, Res.Export_ChatExported); }
    }

    private async void OnExportTokenUsage(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = Res.Export_TokenUsage, SuggestedFileName = $"token_usage_{DateTime.Now:yyyyMMdd_HHmmss}.json", FileTypeChoices = new[] { new FilePickerFileType(Res.Export_JSON) { Patterns = new[] { "*.json" } } } });
        if (file != null) { await ExportTokenUsageAsync(file.Path.LocalPath); await ShowMessageAsync(Res.Common_Success, Res.Export_TokenExported); }
    }

    private async void OnImportData(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = Res.Import_Data, AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(Res.Export_JSON) { Patterns = new[] { "*.json" } } } });
        var file = files.FirstOrDefault();
        if (file != null && await ShowConfirmAsync(Res.Import_ConfirmTitle, Res.Import_ConfirmMessage)) { await ImportDataAsync(file.Path.LocalPath); LoadSettings(); await ShowMessageAsync(Res.Common_Success, Res.Import_Success); }
    }

    private async void OnClearAllData(object? sender, RoutedEventArgs e)
    { if (await ShowConfirmAsync(Res.Clear_ConfirmTitle, Res.Clear_ConfirmMessage)) { ClearAllData(); await ShowMessageAsync(Res.Common_Success, Res.Clear_Success); Close(); } }

    private void OnOpenGitHub(object? sender, RoutedEventArgs e) { Process.Start(new ProcessStartInfo { FileName = "https://github.com/nekot-ai/nekot", UseShellExecute = true }); }

    private async Task ExportChatHistoryAsync(string path) { var src = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT", "chat_history.json"); await File.WriteAllTextAsync(path, File.Exists(src) ? File.ReadAllText(src) : "[]"); }
    private async Task ExportTokenUsageAsync(string path) { var src = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT", "token_usage.json"); await File.WriteAllTextAsync(path, File.Exists(src) ? File.ReadAllText(src) : "[]"); }
    private async Task ImportDataAsync(string path) { var settings = JsonSerializer.Deserialize<UserSettings>(await File.ReadAllTextAsync(path)); if (settings != null) UserSettingsService.Instance.ImportSettings(settings); }
    private void ClearAllData() { var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT"); UserSettingsService.Instance.ResetToFirstRun(); foreach (var f in new[] { "chat_history.json", "token_usage.json", "api_keys.json" }) { var p = Path.Combine(appData, f); if (File.Exists(p)) File.Delete(p); } }

    private void UpdateSystemInfo() { SystemInfoText.Text = $"{RuntimeInformation.OSDescription}\n{RuntimeInformation.FrameworkDescription}\n{RuntimeInformation.ProcessArchitecture}"; }
    private void UpdateVersionInfo() { try { var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; VersionText.Text = string.Format(Res.Settings_Version, v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0"); } catch { VersionText.Text = Res.Settings_Version_Unknown; } }
    private async Task ShowMessageAsync(string title, string msg) { ErrorDialog.ShowError(this, title, msg); await Task.CompletedTask; }
    private async Task<bool> ShowConfirmAsync(string title, string msg) { var tcs = new TaskCompletionSource<bool>(); var ok = new Button { Content = Res.Common_Confirm }; ok.Click += (s, e) => { tcs.SetResult(true); }; var cancel = new Button { Content = Res.Common_Cancel }; cancel.Click += (s, e) => { tcs.SetResult(false); }; var sp = new StackPanel { Spacing = 16 }; sp.Children.Add(new TextBlock { Text = msg, TextWrapping = Avalonia.Media.TextWrapping.Wrap }); sp.Children.Add(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 12, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Children = { cancel, ok } }); var d = new Window { Content = sp, Width = 340, WindowStartupLocation = WindowStartupLocation.CenterOwner }; await d.ShowDialog(this); return await tcs.Task; }
}