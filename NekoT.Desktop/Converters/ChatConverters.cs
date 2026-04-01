using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NekoT.Desktop.Converters;

public class MessageBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role.ToLowerInvariant() switch
            {
                "assistant" => new SolidColorBrush(Color.Parse("#E8F5E9")),
                "user" => new SolidColorBrush(Color.Parse("#E3F2FD")),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RoleDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role.ToLowerInvariant() switch
            {
                "assistant" => "Assistant",
                "user" => "User",
                _ => role
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Avalonia.Controls.Visibility.Visible : Avalonia.Controls.Visibility.Collapsed;
        }
        return Avalonia.Controls.Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TimestampConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime timestamp)
        {
            return timestamp.ToString("HH:mm:ss");
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
