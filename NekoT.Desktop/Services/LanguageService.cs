using System;
using System.Globalization;
using System.Threading;
using Avalonia;
using Avalonia.Data.Converters;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.Services;

public class LanguageService
{
    private static LanguageService? _instance;
    private static readonly object _lock = new();

    public static LanguageService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LanguageService();
                }
            }
            return _instance;
        }
    }

    private LanguageService()
    {
    }

    public event EventHandler? LanguageChanged;

    public void SwitchLanguage(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Strings.Culture = culture;

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Failed to switch language: {ex.Message}");
        }
    }

    public string GetCurrentLanguage()
    {
        return Thread.CurrentThread.CurrentUICulture.Name;
    }
}