using System;
using NekoT.Desktop.Services.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using NekoT.Core.Browser;
using NekoT.Core.LlmProviders;
using NekoT.Desktop.NetworkMonitoring;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.Views;
using NekoT.Desktop.ViewModels;
using NekoT.Core.Forwarding;
using NekoT.Models.Responses;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels;

public enum AppLogoMode
{
    Browser,
    Chat
}

public partial class MainViewModel : ViewModelBase
{
    private const int MaxUsageRecords = 1000;

    private readonly BrowserTabManager _tabManager;
    private readonly ChatForwardingService _forwardingService;
    private readonly ForwardingServiceViewModel _forwardingServiceViewModel;
    private readonly TabOverflowManager _overflowManager;
    private bool _isServiceRunning;
    private TabItemViewModel? _selectedTab;
    private UserControl? _currentTabContent;
    private AppLogoMode _logoMode = AppLogoMode.Browser;
    private string _uploadSpeedText = "0 B/s";
    private string _downloadSpeedText = "0 B/s";
    private double _availableWidth = 800;
    private TabOverflowResult? _cachedOverflowResult;
    private double _lastCalculatedWidth = -1;
    
    private readonly Dictionary<TabItemViewModel, BrowserTabEventHandlers> _tabEventHandlers = new();
    
    private HomeViewModel? _homeViewModel;
    private HomeView? _homeView;

