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
    private readonly TimeSpan _logFlushInterval = TimeSpan.FromMilliseconds(100);
    private bool _disposed;

    public FileLogger(string logFile) { _logFile = logFile; AppDomain.CurrentDomain.ProcessExit += OnProcessExit; }
    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    public void Log(string message)
    {
        if (_disposed) return;
        _logQueue.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        EnsureTimerStarted();
    }

    private void EnsureTimerStarted()
    {
        lock (_logLock)
        {
            if (_disposed) return;
            _logTimer ??= new Timer(FlushLogs, null, _logFlushInterval, _logFlushInterval);
        }
    }

    private void FlushLogs(object? state)
    {
        if (_logQueue.IsEmpty || _disposed) return;
        var lines = new List<string>();
        while (_logQueue.TryDequeue(out var line)) lines.Add(line);
        if (lines.Count > 0) lock (_logLock) { try { File.AppendAllLines(_logFile, lines, Encoding.UTF8); } catch { } }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_logLock) { _logTimer?.Dispose(); }
        FlushLogs(null);
    }
}
