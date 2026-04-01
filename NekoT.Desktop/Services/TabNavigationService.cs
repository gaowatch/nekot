using System;

namespace NekoT.Desktop.Services;

public interface ITabNavigationService
{
    void NavigateTo(string tabId);
    void NavigateBack();
    void NavigateForward();
    bool CanNavigateBack { get; }
    bool CanNavigateForward { get; }
}

public class TabNavigationService : ITabNavigationService
{
    private readonly TabItemViewModel _currentTab;

    public TabNavigationService(TabItemViewModel currentTab)
    {
        _currentTab = currentTab ?? throw new ArgumentNullException(nameof(currentTab));
    }

    public void NavigateTo(string tabId)
    {
    }

    public void NavigateBack()
    {
    }

    public void NavigateForward()
    {
    }

    public bool CanNavigateBack => false;
    public bool CanNavigateForward => false;
}