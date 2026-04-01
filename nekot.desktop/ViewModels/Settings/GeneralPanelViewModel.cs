using System;
using System.Threading.Tasks;
using NekoT.Desktop.Services;

namespace NekoT.Desktop.ViewModels.Settings;

public class GeneralPanelViewModel : ViewModelBase, ISettingsPanelViewModel
{
    private int _languageIndex;
    private bool _startWithWindows;
    private bool _minimizeToTray = true;
    private bool _startMinimized;
    private bool _startMaximized = true;
    private string _homePage = "about:blank";
    private bool _showTokenMonitor;
    private string _userAgent = string.Empty;
    private bool _enableStealthMode = true;
    private bool _disableDevTools = true;

    public string PanelName => "general";

    public int LanguageIndex
    {
        get => _languageIndex;
        set
        {
            if (SetField(ref _languageIndex, value))
            {
                OnPropertyChanged(nameof(Language));
            }
        }
    }

    public string Language
    {
        get => _languageIndex == 1 ? "en" : "zh-CN";
        set => LanguageIndex = value == "en" ? 1 : 0;
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetField(ref _startWithWindows, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetField(ref _minimizeToTray, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetField(ref _startMinimized, value);
    }

    public bool StartMaximized
    {
        get => _startMaximized;
        set => SetField(ref _startMaximized, value);
    }

    public string HomePage
    {
        get => _homePage;
        set => SetField(ref _homePage, value);
    }

    public bool ShowTokenMonitor
    {
        get => _showTokenMonitor;
        set => SetField(ref _showTokenMonitor, value);
    }

    public string UserAgent
    {
        get => _userAgent;
        set => SetField(ref _userAgent, value);
    }

    public bool EnableStealthMode
    {
        get => _enableStealthMode;
        set => SetField(ref _enableStealthMode, value);
    }

    public bool DisableDevTools
    {
        get => _disableDevTools;
        set => SetField(ref _disableDevTools, value);
    }

    public event Func<string, Task<bool>>? LanguageChangeRequested;

    public void LoadSettings()
    {
        var settings = UserSettingsService.Instance;

        LanguageIndex = settings.Language == "en" ? 1 : 0;
        StartWithWindows = settings.StartWithWindows;
        MinimizeToTray = settings.MinimizeToTray;
        StartMinimized = settings.StartMinimized;
        StartMaximized = settings.StartMaximized;
        HomePage = settings.HomePage ?? "about:blank";
        ShowTokenMonitor = settings.ShowTokenMonitor;
        UserAgent = settings.UserAgent ?? string.Empty;
        EnableStealthMode = settings.EnableStealthMode;
        DisableDevTools = settings.DisableDevTools;
    }

    public void SaveSettings()
    {
        var settings = UserSettingsService.Instance;

        settings.Language = Language;
        settings.StartWithWindows = StartWithWindows;
        settings.MinimizeToTray = MinimizeToTray;
        settings.StartMinimized = StartMinimized;
        settings.StartMaximized = StartMaximized;
        settings.HomePage = HomePage;
        settings.ShowTokenMonitor = ShowTokenMonitor;
        settings.UserAgent = UserAgent;
        settings.EnableStealthMode = EnableStealthMode;
        settings.DisableDevTools = DisableDevTools;
    }

    public async Task<bool> HandleLanguageChangeAsync(string newLanguage)
    {
        var currentLanguage = UserSettingsService.Instance.Language;

        if (newLanguage != currentLanguage)
        {
            if (LanguageChangeRequested != null)
            {
                return await LanguageChangeRequested.Invoke(newLanguage);
            }
        }

        return false;
    }
}