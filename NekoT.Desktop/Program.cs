using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using NekoT.Desktop.Resources;
using NekoT.Desktop.Services;

namespace NekoT.Desktop;

class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        try { NekoT.Desktop.Services.GlobalExceptionHandler.Instance.InitializeEarly(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Program] Failed to initialize exception handler early: {ex.Message}"); }

        if (args.Contains("--restart")) { System.Diagnostics.Debug.WriteLine("[Program] Restart mode detected, waiting for old instance to exit..."); Thread.Sleep(2000); }
        if (!EnsureSingleInstance()) return;
        try { InitializeLanguage(); BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
        finally { ReleaseSingleInstance(); }
    }

    private static bool EnsureSingleInstance()
    {
        const string mutexName = @"Global\NekoT_SingleInstance_8B8D8D90-1234-5678-ABCD-123456789ABC";
        _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew) { System.Diagnostics.Debug.WriteLine("[Program] Another instance is already running, exiting..."); var existingProcess = FindExistingProcess(); if (existingProcess != null) BringWindowToFront(existingProcess); _singleInstanceMutex?.Dispose(); _singleInstanceMutex = null; return false; }
        System.Diagnostics.Debug.WriteLine("[Program] Single instance acquired"); return true;
    }

    private static System.Diagnostics.Process? FindExistingProcess() { var currentProcess = System.Diagnostics.Process.GetCurrentProcess(); var processName = currentProcess.ProcessName; foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName)) { if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero) return process; } return null; }

    private static void BringWindowToFront(System.Diagnostics.Process process) { try { if (process.MainWindowHandle != IntPtr.Zero) { NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.SW_RESTORE); NativeMethods.SetForegroundWindow(process.MainWindowHandle); } } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Program] Failed to bring window to front: {ex.Message}"); } }

    private static void ReleaseSingleInstance() { if (_singleInstanceMutex != null) { try { _singleInstanceMutex.ReleaseMutex(); } catch (ApplicationException) { } finally { _singleInstanceMutex.Dispose(); _singleInstanceMutex = null; } } }

    private static void InitializeLanguage() { try { var savedLanguage = UserSettingsService.Instance.Language; var culture = new CultureInfo(savedLanguage); Strings.Culture = culture; LanguageService.Instance.SwitchLanguage(savedLanguage); System.Diagnostics.Debug.WriteLine($"[Program] Language initialized to: {savedLanguage}"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Program] Failed to initialize language: {ex.Message}"); } }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().WithInterFont();
}

internal static class NativeMethods { public const int SW_RESTORE = 9; [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd); }