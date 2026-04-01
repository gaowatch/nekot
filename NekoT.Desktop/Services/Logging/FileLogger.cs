using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace NekoT.Desktop.Services.Logging;

internal class FileLogger : IDisposable
{
    private readonly string _logFile;
    private readonly object _logLock = new();
    private readonly ConcurrentQueue<string> _logQueue = new();
    private Timer? _logTimer;
    private int _instanceCount;
    private bool _timerDisposed;
    private readonly TimeSpan _logFlushInterval = TimeSpan.FromMilliseconds(100);
    private bool _disposed;

    public FileLogger(string logFile)
    {
        _logFile = logFile;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e) { Dispose(); }

    public void Log(string message)
    {
        if (_disposed) return;
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        _logQueue.Enqueue(line);
        System.Diagnostics.Debug.WriteLine(message);
        EnsureTimerStarted();
    }

    private void EnsureTimerStarted()
    {
        lock (_logLock)
        {
            if (_disposed) return;
            _timerDisposed = false;
            _instanceCount++;
            if (_instanceCount == 1 || _logTimer == null)
            {
                _logTimer?.Dispose();
                _logTimer = new Timer(FlushLogs, null, _logFlushInterval, _logFlushInterval);
            }
        }
    }

    private void FlushLogs(object? state)
    {
        if (_logQueue.IsEmpty || _timerDisposed || _disposed) return;
        try
        {
            var lines = new List<string>();
            while (_logQueue.TryDequeue(out var line)) lines.Add(line);
            if (lines.Count > 0)
            {
                lock (_logLock)
                {
                    try { File.AppendAllLines(_logFile, lines, Encoding.UTF8); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LogError] Failed to write to {_logFile}: {ex.Message}"); }
                }
            }
        }
        catch (ObjectDisposedException) { _timerDisposed = true; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LogError] FlushLogs failed: {ex.Message}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timerDisposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        lock (_logLock) { _logTimer?.Dispose(); _logTimer = null; }
        FlushLogs(null);
    }
}