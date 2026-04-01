using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class PersistenceService : IPersistenceService
{
    private readonly string _basePath;
    private readonly Dictionary<Type, IStorageAdapter> _adapters;

    public PersistenceService(string basePath)
    {
        _basePath = basePath;
        _adapters = new Dictionary<Type, IStorageAdapter>();
        if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
    }

    public void RegisterAdapter<T>(IStorageAdapter<T> adapter) where T : class => _adapters[typeof(T)] = adapter;

    public async Task<T?> LoadAsync<T>() where T : class
    {
        if (_adapters.TryGetValue(typeof(T), out var adapter))
        {
            return await ((IStorageAdapter<T>)adapter).LoadAsync();
        }
        return null;
    }

    public async Task SaveAsync<T>(T data) where T : class
    {
        if (_adapters.TryGetValue(typeof(T), out var adapter))
        {
            await ((IStorageAdapter<T>)adapter).SaveAsync(data);
        }
    }
}

public interface IPersistenceService
{
    Task<T?> LoadAsync<T>() where T : class;
    Task SaveAsync<T>(T data) where T : class;
}

public interface IStorageAdapter
{
}

public interface IStorageAdapter<T> : IStorageAdapter where T : class
{
    Task<T?> LoadAsync();
    Task SaveAsync(T data);
}