    public MainViewModel(
        BrowserTabManager tabManager,
        ChatForwardingService forwardingService,
        ChatViewModel chatViewModel,
        SidePanelViewModel sidePanelViewModel)
    {
        _tabManager = tabManager;
        _forwardingService = forwardingService;
        _overflowManager = new TabOverflowManager();
        Tabs = new ObservableCollection<TabItemViewModel>();
        UsageRecords = new ObservableCollection<UsageDisplayRecord>();

        ChatViewModel = chatViewModel;
        SidePanelViewModel = sidePanelViewModel;
        _forwardingServiceViewModel = chatViewModel.ForwardingService;

        ChatViewModel.MessageSent += OnChatMessageSent;
        ChatViewModel.AIServicesToggleRequested += OnAIServicesToggleRequested;
        SidePanelViewModel.ModelChanged += OnModelChanged;

        ChatViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ChatViewModel.ActiveView))
                OnPropertyChanged(nameof(IsSidePanelVisible));
        };

        SidePanelViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SidePanelViewModel.IsOpen))
                OnPropertyChanged(nameof(IsSidePanelVisible));
        };

        UserSettingsService.Instance.PropertyChanged += OnUserSettingsPropertyChanged;

        AddBrowserTab = new RelayCommand(_ => CreateBrowserTab());
        AddChatTab = new RelayCommand(_ => CreateChatTab());
        LogoClickCommand = new RelayCommand(_ => ToggleLogoMode());
        GoHomeCommand = new RelayCommand(_ => GoHome());
        NavigateBackCommand = new RelayCommand(_ => NavigateBack(), _ => CanNavigateBack);
        NavigateForwardCommand = new RelayCommand(_ => NavigateForward(), _ => CanNavigateForward);
        NavigateRefreshCommand = new RelayCommand(_ => NavigateRefresh());
        ScrollLeftCommand = new RelayCommand(_ => ScrollLeft(), _ => CanScrollLeft);
        ScrollRightCommand = new RelayCommand(_ => ScrollRight(), _ => CanScrollRight);
        SelectOverflowTabCommand = new RelayCommand(tab => SelectOverflowTab(tab as TabItemViewModel));

        SidePanelViewModel.IsForwardingConnected = false;

        UpdateChatModelDisplay();

        CreateInitialTabs();
        
        var startupPanel = UserSettingsService.Instance.StartupPanel;
        if (startupPanel == "token-monitor" && ChatTab != null)
        {
            SelectedTab = ChatTab;
            LogoMode = AppLogoMode.Chat;
        }
        else if (HomeTab != null)
        {
            SelectedTab = HomeTab;
        }
    }

    public ChatViewModel ChatViewModel { get; }
    public SidePanelViewModel SidePanelViewModel { get; }
    public ForwardingServiceViewModel ForwardingService => _forwardingServiceViewModel;

    public bool IsSidePanelVisible =>
        LogoMode == AppLogoMode.Chat && 
        ChatViewModel.ActiveView == ActiveViewMode.Chat && 
        SidePanelViewModel.IsOpen;

    public ObservableCollection<TabItemViewModel> Tabs { get; }
    public ObservableCollection<UsageDisplayRecord> UsageRecords { get; }

    public TabItemViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != value)
            {
                if (_selectedTab != null)
                {
                    _selectedTab.SetIsSelectedSilent(false);
                }
                
                if (SetField(ref _selectedTab, value))
                {
                    if (value != null)
                    {
                        value.SetIsSelectedSilent(true);
                    }
                    CurrentTabContent = value?.Content;
                    OnPropertyChanged(nameof(CanNavigateBack));
                    OnPropertyChanged(nameof(CanNavigateForward));
                }
            }
        }
    }

    public UserControl? CurrentTabContent
    {
        get => _currentTabContent;
        set => SetField(ref _currentTabContent, value);
    }

    public int TabCount => Tabs.Count;

    public bool HasTabs => Tabs.Count > 0;

    private TabOverflowResult GetOrCreateOverflowResult()
    {
        if (_cachedOverflowResult == null || Math.Abs(_lastCalculatedWidth - _availableWidth) > 0.1)
        {
            _cachedOverflowResult = _overflowManager.CalculateVisibleTabs(Tabs, _availableWidth);
            _lastCalculatedWidth = _availableWidth;
        }
        return _cachedOverflowResult;
    }

    public IReadOnlyList<TabItemViewModel> VisibleTabs
    {
        get
        {
            return GetOrCreateOverflowResult().VisibleTabs;
        }
    }

    public IReadOnlyList<TabItemViewModel> OverflowTabs
    {
        get
        {
            return GetOrCreateOverflowResult().OverflowTabs;
        }
    }

    public bool HasOverflowTabs
    {
        get
        {
            return GetOrCreateOverflowResult().HasOverflow;
        }
    }

    private double _scrollOffset;
    public double ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            if (SetField(ref _scrollOffset, value))
            {
                OnPropertyChanged(nameof(CanScrollLeft));
                OnPropertyChanged(nameof(CanScrollRight));
                (ScrollLeftCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ScrollRightCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanScrollLeft => _scrollOffset > 0;

    public bool CanScrollRight => HasOverflowTabs;

    public void ScrollLeft()
    {
        ScrollOffset = Math.Max(0, ScrollOffset - 150);
    }

    public void ScrollRight()
    {
        ScrollOffset += 150;
    }

    public void SelectOverflowTab(TabItemViewModel? tab)
    {
        if (tab == null) return;
        SelectTab(tab);
    }

    public bool IsServiceRunning
    {
        get => _isServiceRunning;
        set => SetField(ref _isServiceRunning, value);
    }

    public bool ShowTokenMonitor
    {
        get => UserSettingsService.Instance.ShowTokenMonitor;
        set
        {
            if (UserSettingsService.Instance.ShowTokenMonitor != value)
            {
                UserSettingsService.Instance.ShowTokenMonitor = value;
                OnPropertyChanged();
            }
        }
    }

    public string UploadSpeedText
    {
        get => _uploadSpeedText;
        set => SetField(ref _uploadSpeedText, value);
    }

    public string DownloadSpeedText
    {
        get => _downloadSpeedText;
        set => SetField(ref _downloadSpeedText, value);
    }

    public AppLogoMode LogoMode
    {
        get => _logoMode;
        set
        {
            if (SetField(ref _logoMode, value))
            {
                OnPropertyChanged(nameof(LogoDisplayText));
                OnPropertyChanged(nameof(IsBrowserMode));
                OnPropertyChanged(nameof(IsChatMode));
                OnPropertyChanged(nameof(IsSidePanelVisible));
            }
        }
    }

    public string LogoDisplayText => _logoMode == AppLogoMode.Browser ? "NekoT" : "TokeN";

    public bool IsBrowserMode => _logoMode == AppLogoMode.Browser;
    public bool IsChatMode => _logoMode == AppLogoMode.Chat;

    public ICommand LogoClickCommand { get; }

    public ICommand AddBrowserTab { get; }
    public ICommand AddChatTab { get; }
    public ICommand GoHomeCommand { get; }
    public ICommand NavigateBackCommand { get; }
    public ICommand NavigateForwardCommand { get; }
    public ICommand NavigateRefreshCommand { get; }
    public ICommand ScrollLeftCommand { get; }
    public ICommand ScrollRightCommand { get; }
    public ICommand SelectOverflowTabCommand { get; }

    public void ToggleLogoMode()
    {
        if (HomeTab == null || ChatTab == null) return;

        var newMode = LogoMode == AppLogoMode.Browser ? AppLogoMode.Chat : AppLogoMode.Browser;
        var targetTab = newMode == AppLogoMode.Browser ? HomeTab : ChatTab;

        SelectedTab = targetTab;
        LogoMode = newMode;
    }

    public void GoHome()
    {
        if (HomeTab == null) return;
        
        SelectedTab = HomeTab;
        LogoMode = AppLogoMode.Browser;
    }

    public bool CanNavigateBack
    {
        get
        {
            if (SelectedTab?.Content is BrowserTabView browserView)
            {
                return browserView.DataContext is BrowserTabViewModel vm && vm.CanGoBack;
            }
            return false;
        }
    }

    public bool CanNavigateForward
    {
        get
        {
            if (SelectedTab?.Content is BrowserTabView browserView)
            {
                return browserView.DataContext is BrowserTabViewModel vm && vm.CanGoForward;
            }
            return false;
        }
    }

    public void NavigateBack()
    {
        if (SelectedTab?.Content is BrowserTabView browserView)
        {
            if (browserView.DataContext is BrowserTabViewModel vm)
            {
                vm.GoBack();
            }
        }
    }

    public void NavigateForward()
    {
        if (SelectedTab?.Content is BrowserTabView browserView)
        {
            if (browserView.DataContext is BrowserTabViewModel vm)
            {
                vm.GoForward();
            }
        }
    }

    public void NavigateRefresh()
    {
        if (SelectedTab?.Content is BrowserTabView browserView)
        {
            if (browserView.DataContext is BrowserTabViewModel vm)
            {
                vm.Refresh();
            }
        }
    }

    private void CreateInitialTabs()
    {
        CreateHomeTab();
        CreateChatTab();
    }

    public TabItemViewModel? HomeTab { get; private set; }
    public TabItemViewModel? ChatTab { get; private set; }

    private void CreateHomeTab()
    {
        _homeViewModel = new HomeViewModel();
        _homeView = new HomeView();
        _homeView.SetViewModel(_homeViewModel);

        HomeTab = new TabItemViewModel
        {
            Title = Strings.Tab_Home,
            Content = _homeView,
            TabType = "home",
            CanClose = false
        };

        _homeViewModel.NavigateRequested += OnHomeNavigateRequested;
        _homeView.NavigationRequested += OnHomeNavigateRequested;
        
        HomeTab.Selected += (s, e) => SelectTab(HomeTab);
        
        Tabs.Add(HomeTab);
    }
    
    private void OnHomeNavigateRequested(object? sender, string url)
    {
        OnHomeViewNavigateRequested(url);
    }

    private void CreateChatTab()
    {
        var view = new ChatTabView { DataContext = ChatViewModel };

        ChatTab = new TabItemViewModel
        {
            Title = Strings.Tab_AIChat,
            Content = view,
            TabType = "chat",
            CanClose = false
        };
        
        ChatTab.Selected += (s, e) => SelectTab(ChatTab);
        
        Tabs.Add(ChatTab);
    }

    private void OnHomeViewNavigateRequested(string url)
    {
        var viewModel = new BrowserTabViewModel();
        var view = new BrowserTabView { DataContext = viewModel };

        var tab = new TabItemViewModel
        {
            Title = Strings.Tab_Loading,
            Content = view,
            TabType = "browser",
            CanClose = true
        };

        var handlers = new BrowserTabEventHandlers
        {
            Tab = tab,
            ViewModel = viewModel,
            ClosedHandler = (s, e) => RemoveTab(tab),
            SelectedHandler = (s, e) => SelectTab(tab),
            TokenDetectedHandler = (s, e) => OnBrowserTabTokenDetected(e),
            TrafficDetectedHandler = (s, e) => OnBrowserTabTrafficDetected(e),
            PropertyChangedHandler = (s, e) => OnBrowserTabPropertyChanged(tab, viewModel, e)
        };
        
        _tabEventHandlers[tab] = handlers;

        tab.Closed += handlers.ClosedHandler;
        tab.Selected += handlers.SelectedHandler;
        viewModel.TokenDetected += handlers.TokenDetectedHandler;
        viewModel.TrafficDetected += handlers.TrafficDetectedHandler;
        viewModel.PropertyChanged += handlers.PropertyChangedHandler;

        Tabs.Add(tab);
        SelectTab(tab);
        OnPropertyChanged(nameof(TabCount));
        OnPropertyChanged(nameof(HasTabs));

        viewModel.NavigateTo(url);
    }

    private void CreateBrowserTab()
    {
        var homePage = UserSettingsService.Instance.HomePage;
        
        if (!string.IsNullOrEmpty(homePage) && homePage != "about:blank")
        {
            OnHomeViewNavigateRequested(homePage);
            return;
        }
        
        var viewModel = new BrowserTabViewModel();
        var view = new BrowserTabView { DataContext = viewModel };

        var tab = new TabItemViewModel
        {
            Title = Strings.Tab_Browser,
            Content = view,
            TabType = "browser",
            CanClose = true
        };

        var handlers = new BrowserTabEventHandlers
        {
            Tab = tab,
            ViewModel = viewModel,
            ClosedHandler = (s, e) => RemoveTab(tab),
            SelectedHandler = (s, e) => SelectTab(tab),
            TokenDetectedHandler = (s, e) => OnBrowserTabTokenDetected(e),
            TrafficDetectedHandler = (s, e) => OnBrowserTabTrafficDetected(e),
            PropertyChangedHandler = (s, e) => OnBrowserTabPropertyChanged(tab, viewModel, e)
        };
        
        _tabEventHandlers[tab] = handlers;

        tab.Closed += handlers.ClosedHandler;
        tab.Selected += handlers.SelectedHandler;
        viewModel.TokenDetected += handlers.TokenDetectedHandler;
        viewModel.TrafficDetected += handlers.TrafficDetectedHandler;
        viewModel.PropertyChanged += handlers.PropertyChangedHandler;

        Tabs.Add(tab);
        SelectedTab = tab;
        OnPropertyChanged(nameof(TabCount));
        OnPropertyChanged(nameof(HasTabs));
    }
    
    private void OnBrowserTabTokenDetected(TokenExtractedEventArgs e)
    {
        if (e.IsAuthExtraction)
        {
            LoggerService.Instance.LogDebug("Token", $"Auth extracted: Type={e.TokenType}, Provider={e.Provider}");
        }
        else
        {
            RecordUsage(e.Tokens, e.Provider);
        }
    }
    
    private void OnBrowserTabTrafficDetected(TrafficStatsEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UploadSpeedText = e.UploadSpeedFormatted;
            DownloadSpeedText = e.DownloadSpeedFormatted;
        });
    }
    
    private void OnBrowserTabPropertyChanged(TabItemViewModel tab, BrowserTabViewModel viewModel, PropertyChangedEventArgs e)
    {
        if (tab == null || viewModel == null || e == null) return;
        
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnBrowserTabPropertyChanged(tab, viewModel, e));
            return;
        }
        
        switch (e.PropertyName)
        {
            case nameof(BrowserTabViewModel.Title):
                tab.Title = viewModel.Title ?? Strings.Tab_NewTab;
                break;
            case nameof(BrowserTabViewModel.CanGoBack):
            case nameof(BrowserTabViewModel.CanGoForward):
                if (SelectedTab == tab)
                {
                    OnPropertyChanged(nameof(CanNavigateBack));
                    OnPropertyChanged(nameof(CanNavigateForward));
                    (NavigateBackCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (NavigateForwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                break;
        }
    }

    public void SelectTab(TabItemViewModel tab)
    {
        if (tab == null) return;

        SelectedTab = tab;
        
        var targetMode = tab.TabType == "chat" ? AppLogoMode.Chat : AppLogoMode.Browser;
        if (LogoMode != targetMode)
        {
            LogoMode = targetMode;
        }
    }

    private void UpdateLogoDisplay()
    {
        AppLogoMode newMode;
        if (SelectedTab?.TabType == "home" || SelectedTab?.TabType == "browser")
            newMode = AppLogoMode.Browser;
        else
            newMode = AppLogoMode.Chat;

        if (LogoMode != newMode)
        {
            LogoMode = newMode;
        }
    }

    private void RemoveTab(TabItemViewModel tab)
    {
        if (_tabEventHandlers.TryGetValue(tab, out var handlers))
        {
            tab.Closed -= handlers.ClosedHandler;
            tab.Selected -= handlers.SelectedHandler;
            handlers.ViewModel.TokenDetected -= handlers.TokenDetectedHandler;
            handlers.ViewModel.TrafficDetected -= handlers.TrafficDetectedHandler;
            handlers.ViewModel.PropertyChanged -= handlers.PropertyChangedHandler;
            handlers.ViewModel?.Dispose();
            _tabEventHandlers.Remove(tab);
        }
        
        Tabs.Remove(tab);
        OnPropertyChanged(nameof(TabCount));
        OnPropertyChanged(nameof(HasTabs));

        if (Tabs.Count == 0)
        {
            SelectedTab = HomeTab;
            _logoMode = AppLogoMode.Browser;
            OnPropertyChanged(nameof(LogoMode));
            OnPropertyChanged(nameof(LogoDisplayText));
            OnPropertyChanged(nameof(IsBrowserMode));
            OnPropertyChanged(nameof(IsChatMode));
        }
        else if (SelectedTab == tab)
        {
            SelectedTab = HomeTab;
            LogoMode = AppLogoMode.Browser;
        }
    }

    private void OnChatMessageSent(object? sender, string message)
    {
        _ = HandleChatMessageAsync(message).ContinueWith(
            task =>
            {
                if (task.Exception != null)
                {
                    LoggerService.Instance.LogError("Forwarding", "Unhandled exception in OnChatMessageSent", task.Exception);
                    ChatViewModel.IsSending = false;
                }
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }
    
    private async Task HandleChatMessageAsync(string message)
    {
        try
        {
            LoggerService.Instance.LogInfo("Forwarding", $"OnChatMessageSent called: {message}");
            ChatViewModel.IsSending = true;

            var (response, usage) = await ForwardMessageAsync(message);

            LoggerService.Instance.LogInfo("Forwarding", $"Response received, content length: {response?.Length ?? 0}");
            LoggerService.Instance.LogInfo("Forwarding", $"Usage: TotalTokens={usage?.TotalTokens}");

            ChatViewModel.AddAssistantMessage(response);

            if (usage != null)
            {
                LoggerService.Instance.LogInfo("Forwarding", $"Recording usage: input={usage.PromptTokens}, output={usage.CompletionTokens}, total={usage.TotalTokens}");
                RecordUsage(usage.PromptTokens, usage.CompletionTokens, SidePanelViewModel.SelectedProvider);
            }
        }
        catch (Exception ex)
        {
            LoggerService.Instance.LogError("Forwarding", "Error", ex);
            ChatViewModel.AddAssistantMessage($"{Strings.Common_Error}: {ex.Message}");
        }
        finally
        {
            ChatViewModel.IsSending = false;
        }
    }

    private void OnAIServicesToggleRequested(object? sender, EventArgs e)
    {
        if (SidePanelViewModel != null)
        {
            SidePanelViewModel.TogglePanel();
        }
    }

    private async System.Threading.Tasks.Task<(string content, Usage? usage)> ForwardMessageAsync(string message)
    {
        var apiKey = SidePanelViewModel.ApiKey;
        var providerName = SidePanelViewModel.SelectedProvider ?? "openai";
        var model = SidePanelViewModel.SelectedModel;

        if (string.IsNullOrEmpty(model))
        {
            model = LlmProviderManager.Instance.GetDefaultModel(providerName) ?? "gpt-4o-mini";
        }

        LoggerService.Instance.LogInfo("Forwarding", $"ForwardMessageAsync: provider={providerName}, model={model}, apiKey={(!string.IsNullOrEmpty(apiKey) ? Strings.API_KeyConfigured : Strings.API_KeyNotConfigured)}");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (Strings.API_ConfigureFirst, null);
        }

        try
        {
            var chatMessages = new List<NekoT.Core.Forwarding.ChatMessage>();

            foreach (var msg in ChatViewModel.Messages)
            {
                chatMessages.Add(new NekoT.Core.Forwarding.ChatMessage(msg.Role, msg.Content));
            }

            chatMessages.Add(new NekoT.Core.Forwarding.ChatMessage("user", message));

            var request = _forwardingService.CreateRequest(
                model: model,
                apiKey: apiKey,
                messages: chatMessages
            );

            LoggerService.Instance.LogInfo("Forwarding", "Sending request to API...");
            var response = await _forwardingService.SendMessageAsync(request);
            LoggerService.Instance.LogInfo("Forwarding", $"Response received: Content length={response.Content?.Length}, Usage={response.Usage?.TotalTokens}");

            SidePanelViewModel.IsForwardingConnected = true;

            return (response.Content, response.Usage);
        }
        catch (UnauthorizedAccessException ex)
        {
            LoggerService.Instance.LogError("Forwarding", "Unauthorized", ex);
            SidePanelViewModel.IsForwardingConnected = false;
            return ($("{Strings.Error_AccessDenied} {ex.Message}", null);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            LoggerService.Instance.LogError("Forwarding", "HTTP error", ex);
            SidePanelViewModel.IsForwardingConnected = false;
            return ($("{Strings.Error_NetworkFailed} {ex.Message}", null);
        }
        catch (Exception ex)
        {
            LoggerService.Instance.LogError("Forwarding", "Exception", ex);
            SidePanelViewModel.IsForwardingConnected = false;
            return ($("{Strings.Error_ForwardingFailed} {ex.Message}", null);
        }
    }

    private int _totalTokens = 0;

    public int TotalTokens
    {
        get => Interlocked.Add(ref _totalTokens, 0);
        set
        {
            Interlocked.Exchange(ref _totalTokens, value);
            OnPropertyChanged(nameof(TotalTokens));
        }
    }

    public void RecordUsage(int totalTokens, string? provider = null)
    {
        RecordUsage(totalTokens, 0, provider);
    }

    public void RecordUsage(int inputTokens, int outputTokens, string? provider = null)
    {
        var totalTokens = inputTokens + outputTokens;
        
        var newTotal = Interlocked.Add(ref _totalTokens, totalTokens);
        
        SidePanelViewModel.AtomicAddTotalTokens(totalTokens);
        SidePanelViewModel.AtomicAddSessionTokens(totalTokens);
        ChatViewModel.AddTokens(totalTokens);
        
        _forwardingServiceViewModel.RecordTokenUsage(inputTokens, outputTokens);
        
        var record = new UsageDisplayRecord
        {
            Tokens = totalTokens,
            Timestamp = DateTime.Now,
            Provider = provider ?? "Unknown"
        };
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            UsageRecords.Insert(0, record);
            while (UsageRecords.Count > MaxUsageRecords)
            {
                UsageRecords.RemoveAt(UsageRecords.Count - 1);
            }
        });
        
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(TotalTokens));
        });
    }

    private void OnUserSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserSettingsService.ShowTokenMonitor))
        {
            OnPropertyChanged(nameof(ShowTokenMonitor));
        }
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        UpdateChatModelDisplay();
    }

    private void UpdateChatModelDisplay()
    {
        var apiKey = SidePanelViewModel.ApiKey;
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ChatViewModel.CurrentModel = Strings.Model_SelectModelAndAPI;
            return;
        }

        var provider = LlmProviderManager.Instance.GetProvider(SidePanelViewModel.SelectedProvider);
        var modelId = SidePanelViewModel.SelectedModel;

        if (provider != null && !string.IsNullOrEmpty(modelId))
        {
            var modelInfo = provider.SupportedModels.FirstOrDefault(m => m.Id == modelId);
            if (modelInfo != null)
            {
                ChatViewModel.CurrentModel = $"{provider.DisplayName} - {modelInfo.DisplayName}";
            }
            else
            {
                ChatViewModel.CurrentModel = $"{provider.DisplayName} - {modelId}";
            }
        }
        else
        {
            ChatViewModel.CurrentModel = Strings.Model_SelectModel;
        }
    }

    public void StartService() => IsServiceRunning = true;
    public void StopService() => IsServiceRunning = false;

    public void UpdateAvailableWidth(double width)
    {
        if (width < 0)
            throw new ArgumentException("Width cannot be negative", nameof(width));

        if (Math.Abs(_availableWidth - width) > 0.1)
        {
            _availableWidth = width;
            _cachedOverflowResult = null;
            OnPropertyChanged(nameof(VisibleTabs));
            OnPropertyChanged(nameof(OverflowTabs));
            OnPropertyChanged(nameof(HasOverflowTabs));
        }
    }
}

public class UsageDisplayRecord
{
    public int Tokens { get; set; }
    public DateTime Timestamp { get; set; }
    public string Provider { get; set; } = "Unknown";
}

internal class BrowserTabEventHandlers
{
    public TabItemViewModel Tab { get; set; } = null!;
    public BrowserTabViewModel ViewModel { get; set; } = null!;
    public EventHandler ClosedHandler { get; set; } = null!;
    public EventHandler SelectedHandler { get; set; } = null!;
    public EventHandler<TokenExtractedEventArgs> TokenDetectedHandler { get; set; } = null!;
    public EventHandler<TrafficStatsEventArgs> TrafficDetectedHandler { get; set; } = null!;
    public PropertyChangedEventHandler PropertyChangedHandler { get; set; } = null!;
}