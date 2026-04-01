using System.Windows.Input;
using Avalonia.Controls;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Services;

public class TabNavigationService
{
    public System.Collections.ObjectModel.ObservableCollection<TabItemViewModel> Tabs { get; } = new();
    public TabItemViewModel? SelectedTab { get; private set; }
    public UserControl? CurrentTabContent { get; private set; }
    public int TabCount => Tabs.Count;
    public bool HasTabs => Tabs.Count > 0;
    public ICommand GoHomeCommand { get; }

    public TabNavigationService() { GoHomeCommand = new RelayCommand(_ => GoHome()); }

    public void SelectTab(TabItemViewModel? tab) { if (SelectedTab != tab) { SelectedTab = tab; CurrentTabContent = tab?.Content; } }
    public void AddTab(TabItemViewModel tab) { Tabs.Add(tab); SelectTab(tab); }
    public void GoHome() { var homeTab = System.Linq.Enumerable.FirstOrDefault(Tabs, t => t.Title == "Home"); if (homeTab != null) SelectTab(homeTab); }
    public void CloseTab(TabItemViewModel tab) { if (Tabs.Contains(tab)) { Tabs.Remove(tab); if (SelectedTab == tab) SelectTab(System.Linq.Enumerable.LastOrDefault(Tabs)); } }
}
