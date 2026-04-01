using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using NekoT.Desktop.Utilities;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Services;

public class TabNavigationService
{
    public ObservableCollection<TabItemViewModel> Tabs { get; }
    public TabItemViewModel? SelectedTab { get; private set; }
    public UserControl? CurrentTabContent { get; private set; }
    public int TabCount => Tabs.Count;
    public bool HasTabs => Tabs.Count > 0;

    public ICommand GoHomeCommand { get; }

    public event Action<TabItemViewModel?>? SelectedTabChanged;
    public event Action<UserControl?>? CurrentTabContentChanged;

    public TabNavigationService()
    {
        Tabs = new ObservableCollection<TabItemViewModel>();
        GoHomeCommand = new RelayCommand(_ => GoHome());
    }

    public void SelectTab(TabItemViewModel? tab)
    {
        if (SelectedTab != tab)
        {
            SelectedTab = tab;
            CurrentTabContent = tab?.Content;
            SelectedTabChanged?.Invoke(tab);
            CurrentTabContentChanged?.Invoke(tab?.Content);
        }
    }

    public void AddTab(TabItemViewModel tab)
    {
        Tabs.Add(tab);
        SelectTab(tab);
    }

    public void GoHome()
    {
        var homeTab = Tabs.FirstOrDefault(t => t.Title == "Home");
        if (homeTab != null)
        {
            SelectTab(homeTab);
        }
    }

    public void CloseTab(TabItemViewModel tab)
    {
        if (Tabs.Contains(tab))
        {
            Tabs.Remove(tab);
            if (SelectedTab == tab)
            {
                SelectTab(Tabs.LastOrDefault());
            }
        }
    }
}