using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NekoT.Core.Security;
using NekoT.Desktop.Constants;

namespace NekoT.Desktop.Services;

public class UserSettingsService : INotifyPropertyChanged
{
    private static UserSettingsService? _instance;
    private static readonly object _lock = new();
    private readonly string _settingsPath;
    private UserSettings _settings;
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public static UserSettingsService Instance { get { lock (_lock) { _instance ??= new UserSettingsService(); return _instance; } } }

    private UserSettingsService()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT");
        if (!Directory.Exists(appDataPath)) { Directory.CreateDirectory(appDataPath); SecureStorage.SetDirectoryAccessControl(appDataPath); }
        _settingsPath = Path.Combine(appDataPath, "user_settings.json");
        _settings = LoadSettings();
    }

    public bool HasAcceptedDisclaimer { get => _settings.DisclaimerAccepted; set { if (_settings.DisclaimerAccepted != value) { _settings.DisclaimerAccepted = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool HasCompletedOnboarding { get => _settings.OnboardingCompleted; set { if (_settings.OnboardingCompleted != value) { _settings.OnboardingCompleted = value; SaveSettings(); OnPropertyChanged(); } } }
    public string Language { get => _settings.Language; set { if (_settings.Language != value) { _settings.Language = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool StartWithWindows { get => _settings.StartWithWindows; set { if (_settings.StartWithWindows != value) { _settings.StartWithWindows = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool MinimizeToTray { get => _settings.MinimizeToTray; set { if (_settings.MinimizeToTray != value) { _settings.MinimizeToTray = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool StartMinimized { get => _settings.StartMinimized; set { if (_settings.StartMinimized != value) { _settings.StartMinimized = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool StartMaximized { get => _settings.StartMaximized; set { if (_settings.StartMaximized != value) { _settings.StartMaximized = value; SaveSettings(); OnPropertyChanged(); } } }
    public string? HomePage { get => _settings.HomePage; set { if (_settings.HomePage != value) { _settings.HomePage = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool ShowTokenMonitor { get => _settings.ShowTokenMonitor; set { if (_settings.ShowTokenMonitor != value) { _settings.ShowTokenMonitor = value; SaveSettings(); OnPropertyChanged(); } } }
    public string? ProxyUrl { get => _settings.ProxyUrl; set { if (_settings.ProxyUrl != value) { _settings.ProxyUrl = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool BlockTracking { get => _settings.BlockTracking; set { if (_settings.BlockTracking != value) { _settings.BlockTracking = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool BlockAds { get => _settings.BlockAds; set { if (_settings.BlockAds != value) { _settings.BlockAds = value; SaveSettings(); OnPropertyChanged(); } } }
    public string StartupPanel { get => _settings.StartupPanel; set { if (_settings.StartupPanel != value) { _settings.StartupPanel = value; SaveSettings(); OnPropertyChanged(); } } }
    public string? UserAgent { get => _settings.UserAgent; set { if (_settings.UserAgent != value) { _settings.UserAgent = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool EnableStealthMode { get => _settings.EnableStealthMode; set { if (_settings.EnableStealthMode != value) { _settings.EnableStealthMode = value; SaveSettings(); OnPropertyChanged(); } } }
    public bool DisableDevTools { get => _settings.DisableDevTools; set { if (_settings.DisableDevTools != value) { _settings.DisableDevTools = value; SaveSettings(); OnPropertyChanged(); } } }
    public DateTime? LastUpdateCheckTime { get => _settings.LastUpdateCheckTime; set { if (_settings.LastUpdateCheckTime != value) { _settings.LastUpdateCheckTime = value; SaveSettings(); OnPropertyChanged(); } } }
    public HashSet<string> SkippedVersions => _settings.SkippedVersions;
    public string? LastSelectedProvider { get => _settings.LastSelectedProvider; set { if (_settings.LastSelectedProvider != value) { _settings.LastSelectedProvider = value; SaveSettings(); OnPropertyChanged(); } } }
    public string? LastSelectedModel { get => _settings.LastSelectedModel; set { if (_settings.LastSelectedModel != value) { _settings.LastSelectedModel = value; SaveSettings(); OnPropertyChanged(); } } }

    private UserSettings LoadSettings() { try { if (File.Exists(_settingsPath)) { var json = File.ReadAllText(_settingsPath); return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings(); } } catch { } return new UserSettings(); }
    private void SaveSettings() { try { var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_settingsPath, json); SecureStorage.SetFileAccessControl(_settingsPath); } catch { } }
    public void ResetToFirstRun() { _settings = new UserSettings(); SaveSettings(); }
}

public class UserSettings
{
    public bool DisclaimerAccepted { get; set; }
    public bool OnboardingCompleted { get; set; }
    public string Language { get; set; } = LanguageConstants.Chinese;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; }
    public bool StartMaximized { get; set; } = true;
    public string? HomePage { get; set; } = "about:blank";
    public bool ShowTokenMonitor { get; set; } = true;
    public string? ProxyUrl { get; set; }
    public bool BlockTracking { get; set; } = true;
    public bool BlockAds { get; set; }
    public string StartupPanel { get; set; } = "browser";
    public string? UserAgent { get; set; }
    public bool EnableStealthMode { get; set; }
    public bool DisableDevTools { get; set; }
    public DateTime? LastUpdateCheckTime { get; set; }
    public HashSet<string> SkippedVersions { get; set; } = new();
    public string? LastSelectedProvider { get; set; }
    public string? LastSelectedModel { get; set; }
}