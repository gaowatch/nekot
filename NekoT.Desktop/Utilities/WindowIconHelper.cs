using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace NekoT.Desktop.Utilities;

public static class WindowIconHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_DLGMODALFRAME = 0x0001;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint WM_SETICON = 0x0080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int width, int height, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public static void RemoveIcon(Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        window.Opened += (sender, e) =>
        {
            try
            {
                RemoveIconInternal(window);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"移除窗口图标失败: {ex.Message}");
            }
        };
    }

    private static void RemoveIconInternal(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;

        if (handle == null || handle == IntPtr.Zero)
            return;

        IntPtr hwnd = handle.Value;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (extendedStyle == 0)
            return;

        int newStyle = extendedStyle | WS_EX_DLGMODALFRAME;
        SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        SendMessage(hwnd, WM_SETICON, IntPtr.Zero, IntPtr.Zero);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    public static void RestoreIcon(Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        window.Opened += (sender, e) =>
        {
            try
            {
                RestoreIconInternal(window);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复窗口图标失败: {ex.Message}");
            }
        };
    }

    private static void RestoreIconInternal(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;

        if (handle == null || handle == IntPtr.Zero)
            return;

        IntPtr hwnd = handle.Value;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (extendedStyle == 0)
            return;

        int newStyle = extendedStyle & ~WS_EX_DLGMODALFRAME;
        SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }
}