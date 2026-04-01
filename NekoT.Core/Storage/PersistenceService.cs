using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class PersistenceService : IPersistenceService, IDisposable
{
    private readonly IAtomicFileEngine _fileEngine;
    private readonly IWriteBuffer<TokenUsageData> _writeBuffer;
    private readonly Timer _dayCheckTimer;
    private readonly object _dirtyLock = new();
    private readonly string _persistFilePath;
    private bool _disposed;
    private DateTime _currentDate;

    public event EventHandler? DayChanged;

    public PersistenceService(string persistFilePath, TimeSpan flushInterval)
    {
        _persistFilePath = persistFilePath ?? throw new ArgumentNullException(nameof(persistFilePath));
        _fileEngine = new AtomicFileEngine(_persistFilePath);
        _writeBuffer = new WriteBuffer<TokenUsageData>(flushInterval, async data =>
        {
            await _fileEngine.WriteAsync(data);
        });

        _currentDate = DateTime.Now.Date;
        _dayCheckTimer = new Timer(CheckDayChange, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public void MarkDirty(TokenUsageData data)
    {
        if (_disposed) return;
        _writeBuffer.MarkDirty(data);
    }

    public async Task<TokenUsageData> LoadAsync()
    {
        try
        {
            var data = await _fileEngine.ReadAsync<TokenUsageData>();
            if (data != null)
            {
                _currentDate = data.LastRecordDate.Date;
                return data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PersistenceService] Load failed: {ex.Message}");
        }

        return new TokenUsageData
        {
            Version = 1,
            LastRecordDate = DateTime.Now.Date,
            BarDataPoints = new List<BarDataPointInfo>()
        };
    }

    public async Task OnShutdownAsync()
    {
        if (_disposed) return;
        await _writeBuffer.FlushAsync();
    }

    private void CheckDayChange(object? state)
    {
        var today = DateTime.Now.Date;
        if (today != _currentDate)
        {
            _currentDate = today;
            DayChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dayCheckTimer.Dispose();
        _writeBuffer.Dispose();
    }
}