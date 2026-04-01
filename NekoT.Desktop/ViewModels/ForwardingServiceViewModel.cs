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

    public ForwardingServiceViewModel() : this(new SecureStorage(), LlmProviderManager.Instance)
    {
    }

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
        
        AvailableProviders = new ObservableCollection<ProviderDisplayItem>(
            _providerManager.Providers.Values.Select(p => new ProviderDisplayItem
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                Alias = p.Alias,
                ApiUrl = p.ApiUrl
            })
        );
        
        LoadLastSelectedProvider();
        UpdateAvailableModels(_selectedProvider);
        LoadSavedApiKey();
        LoadLastSelectedModel();
        RefreshTodayCost();
        
        _ = LoadTokenUsageAsync();
    }

    private void LoadLastSelectedProvider()
    {
        var lastProvider = UserSettingsService.Instance.LastSelectedProvider;
        
        if (!string.IsNullOrEmpty(lastProvider) && _providerManager.Providers.ContainsKey(lastProvider))
        {
            _selectedProvider = lastProvider;
            return;
        }

        var configuredProvider = FindFirstConfiguredProvider();
        if (!string.IsNullOrEmpty(configuredProvider))
        {
            _selectedProvider = configuredProvider;
            return;
        }

        _selectedProvider = "openai";
    }

    private string? FindFirstConfiguredProvider()
    {
        foreach (var provider in _providerManager.Providers.Keys)
        {
            var key = _secureStorage.GetApiKey(provider);
            if (!string.IsNullOrEmpty(key))
            {
                return provider;
            }
        }
        return null;
    }

    private void LoadLastSelectedModel()
    {
        var lastModel = UserSettingsService.Instance.LastSelectedModel;
        
        if (!string.IsNullOrEmpty(lastModel) && AvailableModels.Any(m => m.Id == lastModel))
        {
            _selectedModel = lastModel;
            OnPropertyChanged(nameof(SelectedModel));
            OnPropertyChanged(nameof(SelectedModelIndex));
        }
    }

    public bool IsServiceRunning
    {
        get => _isServiceRunning;
        set => SetField(ref _isServiceRunning, value);
    }

    public string ListeningAddress => _listeningAddress;

    public int TodayRequestCount
    {
        get => _todayRequestCount;
        set => SetField(ref _todayRequestCount, value);
    }

    public int TodayTokenCount
    {
        get => _todayTokenCount;
        set => SetField(ref _todayTokenCount, value);
    }

    private int _latestTokenCount;
    public int LatestTokenCount
    {
        get => _latestTokenCount;
        set => SetField(ref _latestTokenCount, value);
    }

    public int CurrentConnectionCount
    {
        get => _currentConnectionCount;
        set => SetField(ref _currentConnectionCount, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    public string SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetField(ref _selectedProvider, value))
            {
                UserSettingsService.Instance.LastSelectedProvider = value;
                LoadSavedApiKey();
                UpdateAvailableModels(value);
                LoadPricingForModel();
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
                UserSettingsService.Instance.LastSelectedModel = value;
                LoadPricingForModel();
            }
        }
    }

    public ObservableCollection<ModelDisplayItem> AvailableModels { get; } = new();

    public int SelectedModelIndex
    {
        get
        {
            for (int i = 0; i < AvailableModels.Count; i++)
            {
                if (AvailableModels[i].Id == _selectedModel)
                    return i;
            }
            return 0;
        }
        set
        {
            if (value >= 0 && value < AvailableModels.Count)
            {
                SelectedModel = AvailableModels[value].Id;
            }
        }
    }

    public ObservableCollection<ProviderDisplayItem> AvailableProviders { get; }

    public int SelectedProviderIndex
    {
        get
        {
            for (int i = 0; i < AvailableProviders.Count; i++)
            {
                if (AvailableProviders[i].Name.Equals(_selectedProvider, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }
        set
        {
            if (value >= 0 && value < AvailableProviders.Count)
            {
                SelectedProvider = AvailableProviders[value].Name;
            }
        }
    }

    public string ValidationStatus
    {
        get => _validationStatus;
        set => SetField(ref _validationStatus, value);
    }

    private string _pricingValidationStatus = string.Empty;
    public string PricingValidationStatus
    {
        get => _pricingValidationStatus;
        set => SetField(ref _pricingValidationStatus, value);
    }

    public bool HasApiKey => !string.IsNullOrEmpty(_apiKey);

    public decimal InputPricePer1K
    {
        get => _inputPricePer1K;
        set => SetField(ref _inputPricePer1K, value);
    }

    public decimal OutputPricePer1K
    {
        get => _outputPricePer1K;
        set => SetField(ref _outputPricePer1K, value);
    }

    public decimal TodayCost
    {
        get => _todayCost;
        set => SetField(ref _todayCost, value);
    }

    public decimal SessionCost
    {
        get => _sessionCost;
        set => SetField(ref _sessionCost, value);
    }

    public string Currency
    {
        get => _currency;
        set => SetField(ref _currency, value);
    }

    public string TodayCostDisplay => _pricingCalculator.FormatCost(_todayCost, _currency);
    public string SessionCostDisplay => _pricingCalculator.FormatCost(_sessionCost, _currency);

    public ICommand ToggleServiceCommand { get; }
    public ICommand CopyAddressCommand { get; }
    public ICommand SaveApiKeyCommand { get; }
    public ICommand ClearApiKeyCommand { get; }
    public ICommand SavePricingCommand { get; }
    public ICommand ResetPricingCommand { get; }

    private async Task ToggleServiceAsync()
    {
        if (!_isServiceRunning)
        {
            try
            {
                await StartForwardingServiceAsync();
                IsServiceRunning = true;
                _lastRecordedTotalTokens = 0;
                StartStatsPolling();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForwardingService] 启动失败: {ex.Message}");
                await StopForwardingServiceAsync();
                IsServiceRunning = false;
            }
        }
        else
        {
            await StopForwardingServiceAsync();
            StopStatsPolling();
            IsServiceRunning = false;
        }
    }

    private async Task StartForwardingServiceAsync()
    {
        _gatewayService = new LLMApiGatewayService();
        await _gatewayService.StartProxyAsync();
    }

    private async Task StopForwardingServiceAsync()
    {
        try
        {
            if (_gatewayService != null)
            {
                await _gatewayService.StopProxyAsync();
                await _gatewayService.DisposeAsync();
                _gatewayService = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ForwardingService] 停止失败: {ex.Message}");
        }
    }

    private void StartStatsPolling()
    {
        _statsPollingTimer?.Dispose();
        _statsPollingTimer = new System.Timers.Timer(1000);
        _statsPollingTimer.Elapsed += async (s, e) => await PollStatsAsync();
        _statsPollingTimer.Start();
    }

    private void StopStatsPolling()
    {
        _statsPollingTimer?.Stop();
        _statsPollingTimer?.Dispose();
        _statsPollingTimer = null;
    }

    private async Task PollStatsAsync()
    {
        if (_gatewayService?.IsRunning != true)
        {
            return;
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await client.GetStringAsync(StatsUrl);
            var stats = JsonSerializer.Deserialize<ProxyStatsResponse>(response);

            if (stats != null)
            {
                var currentTotal = stats.total_tokens;
                var delta = currentTotal - _lastRecordedTotalTokens;

                if (delta > 0 && delta < 1000000)
                {
                    var inputTokens = Math.Max(0, stats.total_input_tokens - (_lastRecordedTotalTokens / 2));
                    var outputTokens = Math.Max(0, stats.total_output_tokens - (_lastRecordedTotalTokens / 2));

                    if (inputTokens > 0 || outputTokens > 0)
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            RecordTokenUsage(inputTokens, outputTokens);
                        });
                    }

                    _lastRecordedTotalTokens = currentTotal;
                }
                else if (delta < 0)
                {
                    _lastRecordedTotalTokens = currentTotal;
                }
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    private class ProxyStatsResponse
    {
        public int total_requests { get; set; }
        public int total_input_tokens { get; set; }
        public int total_output_tokens { get; set; }
        public int total_tokens { get; set; }
        public double uptime_seconds { get; set; }
    }

    private async void CopyAddress()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(_listeningAddress);
                }
            }
        }
        catch
        {
        }
    }

    private void UpdateAvailableModels(string providerName)
    {
        AvailableModels.Clear();
        var models = _providerManager.GetSupportedModels(providerName);
        foreach (var model in models)
        {
            AvailableModels.Add(model);
        }

        var defaultModel = _providerManager.GetDefaultModel(providerName);
        if (!string.IsNullOrEmpty(defaultModel))
        {
            _selectedModel = defaultModel;
            OnPropertyChanged(nameof(SelectedModel));
            OnPropertyChanged(nameof(SelectedModelIndex));
        }
        else if (AvailableModels.Count > 0)
        {
            _selectedModel = AvailableModels[0].Id;
            OnPropertyChanged(nameof(SelectedModel));
            OnPropertyChanged(nameof(SelectedModelIndex));
        }
    }

    public bool ValidateApiKeyFormat(string provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

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
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                ValidationStatus = Strings.Status_EnterAPIKey;
                return;
            }

            if (!ValidateApiKeyFormat(SelectedProvider, ApiKey))
            {
                ValidationStatus = Strings.Status_InvalidAPIKey;
                return;
            }

            _secureStorage.SaveApiKey(SelectedProvider, ApiKey);
            OnPropertyChanged(nameof(HasApiKey));
            ValidationStatus = Strings.Status_APIKeySaved;
        }
        catch (Exception ex)
        {
            ValidationStatus = $"{Strings.Status_SaveFailed}{ex.Message}";
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ForwardingService] 清除API密钥失败：{ex.Message}");
        }
    }

    private void LoadSavedApiKey()
    {
        var savedKey = _secureStorage.GetApiKey(SelectedProvider);
        if (!string.IsNullOrEmpty(savedKey))
        {
            ApiKey = savedKey;
        }
        else
        {
            ApiKey = string.Empty;
        }
    }

    private void LoadPricingForModel()
    {
        if (string.IsNullOrEmpty(_selectedModel))
            return;

        var pricing = PricingStorage.Instance.GetModelPricing(_selectedModel);
        if (pricing != null)
        {
            InputPricePer1K = pricing.InputPricePer1K;
            OutputPricePer1K = pricing.OutputPricePer1K;
            Currency = pricing.Currency;
        }
        else
        {
            var defaultPricing = ModelPricing.GetDefaultPricing(_selectedModel);
            InputPricePer1K = defaultPricing.InputPricePer1K;
            OutputPricePer1K = defaultPricing.OutputPricePer1K;
            Currency = defaultPricing.Currency;
        }

        OnPropertyChanged(nameof(InputPricePer1K));
        OnPropertyChanged(nameof(OutputPricePer1K));
        OnPropertyChanged(nameof(Currency));
    }

    public void SavePricing()
    {
        if (string.IsNullOrEmpty(_selectedModel))
            return;

        var pricing = new ModelPricing
        {
            ModelId = _selectedModel,
            InputPricePer1K = _inputPricePer1K,
            OutputPricePer1K = _outputPricePer1K,
            Currency = _currency,
            EffectiveDate = DateTime.Now
        };

        PricingStorage.Instance.SaveModelPricing(_selectedModel, pricing);
        PricingValidationStatus = Strings.Forwarding_Pricing_SavePricing;
    }

    public void ResetPricing()
    {
        if (string.IsNullOrEmpty(_selectedModel))
            return;

        var defaultPricing = ModelPricing.GetDefaultPricing(_selectedModel);
        InputPricePer1K = defaultPricing.InputPricePer1K;
        OutputPricePer1K = defaultPricing.OutputPricePer1K;
        Currency = defaultPricing.Currency;

        OnPropertyChanged(nameof(InputPricePer1K));
        OnPropertyChanged(nameof(OutputPricePer1K));
        OnPropertyChanged(nameof(Currency));
        PricingValidationStatus = Strings.Forwarding_Pricing_ResetPricing;
    }

    public void RecordTokenUsage(int inputTokens, int outputTokens)
    {
        var modelId = string.IsNullOrEmpty(_selectedModel) ? "forwarding" : _selectedModel;
        
        var cost = _pricingCalculator.CalculateCost(modelId, inputTokens, outputTokens);
        
        var totalTokens = inputTokens + outputTokens;
        LatestTokenCount = totalTokens;
        SessionCost += cost.TotalCost;
        
        _pricingCalculator.RecordUsage(ServiceType, modelId, inputTokens, outputTokens);
        
        RefreshTodayCost();
        
        OnPropertyChanged(nameof(SessionCostDisplay));
        OnPropertyChanged(nameof(TodayCostDisplay));
    }

    public void RefreshTodayCost()
    {
        var summary = _pricingCalculator.GetTodaySummary(ServiceType);
        TodayCost = summary.TotalCost;
        TodayRequestCount = summary.RequestCount;
        TodayTokenCount = summary.TotalInputTokens + summary.TotalOutputTokens;
        
        OnPropertyChanged(nameof(TodayCost));
        OnPropertyChanged(nameof(TodayCostDisplay));
        OnPropertyChanged(nameof(TodayRequestCount));
        OnPropertyChanged(nameof(TodayTokenCount));
    }

    #region IDisposable & IAsyncDisposable

    private bool _disposed = false;
    
    public bool IsDisposed => _disposed;

    public void SaveTokenUsageSync()
    {
        if (_disposed) return;
        try
        {
            var data = new TokenUsageData
            {
                LatestTokenCount = LatestTokenCount,
                TodayTokenCount = TodayTokenCount,
                TodayRequestCount = TodayRequestCount,
                LastSavedTime = DateTime.Now
            };

            if (TokenBarChartVM != null)
            {
                foreach (var point in TokenBarChartVM.BarDataPoints)
                {
                    data.BarDataPoints.Add(new BarDataPointInfo { Value = point.Value, Timestamp = point.Timestamp });
                }
            }

            _ = _tokenUsageStorage.SaveAsync(data);
        }
        catch (Exception ex)
 {
            Debug.WriteLine($"[ForwardingServiceViewModel] 保存 Token 使用数据失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            SaveTokenUsageSync();
            StopStatsPolling();
            TokenBarChartVM?.Dispose();

            if (_isServiceRunning && _gatewayService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gatewayService.DisposeAsync();
                    }
                    catch { }
                });
            }
        }
        catch { }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            await SaveTokenUsageAsync();
            TokenBarChartVM?.Dispose();
            StopStatsPolling();

            if (_isServiceRunning)
            {
                try
                {
                    await StopForwardingServiceAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ForwardingServiceViewModel] Error stopping gateway: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ForwardingServiceViewModel] DisposeAsync error: {ex.Message}");
        }
        finally
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    #endregion

    #region Token Usage Persistence

    private async Task LoadTokenUsageAsync()
    {
        try
        {
            var data = await _tokenUsageStorage.LoadAsync();

            TodayTokenCount = data.TodayTokenCount;
            TodayRequestCount = data.TodayRequestCount;
            LatestTokenCount = data.LatestTokenCount;

            if (TokenBarChartVM != null && data.BarDataPoints.Count > 0)
            {
                foreach (var pointData in data.BarDataPoints)
                {
                    var point = new BarDataPoint
                    {
                        Value = pointData.Value,
                        Timestamp = pointData.Timestamp,
                        Height = 4.0
                    };
                    TokenBarChartVM.BarDataPoints.Add(point);
                }

                UpdateBarChartHeights();
            }

            OnPropertyChanged(nameof(TodayTokenCount));
            OnPropertyChanged(nameof(TodayRequestCount));
            OnPropertyChanged(nameof(LatestTokenCount));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ForwardingServiceViewModel] 加载 Token 使用数据失败: {ex.Message}");
        }
    }

    private async Task SaveTokenUsageAsync()
    {
        try
        {
            var data = new TokenUsageData
            {
                LatestTokenCount = LatestTokenCount,
                TodayTokenCount = TodayTokenCount,
                TodayRequestCount = TodayRequestCount,
                LastSavedTime = DateTime.Now
            };

            if (TokenBarChartVM != null)
            {
                foreach (var point in TokenBarChartVM.BarDataPoints)
                {
                    data.BarDataPoints.Add(new BarDataPointInfo { Value = point.Value, Timestamp = point.Timestamp });
                }
            }

            await _tokenUsageStorage.SaveAsync(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ForwardingServiceViewModel] 保存 Token 使用数据失败: {ex.Message}");
        }
    }

    private void UpdateBarChartHeights()
    {
        if (TokenBarChartVM == null || TokenBarChartVM.BarDataPoints.Count == 0)
            return;

        int maxValue = 0;
        foreach (var point in TokenBarChartVM.BarDataPoints)
        {
            if (point.Value > maxValue)
                maxValue = point.Value;
        }

        if (TokenBarChartVM != null)
        {
            TokenBarChartVM.UpdateMaxTokenValueFromExternal(maxValue);
        }

        const double MaxBarHeight = 200.0;
        const double MinBarHeight = 4.0;

        foreach (var point in TokenBarChartVM.BarDataPoints)
        {
            point.Height = maxValue > 0
                ? Math.Max(MinBarHeight, (double)point.Value / maxValue * MaxBarHeight)
                : MinBarHeight;
        }
    }

    #endregion
}