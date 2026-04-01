using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NekoT.Core.Security;
using NekoT.Core.LlmProviders;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels;

public class SidePanelViewModel : ViewModelBase
{
    private bool _isOpen;
    private string _apiKey = string.Empty;
    private string _selectedProvider = "openai";
    private string _selectedModel = string.Empty;
    private readonly SecureStorage _secureStorage;
    private readonly LlmProviderManager _providerManager;
    private int _sessionTokens;
    private int _totalTokens;
    private bool _isForwardingConnected;
    private string _forwardingStatus = Strings.Status_NotConnected;
    private string _validationStatus = string.Empty;
    
    private string _homePage = "https://www.baidu.com";
    private int _selectedSearchEngineIndex = 0;
    private int _zoomLevel = 100;
    private bool _enableDevTools = true;

    public event EventHandler? ModelChanged;
    public event EventHandler? BrowserSettingsChanged;

    public SidePanelViewModel() : this(new SecureStorage(), LlmProviderManager.Instance)
    {
    }

    public SidePanelViewModel(SecureStorage secureStorage, LlmProviderManager providerManager)
    {
        _secureStorage = secureStorage;
        _providerManager = providerManager;
        
        SaveApiKeyCommand = new RelayCommand(_ => SaveApiKey());
        ClearApiKeyCommand = new RelayCommand(_ => ClearApiKey());
        
        AvailableProviders = new ObservableCollection<ProviderDisplayItem>(
            _providerManager.Providers.Values.Select(p => new ProviderDisplayItem
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                Alias = p.Alias,
                ApiUrl = p.ApiUrl
            }));

        SearchEngines = new ObservableCollection<string>
        {
            Strings.Search_Baidu,
            Strings.Search_Bing,
            Strings.Search_Google,
            Strings.Search_Sogou
        };

        UpdateAvailableModels(_selectedProvider);
    }

    public bool IsOpen { get => _isOpen; set => SetField(ref _isOpen, value); }
    public string ApiKey { get => _apiKey; set => SetField(ref _apiKey, value); }
    public string SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetField(ref _selectedProvider, value))
            {
                LoadSavedApiKey();
                UpdateAvailableModels(value);
            }
        }
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetField(ref _selectedModel, value))
            {
                ModelChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ObservableCollection<ModelDisplayItem> AvailableModels { get; } = new();
    public ObservableCollection<ProviderDisplayItem> AvailableProviders { get; }
    public ObservableCollection<string> SearchEngines { get; }
    public bool HasApiKey => !string.IsNullOrEmpty(_apiKey);
    public ICommand SaveApiKeyCommand { get; }
    public ICommand ClearApiKeyCommand { get; }
    public int SessionTokens { get => _sessionTokens; set => SetField(ref _sessionTokens, value); }
    public int TotalTokens { get => _totalTokens; set => SetField(ref _totalTokens, value); }
    public bool IsForwardingConnected
    {
        get => _isForwardingConnected;
        set { if (SetField(ref _isForwardingConnected, value)) ForwardingStatus = value ? Strings.Status_Connected : Strings.Status_NotConnected; }
    }
    public string ForwardingStatus { get => _forwardingStatus; private set => SetField(ref _forwardingStatus, value); }
    public string ValidationStatus { get => _validationStatus; set => SetField(ref _validationStatus, value); }
    public string HomePage { get => _homePage; set { if (SetField(ref _homePage, value)) BrowserSettingsChanged?.Invoke(this, EventArgs.Empty); } }
    public int SelectedSearchEngineIndex { get => _selectedSearchEngineIndex; set { if (SetField(ref _selectedSearchEngineIndex, value)) BrowserSettingsChanged?.Invoke(this, EventArgs.Empty); } }
    public int ZoomLevel { get => _zoomLevel; set { if (SetField(ref _zoomLevel, value)) BrowserSettingsChanged?.Invoke(this, EventArgs.Empty); } }
    public bool EnableDevTools { get => _enableDevTools; set { if (SetField(ref _enableDevTools, value)) BrowserSettingsChanged?.Invoke(this, EventArgs.Empty); } }

    public string GetSearchEngineUrl() => _selectedSearchEngineIndex switch
    {
        0 => "https://www.baidu.com/s?wd=",
        1 => "https://www.bing.com/search?q=",
        2 => "https://www.google.com/search?q=",
        3 => "https://www.sogou.com/web?query=",
        _ => "https://www.baidu.com/s?wd="
    };

    public void UpdateTokens(int sessionTokens, int totalTokens) { SessionTokens = sessionTokens; TotalTokens = totalTokens; }

    private void UpdateAvailableModels(string providerName)
    {
        AvailableModels.Clear();
        var models = _providerManager.GetSupportedModels(providerName);
        foreach (var model in models) AvailableModels.Add(model);
        var defaultModel = _providerManager.GetDefaultModel(providerName);
        if (!string.IsNullOrEmpty(defaultModel)) { _selectedModel = defaultModel; OnPropertyChanged(nameof(SelectedModel)); }
        else if (AvailableModels.Count > 0) { _selectedModel = AvailableModels[0].Id; OnPropertyChanged(nameof(SelectedModel)); }
    }

    public void TogglePanel() => IsOpen = !IsOpen;

    public bool ValidateApiKeyFormat(string provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return false;
        return provider.ToLowerInvariant() switch
        {
            "openai" => apiKey.StartsWith("sk-") && apiKey.Length >= 20,
            "anthropic" => apiKey.StartsWith("sk-ant-"),
            "minimax" => apiKey.StartsWith("sk-"),
            "deepseek" => apiKey.StartsWith("sk-"),
            "moonshot" => apiKey.StartsWith("sk-"),
            "zhipuai" => apiKey.Length >= 20,
            "doubao" => apiKey.Length >= 20,
            "baidu" => apiKey.Length >= 20,
            "alibaba" => apiKey.Length >= 20,
            "iflytek" => apiKey.Length >= 20,
            _ => apiKey.Length >= 10
        };
    }

    public void SaveApiKey()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ApiKey)) { ValidationStatus = Strings.Status_EnterAPIKey; return; }
            if (!ValidateApiKeyFormat(SelectedProvider, ApiKey)) { ValidationStatus = Strings.Status_InvalidAPIKey; return; }
            _secureStorage.SaveApiKey(SelectedProvider, ApiKey);
            OnPropertyChanged(nameof(HasApiKey));
            ValidationStatus = Strings.Status_APIKeySaved;
        }
        catch (Exception ex) { ValidationStatus = $"{Strings.Status_SaveFailed}{ex.Message}"; }
    }

    public void ClearApiKey()
    {
        try
        {
            _secureStorage.DeleteApiKey(SelectedProvider);
            ApiKey = string.Empty;
            ValidationStatus = string.Empty;
            OnPropertyChanged(nameof(HasApiKey));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SidePanel] Clear API key failed: {ex.Message}"); }
    }

    private void LoadSavedApiKey()
    {
        var savedKey = _secureStorage.GetApiKey(SelectedProvider);
        if (!string.IsNullOrEmpty(savedKey)) ApiKey = savedKey;
    }
}
