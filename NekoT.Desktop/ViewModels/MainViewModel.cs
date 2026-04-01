using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Services;
using NekoT.Desktop.Utilities;

namespace NekoT.Desktop.ViewModels;

public enum ActiveViewMode
{
    Chat,
    Forwarding
}

public class MainViewModel : ViewModelBase, IDisposable
{
    private TabItemViewModel? _selectedTab;
    private bool _isLogoMode;
    private bool _isSidePanelOpen;
    private string _title = Strings.Main_Title;
    private double _availableWidth = 800;
    private bool _disposed;
    private readonly ObservableCollection<TabItemViewModel> _tabs = new();
    private readonly TabOverflowManager _tabOverflowManager = new();
    private readonly ForwardingServiceViewModel _forwardingServiceViewModel;
    private readonly ChatViewModel _chatViewModel;
    private readonly SidePanelViewModel _sidePanelViewModel;
    private readonly ObservableCollection<TabItemViewModel> _visibleTabs = new();
    private readonly ObservableCollection<TabItemViewModel> _overflowTabs = new();n
    public event EventHandler<TabItemViewModel>? TabAdded;
    public event EventHandler<TabItemViewModel>? TabRemoved;
    public event EventHandler<TabItemViewModel>? TabSelected;

    public ObservableCollection<TabItemViewModel> Tabs => _tabs;
    public ObservableCollection<TabItemViewModel> VisibleTabs => _visibleTabs;
    public ObservableCollection<TabItemViewModel> OverflowTabs => _overflowTabs;

    public TabItemViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetField(ref _selectedTab, value) && value != null)
            {
                SelectTab(value);
            }
        }
    }

    public bool IsLogoMode
    {
        get => _isLogoMode;
        set => SetField(ref _isLogoMode, value);
    }

    public bool IsSidePanelOpen
    {
        get => _isSidePanelOpen;
        set => SetField(ref _isSidePanelOpen, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public double AvailableWidth
    {
        get => _availableWidth;
        set
        {
            if (SetField(ref _availableWidth, value))
 {
                RecalculateVisibleTabs();
            }
        }
    }

    public ForwardingServiceViewModel ForwardingService => _forwardingServiceViewModel;
    public ChatViewModel ChatViewModel => _chatViewModel;
    public SidePanelViewModel SidePanelViewModel => _sidePanelViewModel;

    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand ToggleLogoModeCommand { get; }
    public ICommand ToggleSidePanelCommand { get; }
    public ICommand GoHomeCommand { get; }

    public MainViewModel()
    {
        _forwardingServiceViewModel = new ForwardingServiceViewModel();
        _chatViewModel = new ChatViewModel();
        _sidePanelViewModel = new SidePanelViewModel();

        AddTabCommand = new RelayCommand(_ => AddNewTab());
        CloseTabCommand = new RelayCommand(param => CloseTab(param as TabItemViewModel));
        ToggleLogoModeCommand = new RelayCommand(_ => ToggleLogoMode());
        ToggleSidePanelCommand = new RelayCommand(_ => ToggleSidePanel());
        GoHomeCommand = new RelayCommand(_ => GoHome());

        _sidePanelViewModel.BrowserSettingsChanged += OnBrowserSettingsChanged;

        AddDefaultTabs();
    }

    private void AddDefaultTabs()
    {
        var homeTab = new TabItemViewModel
        {
            Title = Strings.Tab_Home,
            Content = new Views.HomeView { DataContext = new HomeViewModel() },
            TabType = "home",
            CanClose = false
        };

        var chatTab = new TabItemViewModel
        {
            Title = Strings.Tab_AIChat,
            Content = new Views.ChatView { DataContext = _chatViewModel },
            TabType = "chat"
        };

        homeTab.Closed += OnTabClosed;
        chatTab.Closed += OnTabClosed;
        homeTab.Selected += OnTabSelected;
        chatTab.Selected += OnTabSelected;

        _tabs.Add(homeTab);
        _tabs.Add(chatTab);

        SelectTab(homeTab);
        RecalculateVisibleTabs();

        TabAdded?.Invoke(this, homeTab);
        TabAdded?.Invoke(this, chatTab);
    }

    public void UpdateAvailableWidth(double width)
    {
        AvailableWidth = width;
    }

    private void RecalculateVisibleTabs()
    {
        var result = _tabOverflowManager.CalculateVisibleTabs(_tabs, AvailableWidth);

        Dispatcher.UIThread.Post(() =>
        {
            VisibleTabs.Clear();
            OverflowTabs.Clear();

            foreach (var tab in result.VisibleTabs)
            {
                VisibleTabs.Add(tab);
            }

            foreach (var tab in result.OverflowTabs)
            {
                OverflowTabs.Add(tab);
            }
        });
    }

    public void AddNewTab()
    {
        var newTab = new TabItemViewModel
        {
            Title = $"{Strings.Tab_NewTab} {_tabs.Count + 1}",
            Content = new Views.HomeView { DataContext = new HomeViewModel() },
            TabType = "browser"
        };

        newTab.Closed += OnTabClosed;
        newTab.Selected += OnTabSelected;

        _tabs.Add(newTab);
        SelectTab(newTab);
        RecalculateVisibleTabs();

        TabAdded?.Invoke(this, newTab);
    }

    public void CloseTab(TabItemViewModel? tab)
    {
        if (tab == null || !tab.CanClose) return;

        tab.Closed -= OnTabClosed;
        tab.Selected -= OnTabSelected;

        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        if (SelectedTab == tab)
        {
            var newSelectedIndex = Math.Min(index, _tabs.Count - 1);
            if (newSelectedIndex >= 0)
            {
                SelectTab(_tabs[newSelectedIndex]);
            }
        }

        RecalculateVisibleTabs();
        TabRemoved?.Invoke(this, tab);
    }

    private void OnTabClosed(object? sender, EventArgs e)
    {
        if (sender is TabItemViewModel tab)
        {
            CloseTab(tab);
        }
    }

    private void OnTabSelected(object? sender, EventArgs e)
    {
        if (sender is TabItemViewModel tab)
        {
            SelectTab(tab);
        }
    }

    private void SelectTab(TabItemViewModel tab)
    {
        foreach (var t in _tabs)
        {
            t.SetIsSelectedSilent(t == tab);
        }

        _selectedTab = tab;
        OnPropertyChanged(nameof(SelectedTab));
        TabSelected?.Invoke(this, tab);
    }

    public void ToggleLogoMode()
    {
        IsLogoMode = !IsLogoMode;
    }

    public void ToggleSidePanel()
    {
        IsSidePanelOpen = !IsSidePanelOpen;
    }

    public void GoHome()
    {
        var homeTab = _tabs.FirstOrDefault(t => !t.CanClose && t.TabType == "home");
        if (homeTab != null)
        {
            SelectTab(homeTab);
        }
    }

    private void OnBrowserSettingsChanged(object? sender, EventArgs e)
    {
        var homeVm = _sidePanelViewModel;
        if (homeVm == null) return;

        var homeTab = _tabs.FirstOrDefault(t => t.TabType == "home");
        if (homeTab?.Content is Views.HomeView homeView && homeView.DataContext is HomeViewModel vm)
        {
            vm.SearchQuery = string.Empty;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sidePanelViewModel.BrowserSettingsChanged -= OnBrowserSettingsChanged;

        foreach (var tab in _tabs)
        {
            tab.Closed -= OnTabClosed;
            tab.Selected -= OnTabSelected;
        }

        _forwardingServiceViewModel.Dispose();
        _chatViewModel.Dispose();

        GC.SuppressFinalize(this);
    }
}