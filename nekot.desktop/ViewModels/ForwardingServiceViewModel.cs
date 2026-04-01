using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NekoT.Core.Forwarding;
using NekoT.Core.Http;
using NekoT.Core.TokenManagement;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels;

public class ForwardingServiceViewModel : ViewModelBase, IDisposable
{
    private ForwardingService? _forwardingService;
    private readonly TokenService _tokenService;
    private readonly HttpClient _httpClient;
    private readonly ServiceHostManager _hostManager;
    private bool _isRunning;
    private string _statusText = Strings.Status_NotConnected;
    private string _listenAddress = "http://127.0.0.1:18888";
    private int _todayRequestCount;
    private int _todayTokenCount;
    private int _currentConnections;
    private string _lastError = string.Empty;
    private bool _disposed;

    public ForwardingServiceViewModel()
    {
        _tokenService = new TokenService();
        _httpClient = HttpClientManager.GetSharedClient();
        _hostManager = new ServiceHostManager();
        
        StartCommand = new RelayCommand(async _ => await StartServiceAsync(), _ => !IsRunning);
        StopCommand = new RelayCommand(async _ => await StopServiceAsync(), _ => IsRunning);
        CopyAddressCommand = new RelayCommand(_ => CopyListenAddress());
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetField(ref _isRunning, value))
            {
                StatusText = value ? Strings.Status_Running : Strings.Status_NotConnected;
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
            }
        }
    }

    public bool CanStart => !IsRunning;
    public bool CanStop => IsRunning;

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string ListenAddress
    {
        get => _listenAddress;
        set => SetField(ref _listenAddress, value);
    }

    public int TodayRequestCount
    {
        get => _todayRequestCount;
        private set => SetField(ref _todayRequestCount, value);
    }

    public int TodayTokenCount
    {
        get => _todayTokenCount;
        private set => SetField(ref _todayTokenCount, value);
    }

    public int CurrentConnectionCount => _currentConnections;

    public string LastError
    {
        get => _lastError;
        private set => SetField(ref _lastError, value);
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand CopyAddressCommand { get; }

    public event EventHandler<string>? AddressCopied;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<int>? ConnectionCountChanged;

    private async Task StartServiceAsync()
    {
        if (_forwardingService != null)
        {
            await StopServiceAsync();
        }

        try
        {
            _forwardingService = new ForwardingService();
            _forwardingService.RequestProcessed += OnRequestProcessed;
            _forwardingService.ConnectionChanged += OnConnectionChanged;
            
            await _forwardingService.StartAsync(ListenAddress);
            IsRunning = true;
            LastError = string.Empty;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            ErrorOccurred?.Invoke(this, ex.Message);
            System.Diagnostics.Debug.WriteLine($"[ForwardingServiceVM] Start failed: {ex}");
        }
    }

    private async Task StopServiceAsync()
    {
        try
        {
            if (_forwardingService != null)
            {
                _forwardingService.RequestProcessed -= OnRequestProcessed;
                _forwardingService.ConnectionChanged -= OnConnectionChanged;
                await _forwardingService.StopAsync();
                _forwardingService = null;
            }
            IsRunning = false;
            _currentConnections = 0;
            OnPropertyChanged(nameof(CurrentConnectionCount));
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            ErrorOccurred?.Invoke(this, ex.Message);
            System.Diagnostics.Debug.WriteLine($"[ForwardingServiceVM] Stop failed: {ex}");
        }
    }

    private void OnRequestProcessed(object? sender, ForwardingRequestEventArgs e)
    {
        Interlocked.Increment(ref _todayRequestCount);
        Interlocked.Add(ref _todayTokenCount, e.Tokens);
        OnPropertyChanged(nameof(TodayRequestCount));
        OnPropertyChanged(nameof(TodayTokenCount));
    }

    private void OnConnectionChanged(object? sender, int count)
    {
        Interlocked.Exchange(ref _currentConnections, count);
        ConnectionCountChanged?.Invoke(this, count);
        OnPropertyChanged(nameof(CurrentConnectionCount));
    }

    private void CopyListenAddress()
    {
        try
        {
            if (_forwardingService != null)
            {
                var url = _forwardingService.GetListeningUrl();
                if (!string.IsNullOrEmpty(url))
                {
                    System.Windows.Clipboard.SetText(url);
                    AddressCopied?.Invoke(this, url);
                }
            }
        }
        catch
        {
        }
    }

    public void SaveTokenUsageSync()
    {
        _tokenService.SaveUsageData();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        StopServiceAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}

public class ForwardingServiceConfig : ViewModelBase
{
    private string _listenAddress = "http://127.0.0.1:18888";
    private bool _enableCORS = true;
    private int _maxConcurrentConnections = 100;

    public string ListenAddress
    {
        get => _listenAddress;
        set => SetField(ref _listenAddress, value);
    }

    public bool EnableCORS
    {
        get => _enableCORS;
        set => SetField(ref _enableCORS, value);
    }

    public int MaxConcurrentConnections
    {
        get => _maxConcurrentConnections;
        set => SetField(ref _maxConcurrentConnections, value);
    }
}