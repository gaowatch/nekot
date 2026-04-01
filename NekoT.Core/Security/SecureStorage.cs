using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NekoT.Core.Security;

public class SecureStorage : ISecureStorage
{
    private readonly string _storageFile;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly byte[] _entropy;

    public SecureStorage()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var storageDir = Path.Combine(appDataPath, "NekoT");
        if (!Directory.Exists(storageDir)) Directory.CreateDirectory(storageDir);
        _storageFile = Path.Combine(storageDir, "api_keys.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _entropy = Encoding.UTF8.GetBytes("NekoT_SecureStorage_v1");
    }

    public void SaveApiKey(string provider, string apiKey)
    {
        var keys = LoadAllKeys();
        var encryptedKey = Encrypt(apiKey);
        keys[provider] = encryptedKey;
        SaveAllKeys(keys);
    }

    public string? GetApiKey(string provider)
    {
        var keys = LoadAllKeys();
        if (keys.TryGetValue(provider, out var encryptedKey))
        {
            return Decrypt(encryptedKey);
        }
        return null;
    }

    public void DeleteApiKey(string provider)
    {
        var keys = LoadAllKeys();
        if (keys.Remove(provider)) SaveAllKeys(keys);
    }

    public Dictionary<string, string> LoadAllKeys()
    {
        try
        {
            if (!File.Exists(_storageFile)) return new Dictionary<string, string>();
            var json = File.ReadAllText(_storageFile);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions) ?? new Dictionary<string, string>();
        }
        catch { return new Dictionary<string, string>(); }
    }

    private void SaveAllKeys(Dictionary<string, string> keys)
    {
        var json = JsonSerializer.Serialize(keys, _jsonOptions);
        File.WriteAllText(_storageFile, json);
    }

    public bool HasApiKey(string provider)
    {
        var keys = LoadAllKeys();
        return keys.ContainsKey(provider);
    }

    private string Encrypt(string plainText)
    {
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch { return plainText; }
    }

    private string Decrypt(string encryptedText)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch { return encryptedText; }
    }
}