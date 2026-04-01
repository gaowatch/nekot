namespace NekoT.Core.Contracts;

public interface IChatHistoryStorage
{
    System.Collections.Generic.List<ChatMessageData> LoadMessages();
    void SaveMessages(System.Collections.Generic.List<ChatMessageData> messages);
    void AddMessage(ChatMessageData message);
    void ClearMessages();
}