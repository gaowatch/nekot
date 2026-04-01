using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using NekoT.Desktop.Constants;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.Services;

public class UserSettings
{
    public string Language { get; set; } = "zh-CN";
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool StartMaximized { get; set; } = false;
    public string StartupPanel { get; set; } = "browser";
    public string HomePage { get; set; } = "about:blank";
    public bool ShowTokenMonitor { get; set; } = true;
    public bool EnableStealthMode { get; set; } = true;
    public bool DisableDevTools { get; set; } = true;
    public bool BlockTracking { get; set; } = true;
    public bool BlockAds { get; set; } = false;
    public string? ProxyUrl { get; set; }
    public string? UserAgent { get; set; }
    public bool HasAcceptedDisclaimer { get; set; } = false;
    public bool HasCompletedOnboarding { get; set; } = false;
    public string? LastUpdateCheck { get; set; }
    public string? LastVersion { get; set; }
}

public class UserSettingsService : ObservableObject
{
    private static UserSettingsService? _instance;
    private static readonly object _lock = new();
    private readonly string _settingsPath;
    private UserSettings _settings;
    private readonly SynchronizationContext? _syncContext;

    public static UserSettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new UserSettingsService();
                }
            }
            return _instance;
        }
    }

    private UserSettingsService()
    {
        _syncContext = SynchronizationContext.Current;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _settings = LoadSettings();
    }

    private UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSettings] Load failed: {ex.Message}");
        }
        return new UserSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserSettings] Save failed: {ex.Message}");
        }
    }

    public void ImportSettings(UserSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        SaveSettings();
        OnPropertyChanged(string.Empty);
    }

    public void ResetToFirstRun()
    {
        _settings = new UserSettings();
        SaveSettings();
        OnPropertyChanged(string.Empty);
    }

    public string Language
    {
        get => _settings.Language;
        set
        {
            if (_settings.Language != value)
            {
                _settings.Language = value;
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

    public string HomePage
    {
        get => _settings.HomePage;
        set
        {
            if (_settings.HomePage != value)
            {
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

    public bool HasAcceptedDisclaimer
    {
        get => _settings.HasAcceptedDisclaimer;
        set
        {
            if (_settings.HasAcceptedDisclaimer != value)
            {
                _settings.HasAcceptedDisclaimer = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool HasCompletedOnboarding
    {
        get => _settings.HasCompletedOnboarding;
        set
        {
            if (_settings.HasCompletedOnboarding != value)
            {
                _settings.HasCompletedOnboarding = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string? LastUpdateCheck
    {
        get => _settings.LastUpdateCheck;
        set
        {
            if (_settings.LastUpdateCheck != value)
            {
                _settings.LastUpdateCheck = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public string? LastVersion
    {
        get => _settings.LastVersion;
        set
        {
            if (_settings.LastVersion != value)
            {
                _settings.LastVersion = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }
}