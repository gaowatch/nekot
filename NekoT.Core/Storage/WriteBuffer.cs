using System;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public interface IWriteBuffer<T>
{
    void MarkDirty(T data);
    Task FlushAsync();
    bool IsDirty { get; }
    DateTime LastFlushTime { get; }
}

public class WriteBuffer<T> : IWriteBuffer<T>, IDisposable
{
    private T? _buffer;
    private bool _isDirty;
    private DateTime _lastFlushTime;
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Func<T, Task> _flushAction;
    private readonly TimeSpan _flushInterval;
    private bool _disposed;

    public bool IsDirty => _isDirty;
    public DateTime LastFlushTime => _lastFlushTime;

    public WriteBuffer(TimeSpan flushInterval, Func<T, Task> flushAction)
    {
        _flushInterval = flushInterval;
        _flushAction = flushAction ?? throw new ArgumentNullException(nameof(flushAction));
        _lastFlushTime = DateTime.UtcNow;
        _flushTimer = new Timer(OnTimerCallback, null, flushInterval, flushInterval);
    }

    public void MarkDirty(T data)
    {
        if (_disposed) return;
        lock (this)
        {
            _buffer = data;
            _isDirty = true;
        }
    }

    private async void OnTimerCallback(object? state)
    {
        if (_disposed || !_isDirty) return;
        await FlushAsync();
    }

    public async Task FlushAsync()
    {
        if (_disposed) return;
        await _lock.WaitAsync();
        try
        {
            if (!_isDirty || _buffer == null) return;
            await _flushAction(_buffer);
            _isDirty = false;
            _lastFlushTime = DateTime.UtcNow;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _flushTimer.Dispose();
        if (_isDirty && _buffer != null)
        {
            try { FlushAsync().GetAwaiter().GetResult(); }
            catch { }
        }
        _lock.Dispose();
    }
}