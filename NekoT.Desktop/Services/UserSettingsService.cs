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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
            SecureStorage.SetDirectoryAccessControl(appDataPath);
        }

        _settingsPath = Path.Combine(appDataPath, "user_settings.json");
        _settings = LoadSettings();
    }

    public bool HasAcceptedDisclaimer
    {
        get => _settings.DisclaimerAccepted;
        set
        {
            if (_settings.DisclaimerAccepted != value)
            {
                _settings.DisclaimerAccepted = value;
                _settings.DisclaimerAcceptedDate = DateTime.Now;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool HasCompletedOnboarding
    {
        get => _settings.OnboardingCompleted;
        set
        {
            if (_settings.OnboardingCompleted != value)
            {
                _settings.OnboardingCompleted = value;
                _settings.OnboardingCompletedDate = DateTime.Now;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public int OnboardingStep
    {
        get => _settings.OnboardingStep;
        set
        {
            if (_settings.OnboardingStep != value)
            {
                _settings.OnboardingStep = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool OnboardingSkipped
    {
        get => _settings.OnboardingSkipped;
        set
        {
            if (_settings.OnboardingSkipped != value)
            {
                _settings.OnboardingSkipped = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public HashSet<string> ConfiguredProviders => _settings.ConfiguredProviders;

    public void MarkProviderConfigured(string provider)
    {
        if (_settings.ConfiguredProviders.Add(provider))
        {
            SaveSettings();
        }
    }

    public string? UserAgent
    {
        get => _settings.UserAgent;
        set
        {
            if (_settings.UserAgent != value)
            {
                _settings.UserAgent = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool EnableStealthMode
    {
        get => _settings.EnableStealthMode;
        set
        {
            if (_settings.EnableStealthMode != value)
            {
                _settings.EnableStealthMode = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool DisableDevTools
    {
        get => _settings.DisableDevTools;
        set
        {
            if (_settings.DisableDevTools != value)
            {
                _settings.DisableDevTools = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows != value)
            {
                _settings.StartWithWindows = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set
        {
            if (_settings.MinimizeToTray != value)
            {
                _settings.MinimizeToTray = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool StartMinimized
    {
        get => _settings.StartMinimized;
        set
        {
            if (_settings.StartMinimized != value)
            {
                _settings.StartMinimized = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool StartMaximized
    {
        get => _settings.StartMaximized;
        set
        {
            if (_settings.StartMaximized != value)
            {
                _settings.StartMaximized = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string? HomePage
    {
        get => _settings.HomePage;
        set
        {
            if (_settings.HomePage != value)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Setting HomePage from '{_settings.HomePage}' to '{value}'");
                _settings.HomePage = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool ShowTokenMonitor
    {
        get => _settings.ShowTokenMonitor;
        set
        {
            if (_settings.ShowTokenMonitor != value)
            {
                _settings.ShowTokenMonitor = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool ClearDataOnExit
    {
        get => _settings.ClearDataOnExit;
        set
        {
            if (_settings.ClearDataOnExit != value)
            {
                _settings.ClearDataOnExit = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool AutoBackupData
    {
        get => _settings.AutoBackupData;
        set
        {
            if (_settings.AutoBackupData != value)
            {
                _settings.AutoBackupData = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string? BackupPath
    {
        get => _settings.BackupPath;
        set
        {
            if (_settings.BackupPath != value)
            {
                _settings.BackupPath = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public DateTime? LastUpdateCheckTime
    {
        get => _settings.LastUpdateCheckTime;
        set
        {
            if (_settings.LastUpdateCheckTime != value)
            {
                _settings.LastUpdateCheckTime = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public HashSet<string> SkippedVersions => _settings.SkippedVersions;

    public string Language
    {
        get => _settings.Language;
        set
        {
            System.Diagnostics.Debug.WriteLine($"[Language SETTER] Before: {_settings.Language}, New: {value}");
            if (_settings.Language != value)
            {
                _settings.Language = value;
                SaveSettings();
                System.Diagnostics.Debug.WriteLine($"[Language SETTER] Saved, Now: {_settings.Language}");
                OnPropertyChanged();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Language SETTER] Value same, no save");
            }
        }
    }

    public string? LastSelectedProvider
    {
        get => _settings.LastSelectedProvider;
        set
        {
            if (_settings.LastSelectedProvider != value)
            {
                _settings.LastSelectedProvider = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string? LastSelectedModel
    {
        get => _settings.LastSelectedModel;
        set
        {
            if (_settings.LastSelectedModel != value)
            {
                _settings.LastSelectedModel = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string? ProxyUrl
    {
        get => _settings.ProxyUrl;
        set
        {
            if (_settings.ProxyUrl != value)
            {
                _settings.ProxyUrl = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool BlockTracking
    {
        get => _settings.BlockTracking;
        set
        {
            if (_settings.BlockTracking != value)
            {
                _settings.BlockTracking = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool BlockAds
    {
        get => _settings.BlockAds;
        set
        {
            if (_settings.BlockAds != value)
            {
                _settings.BlockAds = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string StartupPanel
    {
        get => _settings.StartupPanel;
        set
        {
            if (_settings.StartupPanel != value)
            {
                _settings.StartupPanel = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public UserSettings ExportSettings()
    {
        return new UserSettings
        {
            DisclaimerAccepted = _settings.DisclaimerAccepted,
            DisclaimerAcceptedDate = _settings.DisclaimerAcceptedDate,
            OnboardingCompleted = _settings.OnboardingCompleted,
            OnboardingCompletedDate = _settings.OnboardingCompletedDate,
            OnboardingStep = _settings.OnboardingStep,
            OnboardingSkipped = _settings.OnboardingSkipped,
            ConfiguredProviders = new HashSet<string>(_settings.ConfiguredProviders),
            AppVersion = _settings.AppVersion,
            Language = _settings.Language,
            UserAgent = _settings.UserAgent,
            EnableStealthMode = _settings.EnableStealthMode,
            DisableDevTools = _settings.DisableDevTools,
            StartWithWindows = _settings.StartWithWindows,
            MinimizeToTray = _settings.MinimizeToTray,
            StartMinimized = _settings.StartMinimized,
            StartMaximized = _settings.StartMaximized,
            HomePage = _settings.HomePage,
            ShowTokenMonitor = _settings.ShowTokenMonitor,
            LastUpdateCheckTime = _settings.LastUpdateCheckTime,
            SkippedVersions = new HashSet<string>(_settings.SkippedVersions),
            LastSelectedProvider = _settings.LastSelectedProvider,
            LastSelectedModel = _settings.LastSelectedModel,
            ProxyUrl = _settings.ProxyUrl,
            BlockTracking = _settings.BlockTracking,
            BlockAds = _settings.BlockAds,
            StartupPanel = _settings.StartupPanel
        };
    }

    public void ImportSettings(UserSettings imported)
    {
        _settings.DisclaimerAccepted = imported.DisclaimerAccepted;
        _settings.DisclaimerAcceptedDate = imported.DisclaimerAcceptedDate;
        _settings.OnboardingCompleted = imported.OnboardingCompleted;
        _settings.OnboardingCompletedDate = imported.OnboardingCompletedDate;
        _settings.OnboardingStep = imported.OnboardingStep;
        _settings.OnboardingSkipped = imported.OnboardingSkipped;
        _settings.ConfiguredProviders = imported.ConfiguredProviders ?? new HashSet<string>();
        _settings.AppVersion = imported.AppVersion;
        _settings.Language = imported.Language;
        _settings.UserAgent = imported.UserAgent;
        _settings.EnableStealthMode = imported.EnableStealthMode;
        _settings.DisableDevTools = imported.DisableDevTools;
        _settings.StartWithWindows = imported.StartWithWindows;
        _settings.MinimizeToTray = imported.MinimizeToTray;
        _settings.StartMinimized = imported.StartMinimized;
        _settings.StartMaximized = imported.StartMaximized;
        _settings.HomePage = imported.HomePage;
        _settings.ShowTokenMonitor = imported.ShowTokenMonitor;
        _settings.LastUpdateCheckTime = imported.LastUpdateCheckTime;
        _settings.SkippedVersions = imported.SkippedVersions ?? new HashSet<string>();
        _settings.LastSelectedProvider = imported.LastSelectedProvider;
        _settings.LastSelectedModel = imported.LastSelectedModel;
        _settings.ProxyUrl = imported.ProxyUrl;
        _settings.BlockTracking = imported.BlockTracking;
        _settings.BlockAds = imported.BlockAds;
        _settings.StartupPanel = imported.StartupPanel;
        SaveSettings();
    }

    public void ResetToFirstRun()
    {
        _settings = new UserSettings();
        SaveSettings();
    }

    private UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Loading settings from: {_settingsPath}");
                System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Raw JSON: {json}");
                var settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Loaded HomePage: '{settings.HomePage}'");
                return settings;
            }
            System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Settings file not found, using defaults");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Load failed: {ex.Message}");
        }

        return new UserSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json, System.Text.Encoding.UTF8);
            SecureStorage.SetFileAccessControl(_settingsPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSettingsService] Save failed: {ex.Message}");
        }
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
    public string? AppVersion { get; set; }
    
    public string? LastSelectedProvider { get; set; }
    public string? LastSelectedModel { get; set; }
    
    public string Language { get; set; } = LanguageConstants.Chinese;
    
    public string? UserAgent { get; set; }
    public bool EnableStealthMode { get; set; } = false;
    public bool DisableDevTools { get; set; } = false;
    
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; }
    public bool StartMaximized { get; set; } = true;
    public string? HomePage { get; set; } = "about:blank";
    public bool ShowTokenMonitor { get; set; } = true;
    public bool ClearDataOnExit { get; set; }
    public bool AutoBackupData { get; set; } = true;
    public string? BackupPath { get; set; }
    
    public DateTime? LastUpdateCheckTime { get; set; }
    public HashSet<string> SkippedVersions { get; set; } = new();
    
    public string? ProxyUrl { get; set; }
    public bool BlockTracking { get; set; } = true;
    public bool BlockAds { get; set; }
    public string StartupPanel { get; set; } = "browser";
}