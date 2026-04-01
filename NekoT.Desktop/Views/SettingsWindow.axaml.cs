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

    public void SetMainViewModel(MainViewModel viewModel)
    {
        _mainViewModel = viewModel;
    }

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

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is string panelName)
        {
            SwitchPanel(panelName);
        }
    }

    private void SwitchPanel(string panelName)
    {
        foreach (var (name, setVisibility) in _panelVisibilityActions)
        {
            setVisibility(name == panelName);
        }
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_languageChanged)
            {
                var newLanguage = LanguageComboBox.SelectedIndex == 1 ? "en" : "zh-CN";
                UserSettingsService.Instance.Language = newLanguage;
            }

            SaveSettings();

            if (_languageChanged)
            {
                var result = await ShowConfirmAsync(
                    Res.LanguageDialog_Title,
                    Res.LanguageDialog_Message);

                if (result)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = "--restart",
                            UseShellExecute = true,
                            WorkingDirectory = Path.GetDirectoryName(exePath)
                        };

                        try
                            {
                                Process.Start(startInfo);
                                Thread.Sleep(1000);
                            }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to restart: {ex.Message}");
                        }
                    }

                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.Shutdown();
                    }
                    return;
                }
                else
                {
                    var currentLang = UserSettingsService.Instance.Language;
                    LanguageComboBox.SelectedIndex = currentLang == "en" ? 1 : 0;
                    _languageChanged = false;
                }
            }

            await ShowMessageAsync(Res.Common_Success, Res.Settings_Saved);
            Close();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(Res.Common_Error, $"{Res.Settings_SaveFailed}: {ex.Message}");
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox == null) return;
        if (LanguageComboBox.SelectedIndex < 0) return;

        var newLanguage = LanguageComboBox.SelectedIndex == 1 ? "en" : "zh-CN";
        var currentLanguage = UserSettingsService.Instance.Language;

        if (newLanguage != currentLanguage)
        {
            _languageChanged = true;
        }
    }

    private async void OnExportChatHistory(object? sender, RoutedEventArgs e)
    {
        try
        {
            var storageProvider = StorageProvider;
            if (storageProvider == null) return;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Res.Export_ChatHistory,
                SuggestedFileName = $"chat_history_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(Res.Export_JSON) { Patterns = new[] { "*.json" } }
                }
            });

            if (file != null)
            {
                await ExportChatHistoryAsync(file.Path.LocalPath);
                await ShowMessageAsync(Res.Common_Success, Res.Export_ChatExported);
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(Res.Common_Error, $"{Res.Export_ChatHistoryFailed}: {ex.Message}");
        }
    }

    private async void OnExportTokenUsage(object? sender, RoutedEventArgs e)
    {
        try
        {
            var storageProvider = StorageProvider;
            if (storageProvider == null) return;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Res.Export_TokenUsage,
                SuggestedFileName = $"token_usage_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(Res.Export_JSON) { Patterns = new[] { "*.json" } }
                }
            });

            if (file != null)
            {
                await ExportTokenUsageAsync(file.Path.LocalPath);
                await ShowMessageAsync(Res.Common_Success, Res.Export_TokenExported);
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(Res.Common_Error, $"{Res.Export_TokenUsageFailed}: {ex.Message}");
        }
    }

    private async void OnImportData(object? sender, RoutedEventArgs e)
    {
        try
        {
            var storageProvider = StorageProvider;
            if (storageProvider == null) return;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Res.Import_Data,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Res.Export_JSON) { Patterns = new[] { "*.json" } }
                }
            });

            var file = files.FirstOrDefault();
            if (file != null)
            {
                var result = await ShowConfirmAsync(
                    Res.Import_ConfirmTitle,
                    Res.Import_ConfirmMessage);

                if (result)
                {
                    await ImportDataAsync(file.Path.LocalPath);
                    LoadSettings();
                    await ShowMessageAsync(Res.Common_Success, Res.Import_Success);
                }
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(Res.Common_Error, $"{Res.Import_Failed}: {ex.Message}");
        }
    }

    private async void OnClearAllData(object? sender, RoutedEventArgs e)
    {
        try
        {
            var result = await ShowConfirmAsync(
                Res.Clear_ConfirmTitle,
                Res.Clear_ConfirmMessage);

            if (result)
            {
                ClearAllData();
                await ShowMessageAsync(Res.Common_Success, Res.Clear_Success);
                Close();
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(Res.Common_Error, $"{Res.Clear_Failed}: {ex.Message}");
        }
    }

    private void OnOpenGitHub(object? sender, RoutedEventArgs e)
    {
        try
        {
            var url = "https://github.com/nekot-ai/nekot";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open GitHub: {ex.Message}");
        }
    }

    private async Task ExportChatHistoryAsync(string filePath)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");

        var chatHistoryPath = Path.Combine(appDataPath, "chat_history.json");

        if (File.Exists(chatHistoryPath))
        {
            File.Copy(chatHistoryPath, filePath, true);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, "[]");
        }
    }

    private async Task ExportTokenUsageAsync(string filePath)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");

        var tokenUsagePath = Path.Combine(appDataPath, "token_usage.json");

        if (File.Exists(tokenUsagePath))
        {
            File.Copy(tokenUsagePath, filePath, true);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, "[]");
        }
    }

    private async Task ImportDataAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var importedSettings = JsonSerializer.Deserialize<UserSettings>(json);

        if (importedSettings != null)
        {
            UserSettingsService.Instance.ImportSettings(importedSettings);
        }
    }

    private void ClearAllData()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");

        UserSettingsService.Instance.ResetToFirstRun();

        var chatHistoryPath = Path.Combine(appDataPath, "chat_history.json");
        if (File.Exists(chatHistoryPath))
        {
            File.Delete(chatHistoryPath);
        }

        var tokenUsagePath = Path.Combine(appDataPath, "token_usage.json");
        if (File.Exists(tokenUsagePath))
        {
            File.Delete(tokenUsagePath);
        }

        var apiKeysPath = Path.Combine(appDataPath, "api_keys.json");
        if (File.Exists(apiKeysPath))
        {
            File.Delete(apiKeysPath);
        }
    }

    private void UpdateSystemInfo()
    {
        var osDescription = RuntimeInformation.OSDescription;
        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();

        SystemInfoText.Text = string.Format(
            $"{Res.Settings_SystemInfo_OS}\n" +
            $"{Res.Settings_SystemInfo_Runtime}\n" +
            $"{Res.Settings_SystemInfo_Arch}",
            osDescription, runtimeVersion, architecture);
    }

    private void UpdateVersionInfo()
    {
        try
        {
            var version = GetAssemblyVersion();
            VersionText.Text = string.Format(Res.Settings_Version, version);
        }
        catch
        {
            VersionText.Text = Res.Settings_Version_Unknown;
        }
    }

    private string GetAssemblyVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        ErrorDialog.ShowError(this, title, message, null, ErrorSeverity.Info);
        await Task.CompletedTask;
    }

    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var panel = new Panel
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#80000000"))
        };
        
        Grid.SetColumnSpan(panel, 2);
        Grid.SetRowSpan(panel, 1);

        var border = new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1E1E1E")),
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#333333")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24),
            Width = 340,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var stackPanel = new StackPanel
        {
            Spacing = 16
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
        };

        var messageBlock = new TextBlock
        {
            Text = message,
            FontSize = 14,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#B0B0B0"))
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };

        var cancelButton = new Button
        {
            Content = Res.Common_Cancel,
            Classes = { "Secondary" },
            Padding = new Thickness(16, 8)
        };

        var confirmButton = new Button
        {
            Content = Res.Common_Confirm,
            Classes = { "Primary" },
            Padding = new Thickness(24, 8)
        };

        cancelButton.Click += (s, e) =>
        {
            panel.IsVisible = false;
            if (this.Content is Panel rootPanel)
            {
                rootPanel.Children.Remove(panel);
            }
            tcs.SetResult(false);
        };

        confirmButton.Click += (s, e) =>
        {
            panel.IsVisible = false;
            if (this.Content is Panel rootPanel)
            {
                rootPanel.Children.Remove(panel);
            }
            tcs.SetResult(true);
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(confirmButton);

        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(messageBlock);
        stackPanel.Children.Add(buttonPanel);

        border.Child = stackPanel;
        panel.Children.Add(border);

        if (this.Content is Panel rootPanel)
        {
            rootPanel.Children.Add(panel);
        }

        return await tcs.Task;
    }
}