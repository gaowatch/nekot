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