using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class AtomicFileEngine : IAtomicFileEngine
{
    private readonly string _filePath;
    private readonly string _backupPath;
    private readonly string _tempPath;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public AtomicFileEngine(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("文件路径不能为空", nameof(filePath));
        try { Path.GetFullPath(filePath); }
        catch { throw new ArgumentException($"无效的文件路径: {filePath}", nameof(filePath)); }
        _filePath = filePath;
        _backupPath = filePath + ".bak";
        _tempPath = filePath + ".tmp";
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<bool> WriteAsync<T>(T data, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested) return false;
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    if (ct.IsCancellationRequested) return false;
                    if (File.Exists(_filePath)) File.Copy(_filePath, _backupPath, overwrite: true);
                    var wrapper = new DataWrapper<T> { Data = data, Checksum = ComputeChecksum(data), Timestamp = DateTime.UtcNow, Version = 1 };
                    var json = JsonSerializer.Serialize(wrapper, _jsonOptions);
                    File.WriteAllText(_tempPath, json, System.Text.Encoding.UTF8);
                    if (ct.IsCancellationRequested) { TryDeleteFile(_tempPath); return false; }
                    if (File.Exists(_filePath)) File.Replace(_tempPath, _filePath, _backupPath);
                    else File.Move(_tempPath, _filePath);
                    return true;
                }
                catch (OperationCanceledException) { return false; }
                catch { TryDeleteFile(_tempPath); return false; }
            }
        }, ct);
    }

    public async Task<T?> ReadAsync<T>(CancellationToken ct = default)
    {
        TryDeleteFile(_tempPath);
        var result = await TryReadFileAsync<T>(_filePath, ct);
        if (result != null) return result;
        return await TryReadFileAsync<T>(_backupPath, ct);
    }

    public Task<bool> ExistsAsync() => Task.FromResult(File.Exists(_filePath) || File.Exists(_backupPath));

    public async Task<bool> BackupAsync()
    {
        if (!File.Exists(_filePath)) return false;
        try { await Task.Run(() => File.Copy(_filePath, _backupPath, overwrite: true)); return true; }
        catch { return false; }
    }

    private async Task<T?> TryReadFileAsync<T>(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return default;
        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var wrapper = JsonSerializer.Deserialize<DataWrapper<T>>(json, _jsonOptions);
            if (wrapper == null || wrapper.Data == null) return default;
            var expectedChecksum = ComputeChecksum(wrapper.Data);
            if (wrapper.Checksum != expectedChecksum) return default;
            return wrapper.Data;
        }
        catch { return default; }
    }

    private static string ComputeChecksum<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static void TryDeleteFile(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
}

internal class DataWrapper<T>
{
    public T? Data { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}