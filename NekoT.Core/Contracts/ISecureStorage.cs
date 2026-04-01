namespace NekoT.Core.Contracts;

public interface ISecureStorage
{
    void SaveApiKey(string provider, string apiKey);
    string? GetApiKey(string provider);
    void DeleteApiKey(string provider);
    Dictionary<string, string> LoadAllKeys();
    bool HasApiKey(string provider);
}