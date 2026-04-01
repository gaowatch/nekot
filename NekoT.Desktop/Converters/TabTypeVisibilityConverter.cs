using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NekoT.Desktop.Converters;

public class TabTypeVisibilityConverter : IValueConverter
{
    private static readonly HashSet<string> HiddenTabTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "home",
        "chat"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string tabType || string.IsNullOrWhiteSpace(tabType))
        {
            return true;
        }

        return !HiddenTabTypes.Contains(tabType);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}