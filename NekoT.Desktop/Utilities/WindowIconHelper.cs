using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace NekoT.Desktop.Utilities;

public static class WindowIconHelper
{
    private static Icon? _cachedIcon;
    private static readonly object _lock = new();

    public static Icon? GetApplicationIcon()
    {
        lock (_lock)
        {
            if (_cachedIcon != null)
                return _cachedIcon;

            try
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    var iconName = $"{assembly.GetName().Name}.ico";
                    var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconName);

                    if (File.Exists(iconPath))
                    {
                        _cachedIcon = new Icon(iconPath);
                        return _cachedIcon;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load icon: {ex.Message}");
            }

            return null;
        }
    }

    public static void DisposeCachedIcon()
    {
        lock (_lock)
        {
            if (_cachedIcon != null)
            {
                _cachedIcon.Dispose();
                _cachedIcon = null;
            }
        }
    }
}