using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class AtomicFileEngine : IAtomicFileEngine
{
    private readonly string _filePath;
    private readonly string _backupPath;
    private readonly string _tempPath;
    private readonly object _lock = new();

    public AtomicFileEngine(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _backupPath = filePath + ".bak";
        _tempPath = filePath + ".tmp";
    }

    public async Task<bool> WriteAsync<T>(T data, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_filePath))
                        File.Copy(_filePath, _backupPath, overwrite: true);

                    var json = JsonSerializer.Serialize(data);
                    File.WriteAllText(_tempPath, json);

                    if (File.Exists(_filePath))
                        File.Replace(_tempPath, _filePath, _backupPath);
                    else
                        File.Move(_tempPath, _filePath);

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }, ct);
    }

    public async Task<T?> ReadAsync<T>(CancellationToken ct = default)
    {
        TryDeleteFile(_tempPath);

        var result = await TryReadFileAsync<T>(_filePath, ct);
        if (result != null) return result;

        result = await TryReadFileAsync<T>(_backupPath, ct);
        return result;
    }

    public Task<bool> ExistsAsync()
    {
        return Task.FromResult(File.Exists(_filePath) || File.Exists(_backupPath));
    }

    private async Task<T?> TryReadFileAsync<T>(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return default;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}

public interface IAtomicFileEngine
{
    Task<bool> WriteAsync<T>(T data, CancellationToken ct = default);
    Task<T?> ReadAsync<T>(CancellationToken ct = default);
    Task<bool> ExistsAsync();
}