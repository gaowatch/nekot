using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Layout;
using ViewModels = NekoT.Desktop.ViewModels;
using Res = NekoT.Desktop.Resources.Strings;

namespace NekoT.Desktop.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUser && isUser) return new SolidColorBrush(Color.Parse("#007ACC"));
        return new SolidColorBrush(Color.Parse("#3C3C3C"));
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class MessageAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUser && isUser) return HorizontalAlignment.Right;
        return HorizontalAlignment.Left;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class RoleDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role) return role switch { "user" => Res.Converter_User, "assistant" => Res.Converter_AI, _ => role };
        return value;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool isOpen ? 1.0 : 0.0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool isOpen ? 320 : 0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TabBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected) return new SolidColorBrush(Color.Parse("#2D2D30"));
        return new SolidColorBrush(Color.Parse("Transparent"));
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToSuccessBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected && isConnected) return new SolidColorBrush(Color.Parse("#4CAF50"));
        return new SolidColorBrush(Color.Parse("#F44336"));
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StringNotEmptyToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !string.IsNullOrWhiteSpace(value as string);
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool isRunning ? isRunning ? Res.Converter_Running : Res.Converter_Stopped : Res.Converter_Stopped;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool isRunning ? isRunning ? Res.Converter_StopService : Res.Converter_StartService : Res.Converter_StartService;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EnumToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ActiveViewToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ViewModels.ActiveViewMode mode) return mode == ViewModels.ActiveViewMode.Forwarding ? Res.Converter_ForwardingService : Res.Converter_ProxyService;
        return Res.Converter_Unknown;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class CountToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count && parameter is string paramStr && int.TryParse(paramStr, out int targetCount)) return count == targetCount;
        return false;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class MessageCountConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is int count) return string.Format(Res.ChatTab_MessagesFormat, count);
        return values[0]?.ToString() ?? string.Empty;
    }
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}