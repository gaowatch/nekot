using System;
using System.Threading;

namespace NekoT.Desktop.Services;

public sealed class SingleInstanceManager : IDisposable
{
    private readonly Mutex? _mutex;
    private readonly bool _isFirstInstance;
    private readonly string _mutexName;
    private bool _disposed;

    public bool IsFirstInstance => _isFirstInstance;
    public string MutexName => _mutexName;

    public SingleInstanceManager(string mutexName)
    {
        if (string.IsNullOrWhiteSpace(mutexName))
        {
            throw new ArgumentException("Mutex name cannot be null or empty", nameof(mutexName));
        }

        _mutexName = mutexName;
        _mutex = new Mutex(true, _mutexName, out _isFirstInstance);
    }

    internal Mutex? GetMutexForAbandonmentTest() => _mutex;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isFirstInstance && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
            finally
            {
                _mutex.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }

    ~SingleInstanceManager() => Dispose();
}

public class SingleInstanceCheckResult
{
    public bool IsFirstInstance { get; init; }
    public string MutexName { get; init; } = string.Empty;
    public IntPtr MutexHandle { get; init; }
}

public static class SingleInstanceGuard
{
    private const string SingleInstanceGuid = "8B8D8D90-1234-5678-ABCD-123456789ABC";
    private static readonly string AppMutexName = $@"Global\NekoT_SingleInstance_{SingleInstanceGuid}";

    public static SingleInstanceCheckResult Check()
    {
        var mutex = new Mutex(true, AppMutexName, out bool createdNew);
        return new SingleInstanceCheckResult
        {
            IsFirstInstance = createdNew,
            MutexName = AppMutexName,
            MutexHandle = IntPtr.Zero
        };
    }

    public static SingleInstanceManager CreateManager() => new SingleInstanceManager(AppMutexName);
}