using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NekoT.Core.Configuration;
using NekoT.Core.Proxy;
using NekoT.Core.Security;
using NekoT.Core.LlmProviders;
using NekoT.Core.Pricing;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Views.TokenVisualization;
using NekoT.Desktop.Services;

namespace NekoT.Desktop.ViewModels;

public enum ActiveViewMode
{
    Chat,
    Forwarding
}

public class ForwardingServiceViewModel : ViewModelBase, IDisposable, IAsyncDisposable
{
    private const string ServiceType = "ForwardingService";
    private string StatsUrl => AppConstants.Forwarding.StatsUrl;
    private bool _isServiceRunning;
    private int _todayRequestCount;
    private int _todayTokenCount;
    private int _currentConnectionCount;
    private readonly string _listeningAddress = AppConstants.Forwarding.GatewayUrl;
    private string _apiKey = string.Empty;
    private string _selectedProvider = "openai";
    private string _selectedModel = string.Empty;
    private readonly SecureStorage _secureStorage;
    private readonly LlmProviderManager _providerManager;
    private readonly PricingCalculator _pricingCalculator;
    private readonly TokenUsageStorage _tokenUsageStorage;
    private string _validationStatus = string.Empty;
    private decimal _inputPricePer1K;
    private decimal _outputPricePer1K;
    private decimal _todayCost;
    public TokenBarChartViewModel TokenBarChartVM { get; }
    private decimal _sessionCost;
    private string _currency = "USD";
    private System.Timers.Timer? _statsPollingTimer;
    private int _lastRecordedTotalTokens;
    private LLMApiGatewayService? _gatewayService;

    public ForwardingServiceViewModel() : this(new SecureStorage(), LlmProviderManager.Instance) { }

    public ForwardingServiceViewModel(SecureStorage secureStorage, LlmProviderManager providerManager)
    {
        _secureStorage = secureStorage;
        _providerManager = providerManager;
        _pricingCalculator = new PricingCalculator();
        _tokenUsageStorage = TokenUsageStorage.Instance;
        TokenBarChartVM = new TokenBarChartViewModel(this);
        ToggleServiceCommand = new AsyncRelayCommand(async _ => await ToggleServiceAsync());
        CopyAddressCommand = new RelayCommand(_ => CopyAddress());
        SaveApiKeyCommand = new RelayCommand(_ => SaveApiKey());
        ClearApiKeyCommand = new RelayCommand(_ => ClearApiKey());
        SavePricingCommand = new RelayCommand(_ => SavePricing());
        ResetPricingCommand = new RelayCommand(_ => ResetPricing());
        AvailableProviders = new ObservableCollection<ProviderDisplayItem>(_providerManager.Providers.Values.Select(p => new ProviderDisplayItem { Name = p.Name, DisplayName = p.DisplayName, Alias = p.Alias, ApiUrl = p.ApiUrl }));
        LoadLastSelectedProvider();
        UpdateAvailableModels(_selectedProvider);
        LoadSavedApiKey();
        LoadLastSelectedModel();
        RefreshTodayCost();
        _ = LoadTokenUsageAsync();
    }

    public bool IsServiceRunning { get => _isServiceRunning; set => SetField(ref _isServiceRunning, value); }
    public string ListeningAddress => _listeningAddress;
    public int TodayRequestCount { get => _todayRequestCount; set => SetField(ref _todayRequestCount, value); }
    public int TodayTokenCount { get => _todayTokenCount; set => SetField(ref _todayTokenCount, value); }
    private int _latestTokenCount;
    public int LatestTokenCount { get => _latestTokenCount; set => SetField(ref _latestTokenCount, value); }
    public int CurrentConnectionCount { get => _currentConnectionCount; set => SetField(ref _currentConnectionCount, value); }
    public string ApiKey { get => _apiKey; set => SetField(ref _apiKey, value); }
    public string SelectedProvider { get => _selectedProvider; set { if (SetField(ref _selectedProvider, value)) { UserSettingsService.Instance.LastSelectedProvider = value; LoadSavedApiKey(); UpdateAvailableModels(value); LoadPricingForModel(); } } }
    public string SelectedModel { get => _selectedModel; set { if (SetField(ref _selectedModel, value)) { UserSettingsService.Instance.LastSelectedModel = value; LoadPricingForModel(); } } }
    public ObservableCollection<ModelDisplayItem> AvailableModels { get; } = new();
    public int SelectedModelIndex { get { for (int i = 0; i < AvailableModels.Count; i++) { if (AvailableModels[i].Id == _selectedModel) return i; } return 0; } set { if (value >= 0 && value < AvailableModels.Count) { SelectedModel = AvailableModels[value].Id; } } }
    public ObservableCollection<ProviderDisplayItem> AvailableProviders { get; }
    public int SelectedProviderIndex { get { for (int i = 0; i < AvailableProviders.Count; i++) { if (AvailableProviders[i].Name.Equals(_selectedProvider, StringComparison.OrdinalIgnoreCase)) return i; } return 0; } set { if (value >= 0 && value < AvailableProviders.Count) { SelectedProvider = AvailableProviders[value].Name; } } }
    public string ValidationStatus { get => _validationStatus; set => SetField(ref _validationStatus, value); }
    private string _pricingValidationStatus = string.Empty;
    public string PricingValidationStatus { get => _pricingValidationStatus; set => SetField(ref _pricingValidationStatus, value); }
    public bool HasApiKey => !string.IsNullOrEmpty(_apiKey);
    public decimal InputPricePer1K { get => _inputPricePer1K; set => SetField(ref _inputPricePer1K, value); }
    public decimal OutputPricePer1K { get => _outputPricePer1K; set => SetField(ref _outputPricePer1K, value); }
    public decimal TodayCost { get => _todayCost; set => SetField(ref _todayCost, value); }
    public decimal SessionCost { get => _sessionCost; set => SetField(ref _sessionCost, value); }
    public string Currency { get => _currency; set => SetField(ref _currency, value); }
    public string TodayCostDisplay => _pricingCalculator.FormatCost(_todayCost, _currency);
    public string SessionCostDisplay => _pricingCalculator.FormatCost(_sessionCost, _currency);
    public ICommand ToggleServiceCommand { get; }
    public ICommand CopyAddressCommand { get; }
    public ICommand SaveApiKeyCommand { get; }
    public ICommand ClearApiKeyCommand { get; }
    public ICommand SavePricingCommand { get; }
    public ICommand ResetPricingCommand { get; }

