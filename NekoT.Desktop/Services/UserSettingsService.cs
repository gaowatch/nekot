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

    public static UserSettingsService Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new UserSettingsService();
                return _instance;
            }
        }
    }

    private UserSettingsService()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NekoT");
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
        SecureStorage.SetDirectoryAccessControl(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "user_settings.json");
        _settings = LoadSettings();
    }

    public bool HasAcceptedDisclaimer { get => _settings.DisclaimerAccepted; set { _settings.DisclaimerAccepted = value; _settings.DisclaimerAcceptedDate = DateTime.Now; SaveSettings(); OnPropertyChanged(); } }
    public bool HasCompletedOnboarding { get => _settings.OnboardingCompleted; set { _settings.OnboardingCompleted = value; _settings.OnboardingCompletedDate = DateTime.Now; SaveSettings(); OnPropertyChanged(); } }
    public int OnboardingStep { get => _settings.OnboardingStep; set { _settings.OnboardingStep = value; SaveSettings(); OnPropertyChanged(); } }
    public bool OnboardingSkipped { get => _settings.OnboardingSkipped; set { _settings.OnboardingSkipped = value; SaveSettings(); OnPropertyChanged(); } }
    public HashSet<string> ConfiguredProviders => _settings.ConfiguredProviders;
    public string Language { get => _settings.Language; set { _settings.Language = value; SaveSettings(); OnPropertyChanged(); } }
    public string? HomePage { get => _settings.HomePage; set { _settings.HomePage = value; SaveSettings(); OnPropertyChanged(); } }
    public bool ShowTokenMonitor { get => _settings.ShowTokenMonitor; set { _settings.ShowTokenMonitor = value; SaveSettings(); OnPropertyChanged(); } }
    public bool MinimizeToTray { get => _settings.MinimizeToTray; set { _settings.MinimizeToTray = value; SaveSettings(); OnPropertyChanged(); } }
    public bool StartMaximized { get => _settings.StartMaximized; set { _settings.StartMaximized = value; SaveSettings(); OnPropertyChanged(); } }
    public bool BlockTracking { get => _settings.BlockTracking; set { _settings.BlockTracking = value; SaveSettings(); OnPropertyChanged(); } }
    public string StartupPanel { get => _settings.StartupPanel; set { _settings.StartupPanel = value; SaveSettings(); OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private UserSettings LoadSettings()
    {
        try { if (File.Exists(_settingsPath)) return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_settingsPath)) ?? new UserSettings(); }
        catch { }
        return new UserSettings();
    }

    private void SaveSettings()
    {
        try { File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true })); SecureStorage.SetFileAccessControl(_settingsPath); }
        catch { }
    }
}

public class UserSettings
{
    public bool DisclaimerAccepted { get; set; }
    public DateTime? DisclaimerAcceptedDate { get; set; }
    public bool OnboardingCompleted { get; set; }
    public DateTime? OnboardingCompletedDate { get; set; }
    public int OnboardingStep { get; set; }
    public bool OnboardingSkipped { get; set; }
    public HashSet<string> ConfiguredProviders { get; set; } = new();
    public string Language { get; set; } = LanguageConstants.Chinese;
    public string? HomePage { get; set; } = "about:blank";
    public bool ShowTokenMonitor { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMaximized { get; set; } = true;
    public bool BlockTracking { get; set; } = true;
    public string StartupPanel { get; set; } = "browser";
}
