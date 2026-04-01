using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NekoT.Desktop.ViewModels;

namespace NekoT.Desktop.Services;

public class TabOverflowResult
{
    public IReadOnlyList<TabItemViewModel> VisibleTabs { get; }
    public IReadOnlyList<TabItemViewModel> OverflowTabs { get; }
    public bool HasOverflow => OverflowTabs.Count > 0;

    public TabOverflowResult(
        IReadOnlyList<TabItemViewModel> visibleTabs,
        IReadOnlyList<TabItemViewModel> overflowTabs)
    {
        VisibleTabs = visibleTabs ?? throw new ArgumentNullException(nameof(visibleTabs));
        OverflowTabs = overflowTabs ?? throw new ArgumentNullException(nameof(overflowTabs));
    }
}

public class TabOverflowManager
{
    private const double MinTabWidth = 100;
    private const double MaxTabWidth = 200;
    private const double TabPadding = 8;
    private const double OverflowButtonWidth = 40;
    private const double CharWidthEstimate = 8;

    public TabOverflowResult CalculateVisibleTabs(
        ObservableCollection<TabItemViewModel> tabs,
        double availableWidth)
    {
        if (tabs == null)
            throw new ArgumentNullException(nameof(tabs));

        if (availableWidth < 0)
            throw new ArgumentException("Available width cannot be negative", nameof(availableWidth));

        if (tabs.Count == 0)
        {
            return new TabOverflowResult(
                Array.Empty<TabItemViewModel>(),
                Array.Empty<TabItemViewModel>());
        }

        if (availableWidth == 0)
        {
            return new TabOverflowResult(
                new List<TabItemViewModel> { tabs[0] },
                tabs.Skip(1).ToList());
        }

        var fixedTabs = tabs.Where(t => !t.CanClose).ToList();
        var closableTabs = tabs.Where(t => t.CanClose).ToList();

        var fixedTabsWidth = fixedTabs.Sum(t => CalculateTabWidth(t.Title));
        var effectiveWidth = availableWidth - fixedTabsWidth;

        var (visibleClosable, overflowClosable) = DistributeTabs(closableTabs, effectiveWidth, false);

        if (overflowClosable.Count > 0)
        {
            effectiveWidth = availableWidth - fixedTabsWidth - OverflowButtonWidth;
            (visibleClosable, overflowClosable) = DistributeTabs(closableTabs, effectiveWidth, true);
        }

        var visibleTabs = new List<TabItemViewModel>();
        visibleTabs.AddRange(fixedTabs);
        visibleTabs.AddRange(visibleClosable);

        return new TabOverflowResult(visibleTabs, overflowClosable.ToList());
    }

    private (List<TabItemViewModel> visible, List<TabItemViewModel> overflow) DistributeTabs(
        IEnumerable<TabItemViewModel> tabs,
        double availableWidth,
        bool reserveOverflowSpace)
    {
        var visible = new List<TabItemViewModel>();
        var overflow = new List<TabItemViewModel>();
        var remainingWidth = availableWidth;

        foreach (var tab in tabs)
        {
            var tabWidth = CalculateTabWidth(tab.Title);

            if (remainingWidth >= tabWidth + TabPadding)
            {
                visible.Add(tab);
                remainingWidth -= (tabWidth + TabPadding);
            }
            else
            {
                overflow.Add(tab);
            }
        }

        return (visible, overflow);
    }

    private double CalculateTabWidth(string title)
    {
        if (string.IsNullOrEmpty(title))
            return MinTabWidth;

        var estimatedWidth = title.Length * CharWidthEstimate + TabPadding * 2;
        return Math.Max(MinTabWidth, Math.Min(MaxTabWidth, estimatedWidth));
    }
}