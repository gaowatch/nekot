using System;
using System.Threading;
using Microsoft.Win32;

namespace NekoT.Desktop.Services;

public class SingleInstanceManager : IDisposable
{
    private static SingleInstanceManager? _instance;
    private static readonly object _lock = new();
    private Mutex? _mutex;
    private bool _disposed;

    public static SingleInstanceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new SingleInstanceManager();
                }
            }
            return _instance;
        }
    }

    private SingleInstanceManager()
    {
    }

    public bool IsFirstInstance { get; private set; }

    public bool TryAcquireMutex(string mutexName)
    {
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        IsFirstInstance = createdNew;
        return createdNew;
    }

    public void ReleaseMutex()
    {
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ReleaseMutex();

        if (_mutex != null)
        {
            _mutex.Dispose();
            _mutex = null;
        }
    }
}