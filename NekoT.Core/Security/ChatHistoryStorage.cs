using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NekoT.Core.Security;

public class ChatHistoryStorage : IChatHistoryStorage
{
    private static readonly Lazy<ChatHistoryStorage> _instance = new(() => new ChatHistoryStorage());
    public static ChatHistoryStorage Instance => _instance.Value;

    private readonly string _storageFile;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ChatMessageData> _messages;

    private ChatHistoryStorage()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var storageDir = Path.Combine(appDataPath, "NekoT");
        if (!Directory.Exists(storageDir)) Directory.CreateDirectory(storageDir);
        _storageFile = Path.Combine(storageDir, "chat_history.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _messages = new List<ChatMessageData>();
        LoadMessages();
    }

    public List<ChatMessageData> LoadMessages()
    {
        try
        {
            if (File.Exists(_storageFile))
            {
                var json = File.ReadAllText(_storageFile);
                var messages = JsonSerializer.Deserialize<List<ChatMessageData>>(json, _jsonOptions);
                if (messages != null)
                {
                    _messages.Clear();
                    _messages.AddRange(messages);
                }
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ChatHistoryStorage] Load failed: {ex.Message}"); }
        return new List<ChatMessageData>(_messages);
    }

    public void SaveMessages(List<ChatMessageData> messages)
    {
        try
        {
            _messages.Clear();
            _messages.AddRange(messages);
            var json = JsonSerializer.Serialize(_messages, _jsonOptions);
            File.WriteAllText(_storageFile, json);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ChatHistoryStorage] Save failed: {ex.Message}"); }
    }

    public void AddMessage(ChatMessageData message)
    {
        _messages.Add(message);
        SaveMessages(_messages);
    }

    public void ClearMessages()
    {
        _messages.Clear();
        SaveMessages(_messages);
    }
}

public class ChatMessageData
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Model { get; set; }
    public int? Tokens { get; set; }
}