    private async Task ToggleServiceAsync() { /* implementation */ }
    private async Task StartForwardingServiceAsync() { _gatewayService = new LLMApiGatewayService(); await _gatewayService.StartProxyAsync(); }
    private async Task StopForwardingServiceAsync() { if (_gatewayService != null) { await _gatewayService.StopProxyAsync(); await _gatewayService.DisposeAsync(); _gatewayService = null; } }
    private void StartStatsPolling() { _statsPollingTimer?.Dispose(); _statsPollingTimer = new System.Timers.Timer(1000); _statsPollingTimer.Elapsed += async (s, e) => await PollStatsAsync(); _statsPollingTimer.Start(); }
    private void StopStatsPolling() { _statsPollingTimer?.Stop(); _statsPollingTimer?.Dispose(); _statsPollingTimer = null; }
    private async Task PollStatsAsync() { /* implementation */ }
    private async void CopyAddress() { try { if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null) await desktop.MainWindow.Clipboard.SetTextAsync(_listeningAddress); } catch { } }
    private void UpdateAvailableModels(string providerName) { AvailableModels.Clear(); var models = _providerManager.GetSupportedModels(providerName); foreach (var model in models) AvailableModels.Add(model); var defaultModel = _providerManager.GetDefaultModel(providerName); if (!string.IsNullOrEmpty(defaultModel)) { _selectedModel = defaultModel; OnPropertyChanged(nameof(SelectedModel)); OnPropertyChanged(nameof(SelectedModelIndex)); } else if (AvailableModels.Count > 0) { _selectedModel = AvailableModels[0].Id; OnPropertyChanged(nameof(SelectedModel)); OnPropertyChanged(nameof(SelectedModelIndex)); } }
    public bool ValidateApiKeyFormat(string provider, string apiKey) { if (string.IsNullOrWhiteSpace(apiKey)) return false; return provider.ToLowerInvariant() switch { "openai" => apiKey.StartsWith("sk-") && apiKey.Length >= 20, "anthropic" => apiKey.StartsWith("sk-ant-"), "minimax" => apiKey.StartsWith("sk-"), _ => apiKey.Length >= 10 }; }
    public void SaveApiKey() { try { if (string.IsNullOrWhiteSpace(ApiKey)) { ValidationStatus = Strings.Status_EnterAPIKey; return; } if (!ValidateApiKeyFormat(SelectedProvider, ApiKey)) { ValidationStatus = Strings.Status_InvalidAPIKey; return; } _secureStorage.SaveApiKey(SelectedProvider, ApiKey); OnPropertyChanged(nameof(HasApiKey)); ValidationStatus = Strings.Status_APIKeySaved; } catch (Exception ex) { ValidationStatus = $"{Strings.Status_SaveFailed}{ex.Message}"; } }
    public void ClearApiKey() { try { _secureStorage.DeleteApiKey(SelectedProvider); ApiKey = string.Empty; ValidationStatus = string.Empty; OnPropertyChanged(nameof(HasApiKey)); } catch { } }
    private void LoadSavedApiKey() { var savedKey = _secureStorage.GetApiKey(SelectedProvider); ApiKey = !string.IsNullOrEmpty(savedKey) ? savedKey : string.Empty; }
    private void LoadPricingForModel() { /* implementation */ }
    public void SavePricing() { /* implementation */ }
    public void ResetPricing() { /* implementation */ }
    public void RecordTokenUsage(int inputTokens, int outputTokens) { /* implementation */ }
    public void RefreshTodayCost() { /* implementation */ }
    private void LoadLastSelectedProvider() { /* implementation */ }
    private string? FindFirstConfiguredProvider() { foreach (var provider in _providerManager.Providers.Keys) { var key = _secureStorage.GetApiKey(provider); if (!string.IsNullOrEmpty(key)) return provider; } return null; }
    private void LoadLastSelectedModel() { /* implementation */ }
    private async Task LoadTokenUsageAsync() { /* implementation */ }
    private async Task SaveTokenUsageAsync() { /* implementation */ }
    private void UpdateBarChartHeights() { /* implementation */ }
    public void SaveTokenUsageSync() { /* implementation */ }
    public void Dispose() { if (_disposed) return; try { SaveTokenUsageSync(); StopStatsPolling(); TokenBarChartVM?.Dispose(); } catch { } _disposed = true; GC.SuppressFinalize(this); }
    public async ValueTask DisposeAsync() { if (_disposed) return; try { await SaveTokenUsageAsync(); TokenBarChartVM?.Dispose(); StopStatsPolling(); if (_isServiceRunning) { try { await StopForwardingServiceAsync(); } catch { } } } catch { } finally { _disposed = true; GC.SuppressFinalize(this); } }
    private bool _disposed = false;
    public bool IsDisposed => _disposed;
}