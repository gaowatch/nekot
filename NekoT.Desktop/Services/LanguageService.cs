using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.Services;

public class LanguageService : INotifyPropertyChanged
{
    private static LanguageService? _instance;
    private CultureInfo _currentCulture;
    private readonly ResourceManager _resourceManager;

    public static LanguageService Instance => _instance ??= new LanguageService();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public ResourceManager ResourceManager => _resourceManager;

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;
                Strings.Culture = value;
                OnPropertyChanged();
                OnLanguageChanged();
            }
        }
    }

    public string CurrentLanguageCode => CurrentCulture.Name;

    public string CurrentLanguageDisplayName
    {
        get
        {
            return CurrentCulture.Name switch
            {
                "zh-CN" => "简体中文",
                "en" => "English",
                _ => CurrentCulture.DisplayName
            };
        }
    }

    private LanguageService()
    {
        _resourceManager = new ResourceManager("NekoT.Desktop.Resources.Strings", typeof(LanguageService).Assembly);
        _currentCulture = CultureInfo.CurrentUICulture;
        if (_currentCulture.Name != "zh-CN" && !_currentCulture.Name.StartsWith("en"))
        {
            _currentCulture = new CultureInfo("zh-CN");
        }
    }

    public string GetString(string key)
    {
        return _resourceManager.GetString(key, _currentCulture) ?? key;
    }

    public void SwitchLanguage(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            CurrentCulture = culture;
        }
        catch (CultureNotFoundException)
        {
            CurrentCulture = new CultureInfo("zh-CN");
        }
    }

    public void SwitchToChinese() => SwitchLanguage("zh-CN");
    public void SwitchToEnglish() => SwitchLanguage("en");
    public bool IsChinese => CurrentCulture.Name == "zh-CN";
    public bool IsEnglish => CurrentCulture.Name.StartsWith("en");

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnLanguageChanged()
    {
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}