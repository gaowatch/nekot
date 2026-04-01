using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class PersistenceService : IPersistenceService, IAsyncDisposable
{
    private readonly IAtomicFileEngine _fileEngine;
    private readonly IWriteBuffer<TokenUsageData> _writeBuffer;
    private readonly Timer _autoSaveTimer;
    private readonly Timer _dayCheckTimer;
    private DateTime _lastKnownDate;
    private bool _isShuttingDown;
    private bool _disposed;

    public event EventHandler? DayChanged;

    public PersistenceService(IAtomicFileEngine fileEngine)
    {
        _fileEngine = fileEngine ?? throw new ArgumentNullException(nameof(fileEngine));
        _writeBuffer = new WriteBuffer<TokenUsageData>(
            TimeSpan.FromSeconds(30),
            SaveToDiskAsync
        );
        _autoSaveTimer = new Timer(OnAutoSave, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        _dayCheckTimer = new Timer(OnDayCheck, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _lastKnownDate = DateTime.Today;
    }

    public void MarkDirty(TokenUsageData data)
    {
        if (_isShuttingDown || _disposed) return;
        _writeBuffer.MarkDirty(data);
    }

    public async Task<TokenUsageData> LoadAsync()
    {
        var data = await _fileEngine.ReadAsync<TokenUsageData>();
        
        if (data == null)
        {
            return new TokenUsageData
            {
                LastSavedTime = DateTime.UtcNow,
                LastRecordDate = DateTime.Today
            };
        }

        if (data.LastRecordDate.Date < DateTime.Today)
        {
            data.TodayTokenCount = 0;
            data.TodayRequestCount = 0;
            data.BarDataPoints?.Clear();
            data.LastRecordDate = DateTime.Today;
        }

        return data;
    }

    private async void OnAutoSave(object? state)
    {
        if (_isShuttingDown || _disposed) return;
        await _writeBuffer.FlushAsync();
    }

    private void OnDayCheck(object? state)
    {
        if (_isShuttingDown || _disposed) return;

        if (DateTime.Today > _lastKnownDate)
        {
            _lastKnownDate = DateTime.Today;
            DayChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task SaveToDiskAsync(TokenUsageData data)
    {
        data.LastSavedTime = DateTime.UtcNow;
        await _fileEngine.WriteAsync(data);
    }

    public async Task OnShutdownAsync()
    {
        if (_disposed) return;

        _isShuttingDown = true;

        await _autoSaveTimer.DisposeAsync();
        await _dayCheckTimer.DisposeAsync();

        await _writeBuffer.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await OnShutdownAsync();

        if (_writeBuffer is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}