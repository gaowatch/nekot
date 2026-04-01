using System.Collections.Generic;
using System.Linq;

namespace NekoT.Desktop.Services;

public class TabOverflowManager
{
    private const double TabWidth = 120;
    private const double TabOverlap = 40;

    public TabOverflowResult CalculateVisibleTabs(IEnumerable<object> tabs, double availableWidth)
    {
        var tabList = tabs.ToList();
        var visibleTabs = new List<object>();
        var overflowTabs = new List<object>();

        if (tabList.Count == 0)
        {
            return new TabOverflowResult(visibleTabs, overflowTabs);
        }

        var effectiveTabWidth = TabWidth - TabOverlap;
        var maxVisibleTabs = System.Math.Max(1, (int)(availableWidth / effectiveTabWidth));

        if (tabList.Count <= maxVisibleTabs)
        {
            visibleTabs.AddRange(tabList);
            return new TabOverflowResult(visibleTabs, overflowTabs);
        }

        for (int i = 0; i < tabList.Count; i++)
        {
            if (i < maxVisibleTabs - 1)
            {
                visibleTabs.Add(tabList[i]);
            }
            else if (i == maxVisibleTabs - 1)
            {
                visibleTabs.Add(tabList[i]);
            }
            else
            {
                overflowTabs.Add(tabList[i]);
            }
        }

        return new TabOverflowResult(visibleTabs, overflowTabs);
    }

    public TabOverflowResult CalculateVisibleTabs<T>(IEnumerable<T> tabs, double availableWidth)
    {
        var tabList = tabs.ToList();
        var visibleTabs = new List<T>();
        var overflowTabs = new List<T>();

        if (tabList.Count == 0)
        {
            return new TabOverflowResult(visibleTabs, overflowTabs);
        }

        var effectiveTabWidth = TabWidth - TabOverlap;
        var maxVisibleTabs = System.Math.Max(1, (int)(availableWidth / effectiveTabWidth));

        if (tabList.Count <= maxVisibleTabs)
        {
            visibleTabs.AddRange(tabList);
            return new TabOverflowResult(visibleTabs, overflowTabs);
        }

        for (int i = 0; i < tabList.Count; i++)
        {
            if (i < maxVisibleTabs - 1)
            {
                visibleTabs.Add(tabList[i]);
            }
            else if (i == maxVisibleTabs - 1)
            {
                visibleTabs.Add(tabList[i]);
            }
            else
            {
                overflowTabs.Add(tabList[i]);
            }
        }

        return new TabOverflowResult(visibleTabs, overflowTabs);
    }
}

public class TabOverflowResult
{
    public IReadOnlyList<object> VisibleTabs { get; }
    public IReadOnlyList<object> OverflowTabs { get; }
    public bool HasOverflow => OverflowTabs.Count > 0;

    public TabOverflowResult(IReadOnlyList<object> visibleTabs, IReadOnlyList<object> overflowTabs)
    {
        VisibleTabs = visibleTabs;
        OverflowTabs = overflowTabs;
    }
}

public class TabOverflowResult<T> : TabOverflowResult
{
    public new IReadOnlyList<T> VisibleTabs { get; }
    public new IReadOnlyList<T> OverflowTabs { get; }

    public TabOverflowResult(IReadOnlyList<T> visibleTabs, IReadOnlyList<T> overflowTabs)
        : base(visibleTabs, overflowTabs)
    {
        VisibleTabs = visibleTabs;
        OverflowTabs = overflowTabs;
    }
}