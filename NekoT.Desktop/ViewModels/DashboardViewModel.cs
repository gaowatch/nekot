using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using NekoT.Core.Billing;
using NekoT.Core.Hosting;
using NekoT.Core.Http;
using NekoT.Core.LlmProviders;
using NekoT.Core.Security;
using NekoT.Core.TokenManagement;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels;

public class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly TokenService _tokenService;
    private readonly LlmProviderManager _providerManager;
    private readonly HttpClient _httpClient;
    private readonly SecureStorage _secureStorage;
    private readonly CostEstimator _costEstimator;
    private readonly ServiceHostManager _hostManager;
    private bool _isServiceRunning;
    private string _serviceStatus = Strings.Status_NotConnected;
    private string _selectedProvider = "MiniMax-M2.5";
    private string _apiKey = string.Empty;
    private string _customUrl = string.Empty;
    private bool _useCustomUrl;
    private string _lastRequestResult = string.Empty;
    private decimal _estimatedCost;
    private bool _disposed;

    public DashboardViewModel()
    {
        _tokenService = new TokenService();
        _providerManager = LlmProviderManager.Instance;
        _httpClient = HttpClientManager.GetSharedClient();
        _secureStorage = new SecureStorage();
        _costEstimator = new CostEstimator();
        _hostManager = new ServiceHostManager();
        UsageRecords = new ObservableCollection<UsageDisplayRecord>();
        AvailableProviders = new ObservableCollection<ProviderDisplayItem>(
            _providerManager.Providers.Values.Select(p => new ProviderDisplayItem
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                ApiUrl = p.ApiUrl
            })
        );
        LoadSavedApiKey();
    }

    public TokenService TokenService => _tokenService;
    public LlmProviderManager ProviderManager => _providerManager;
    public SecureStorage SecureStorage => _secureStorage;
    public CostEstimator CostEstimator => _costEstimator;
    public ServiceHostManager HostManager => _hostManager;

    public ObservableCollection<UsageDisplayRecord> UsageRecords { get; }
    public ObservableCollection<ProviderDisplayItem> AvailableProviders { get; }

    public decimal EstimatedCost
    {
        get => _estimatedCost;
        set => SetField(ref _estimatedCost, value);
    }

    public int TotalTokens => _tokenService.TotalTokens;
    public int SessionTokens => _tokenService.SessionTokens;

    public bool IsServiceRunning
    {
        get => _isServiceRunning;
        set
        {
            if (SetField(ref _isServiceRunning, value))
            {
                ServiceStatus = value ? Strings.Status_Running : Strings.Status_NotConnected;
                OnPropertyChanged(nameof(TotalTokens));
                OnPropertyChanged(nameof(SessionTokens));
            }
        }
    }

    public string ServiceStatus
    {
        get => _serviceStatus;
        set => SetField(ref _serviceStatus, value);
    }

    public string SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetField(ref _selectedProvider, value))
            {
                LoadSavedApiKey();
            }
        }
    }

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

    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    public string CustomUrl
    {
        get => _customUrl;
        set => SetField(ref _customUrl, value);
    }

    public bool UseCustomUrl
    {
        get => _useCustomUrl;
        set => SetField(ref _useCustomUrl, value);
    }

    public string LastRequestResult
    {
        get => _lastRequestResult;
        set => SetField(ref _lastRequestResult, value);
    }

    public async Task TestApiConnection()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            LastRequestResult = Strings.API_EnterKey;
            return;
        }

        var provider = _providerManager.GetProviderByModel(SelectedProvider);
        if (provider == null)
        {
            LastRequestResult = Strings.API_ProviderNotFound;
            return;
        }

        var targetUrl = UseCustomUrl && !string.IsNullOrWhiteSpace(CustomUrl)
            ? CustomUrl
            : provider.ApiUrl;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);

            var payload = new
            {
                model = SelectedProvider,
                messages = new[] { new { role = "user", content = "hi" } }
            };

            request.Content = JsonContent.Create(payload);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(content);
                if (json.RootElement.TryGetProperty("usage", out var usage) &&
                    usage.TryGetProperty("total_tokens", out var totalTokens))
                {
                    _tokenService.RecordUsage(totalTokens.GetInt32(), provider.Name);
                    LastRequestResult = $"{Strings.API_SuccessToken} {totalTokens.GetInt32()}";
                    RefreshDisplay();
                    IsServiceRunning = true;
                }
                else
                {
                    LastRequestResult = Strings.API_NoUsageField;
                }
            }
            else
            {
                LastRequestResult = $"{Strings.Common_Error}: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            LastRequestResult = $"{Strings.Common_Error}: {ex.Message}";
        }
    }

    public void ResetSession()
    {
        _tokenService.ResetSession();
        OnPropertyChanged(nameof(TotalTokens));
        OnPropertyChanged(nameof(SessionTokens));
    }

    public void ClearRecords()
    {
        UsageRecords.Clear();
        OnPropertyChanged(nameof(TotalTokens));
        OnPropertyChanged(nameof(SessionTokens));
    }

    private void RefreshDisplay()
    {
        OnPropertyChanged(nameof(TotalTokens));
        OnPropertyChanged(nameof(SessionTokens));
        UpdateEstimatedCost();
    }

    private void UpdateEstimatedCost()
    {
        var breakdown = _tokenService.GetProviderBreakdown();
        decimal totalCost = 0;

        foreach (var kvp in breakdown)
        {
            var cost = _costEstimator.EstimateCost(kvp.Key, kvp.Value, kvp.Value / 2);
            if (cost.HasValue)
            {
                totalCost += cost.Value;
            }
        }

        EstimatedCost = totalCost;
    }

    public void SaveApiKey()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                _secureStorage.SaveApiKey(SelectedProvider, ApiKey);
                LastRequestResult = Strings.API_KeySaved;
            }
        }
        catch (Exception ex)
        {
            LastRequestResult = $"{Strings.Error_SaveFailed}{ex.Message}";
        }
    }

    private void LoadSavedApiKey()
    {
        var savedKey = _secureStorage.GetApiKey(SelectedProvider);
        if (!string.IsNullOrEmpty(savedKey))
        {
            ApiKey = savedKey;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
    }
}

public class ProviderDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}