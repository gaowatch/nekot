using System;
using System.Collections.Generic;

namespace NekoT.Core.Security;

public interface IChatHistoryStorage : IDisposable
{
    void SaveMessages(IEnumerable<ChatMessageData> messages);
    List<ChatMessageData>? LoadMessages();
    void ClearMessages();
    string ExportDecrypted();
    string ExportDecryptedJson();
}

public class ChatMessageData
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Model { get; set; }
    public int Tokens { get; set; }
}