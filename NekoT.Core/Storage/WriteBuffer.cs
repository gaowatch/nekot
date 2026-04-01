using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class WriteBuffer : IDisposable
{
    private readonly string _filePath;
    private readonly ConcurrentQueue<string> _queue;
    private readonly Task _flushTask;
    private readonly CancellationTokenSource _cts;
    private readonly TimeSpan _flushInterval;
    private bool _disposed;

    public WriteBuffer(string filePath, TimeSpan? flushInterval = null)
    {
        _filePath = filePath;
        _queue = new ConcurrentQueue<string>();
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(1);
        _cts = new CancellationTokenSource();
        _flushTask = Task.Run(FlushLoop);
    }

    public void Enqueue(string line)
    {
        if (_disposed) return;
        _queue.Enqueue(line);
    }

    private async Task FlushLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            await Task.Delay(_flushInterval, _cts.Token).ContinueWith(t => { }, TaskContinuationOptions.OnlyOnRanToCompletion);
            Flush();
        }
    }

    public void Flush()
    {
        if (_queue.IsEmpty) return;
        var lines = new List<string>();
        while (_queue.TryDequeue(out var line)) lines.Add(line);
        if (lines.Count > 0) File.AppendAllLines(_filePath, lines);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        Flush();
        _cts.Dispose();
    }
}