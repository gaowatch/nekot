using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NekoT.Desktop.Utilities;

public static class SystemFeaturesHelper
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public static bool IsConsoleVisible()
    {
        var handle = GetConsoleWindow();
        return handle != IntPtr.Zero && ShowWindow(handle, SW_SHOW);
    }

    public static void ShowConsole()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_SHOW);
        }
    }

    public static void HideConsole()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }

    public static bool IsDebuggerAttached()
    {
        return Debugger.IsAttached;
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }
}