using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class AtomicFileEngine : IAtomicFileEngine
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AtomicFileEngine(string filePath)
    {
        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> ReadAsync<T>() where T : class
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AtomicFileEngine] Read failed: {ex.Message}");
            return null;
        }
    }

    public async Task WriteAsync<T>(T data) where T : class
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempFile = _filePath + $".{Guid.NewGuid():N}.tmp";
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(tempFile, json);

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            File.Move(tempFile, _filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AtomicFileEngine] Write failed: {ex.Message}");
        }
    }

    public bool Exists()
    {
        return File.Exists(_filePath);
    }
}