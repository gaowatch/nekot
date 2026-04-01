using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NekoT.Desktop.Services;

namespace NekoT.Desktop.Utilities;

public static class SystemFeaturesHelper
{
    private const string AppName = "NekoT";
    private static readonly string AppPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

    public static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            if (enable) key.SetValue(AppName, $"\"{AppPath}\"");
            else key.DeleteValue(AppName, false);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SystemFeaturesHelper] SetAutoStart failed: {ex.Message}"); }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            if (key == null) return false;
            return key.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    public static void ApplyStartupSettings()
    {
        var settings = UserSettingsService.Instance;
        var currentAutoStart = IsAutoStartEnabled();
        if (settings.StartWithWindows != currentAutoStart) SetAutoStart(settings.StartWithWindows);
    }
}