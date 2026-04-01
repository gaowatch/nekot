using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace NekoT.Core.Security;

public sealed class ChatHistoryStorage : IChatHistoryStorage, IDisposable
{
    private static readonly Lazy<ChatHistoryStorage> _instance = new(
        () => new ChatHistoryStorage(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static ChatHistoryStorage Instance => _instance.Value;

    private const byte FormatVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly string _storagePath;
    private readonly string _keyPath;
    private byte[]? _key;
    private bool _disposed;

    private readonly object _lock = new();

    public ChatHistoryStorage()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
            SetDirectoryAccessControl(appDataPath);
        }

        _storagePath = Path.Combine(appDataPath, "chat_history.enc");
        _keyPath = Path.Combine(appDataPath, "chat_key.bin");

        LoadOrCreateKey();
    }

    private void LoadOrCreateKey()
    {
        if (File.Exists(_keyPath))
        {
            try
            {
                var protectedKey = File.ReadAllBytes(_keyPath);
                _key = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
                
                if (_key.Length != KeySize)
                {
                    _key = null;
                }
            }
            catch
            {
                _key = null;
            }
        }

        if (_key == null)
        {
            _key = new byte[KeySize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(_key);

            var protectedKey = ProtectedData.Protect(_key, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_keyPath, protectedKey);
            SetFileAccessControl(_keyPath);
        }
    }

    public void SaveMessages(IEnumerable<ChatMessageData> messages)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var encrypted = Encrypt(json);
            File.WriteAllBytes(_storagePath, encrypted);
            SetFileAccessControl(_storagePath);
        }
    }

    public List<ChatMessageData>? LoadMessages()
    {
        lock (_lock)
        {
            if (!File.Exists(_storagePath))
                return null;

            try
            {
                var encrypted = File.ReadAllBytes(_storagePath);
                var json = Decrypt(encrypted);

                return JsonSerializer.Deserialize<List<ChatMessageData>>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    public void ClearMessages()
    {
        lock (_lock)
        {
            if (File.Exists(_storagePath))
            {
                File.Delete(_storagePath);
            }
        }
    }

    public string ExportDecrypted()
    {
        var messages = LoadMessages();
        if (messages == null || messages.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("# 聊天记录导出");
        sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"消息数量: {messages.Count}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var message in messages)
        {
            var roleDisplay = message.Role == "user" ? "用户" : "助手";
            sb.AppendLine($"### {roleDisplay} ({message.Timestamp:HH:mm:ss})");
            sb.AppendLine();
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*此记录已从加密存储中解密导出*");

        return sb.ToString();
    }

    public string ExportDecryptedJson()
    {
        var messages = LoadMessages();
        if (messages == null || messages.Count == 0)
            return "{}";

        var exportData = new
        {
            ExportTime = DateTime.Now,
            MessageCount = messages.Count,
            Messages = messages,
            Note = "此记录已从加密存储中解密导出"
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private byte[] Encrypt(string plainText)
    {
        if (_key == null)
            throw new InvalidOperationException("Key not initialized");

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[1 + nonce.Length + tag.Length + cipherBytes.Length];
        result[0] = FormatVersion;
        Buffer.BlockCopy(nonce, 0, result, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, 1 + nonce.Length + tag.Length, cipherBytes.Length);

        CryptographicOperations.ZeroMemory(plainBytes);
        CryptographicOperations.ZeroMemory(cipherBytes);

        return result;
    }

    private string Decrypt(byte[] encrypted)
    {
        if (_key == null)
            throw new InvalidOperationException("Key not initialized");

        if (encrypted.Length < 1 + NonceSize + TagSize)
            throw new ArgumentException("Invalid encrypted data");

        var version = encrypted[0];
        if (version != FormatVersion)
            throw new ArgumentException($"Unsupported version: {version}");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[encrypted.Length - 1 - NonceSize - TagSize];

        Buffer.BlockCopy(encrypted, 1, nonce, 0, NonceSize);
        Buffer.BlockCopy(encrypted, 1 + NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encrypted, 1 + NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        var result = Encoding.UTF8.GetString(plainBytes);

        CryptographicOperations.ZeroMemory(plainBytes);

        return result;
    }

    internal static void SetDirectoryAccessControl(string directoryPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var accessControl = directoryInfo.GetAccessControl();

            accessControl.SetAccessRuleProtection(true, false);

            var currentUser = WindowsIdentity.GetCurrent().Owner;
            if (currentUser != null)
            {
                accessControl.SetOwner(currentUser);
            }

            var fullControlRule = new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().Name,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            accessControl.SetAccessRule(fullControlRule);
            directoryInfo.SetAccessControl(accessControl);
        }
        catch
        {
        }
    }

    internal static void SetFileAccessControl(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var accessControl = fileInfo.GetAccessControl();

            accessControl.SetAccessRuleProtection(true, false);

            var currentUser = WindowsIdentity.GetCurrent().Owner;
            if (currentUser != null)
            {
                accessControl.SetOwner(currentUser);
            }

            var fullControlRule = new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().Name,
                FileSystemRights.FullControl,
                AccessControlType.Allow);

            accessControl.SetAccessRule(fullControlRule);
            fileInfo.SetAccessControl(accessControl);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_key != null)
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }

        _disposed = true;
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