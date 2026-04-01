using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public class ChatHistoryStorage : IChatHistoryStorage
{
    private static readonly Lazy<ChatHistoryStorage> _instance = new(() => new ChatHistoryStorage());
    public static ChatHistoryStorage Instance => _instance.Value;

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ChatHistoryStorage()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");
        Directory.CreateDirectory(appDataPath);
        _filePath = Path.Combine(appDataPath, "chat_history.json");
    }

    public async Task<List<ChatMessageData>?> LoadMessagesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<ChatMessageData>>(json);
        }
        catch
        {
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveMessagesAsync(List<ChatMessageData> messages)
    {
        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void ClearMessages()
    {
        _lock.Wait();
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        finally
        {
            _lock.Release();
        }
    }
}

public class ChatMessageData
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Model { get; set; }
    public int Tokens { get; set; }
}