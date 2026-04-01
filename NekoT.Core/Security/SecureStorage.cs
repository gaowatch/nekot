using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using NekoT.Core.Contracts;

namespace NekoT.Core.Security;

internal sealed class SecureKeyManager : IDisposable
{
    private static readonly TraceSource Logger = new("NekoT.Security") { Switch = { Level = SourceLevels.Warning } };

    private static readonly Lazy<SecureKeyManager> _instance = new(
        () => new SecureKeyManager(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static SecureKeyManager Instance => _instance.Value;

    private const int CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;
    private const int CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptProtectMemory(IntPtr pData, int cbData, int dwFlags);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptUnprotectMemory(IntPtr pData, int cbData, int dwFlags);

    private byte[]? _protectedKey;
    private GCHandle _keyHandle;
    private IntPtr _keyPtr;
    private int _keyLength;
    private bool _disposed;

    public SecureKeyManager()
    {
        LoadOrCreateKey();
    }

    private void LoadOrCreateKey()
    {
        var keyPath = GetKeyPath();

        byte[] rawKey;
        if (File.Exists(keyPath))
        {
            try
            {
                var protectedKey = File.ReadAllBytes(keyPath);
                rawKey = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                Logger.TraceEvent(TraceEventType.Error, 0, $"Failed to load DPAPI key: {ex.Message}");
                rawKey = GenerateNewKey(keyPath);
            }
        }
        else
        {
            rawKey = GenerateNewKey(keyPath);
        }

        ProtectKeyInMemory(rawKey);
        CryptographicOperations.ZeroMemory(rawKey);
    }

    private byte[] GenerateNewKey(string keyPath)
    {
        var key = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);

        var encryptedKey = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        var directory = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            SecureStorage.SetDirectoryAccessControl(directory);
        }
        File.WriteAllBytes(keyPath, encryptedKey);
        SecureStorage.SetFileAccessControl(keyPath);

        return key;
    }

    private void ProtectKeyInMemory(byte[] key)
    {
        _keyLength = key.Length;
        var paddedLength = ((_keyLength + CRYPTPROTECTMEMORY_BLOCK_SIZE - 1) / CRYPTPROTECTMEMORY_BLOCK_SIZE)
                          * CRYPTPROTECTMEMORY_BLOCK_SIZE;

        _protectedKey = new byte[paddedLength];
        Buffer.BlockCopy(key, 0, _protectedKey, 0, _keyLength);

        _keyHandle = GCHandle.Alloc(_protectedKey, GCHandleType.Pinned);
        _keyPtr = _keyHandle.AddrOfPinnedObject();

        if (!CryptProtectMemory(_keyPtr, paddedLength, CRYPTPROTECTMEMORY_SAME_PROCESS))
        {
            var error = Marshal.GetLastWin32Error();
            throw new CryptographicException($"CryptProtectMemory failed with error code: {error}");
        }
    }

    public T ExecuteWithKey<T>(Func<byte[], T> action)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SecureKeyManager));

        var paddedLength = _protectedKey!.Length;
        if (!CryptUnprotectMemory(_keyPtr, paddedLength, CRYPTPROTECTMEMORY_SAME_PROCESS))
        {
            var error = Marshal.GetLastWin32Error();
            throw new CryptographicException($"CryptUnprotectMemory failed with error code: {error}");
        }

        try
        {
            var tempKey = new byte[_keyLength];
            Buffer.BlockCopy(_protectedKey, 0, tempKey, 0, _keyLength);

            try
            {
                return action(tempKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(tempKey);
            }
        }
        finally
        {
            if (!CryptProtectMemory(_keyPtr, paddedLength, CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                var error = Marshal.GetLastWin32Error();
                Logger.TraceEvent(TraceEventType.Error, 0, $"Failed to re-protect key memory: {error}");
            }
        }
    }

    public void ExecuteWithKey(Action<byte[]> action)
    {
        ExecuteWithKey(key =>
        {
            action(key);
            return true;
        });
    }

    private static string GetKeyPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT", "nekot.key");
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_protectedKey != null && _keyPtr != IntPtr.Zero)
        {
            CryptUnprotectMemory(_keyPtr, _protectedKey.Length, CRYPTPROTECTMEMORY_SAME_PROCESS);
            CryptographicOperations.ZeroMemory(_protectedKey);
        }

        if (_keyHandle.IsAllocated)
        {
            _keyHandle.Free();
        }

        _protectedKey = null;
        _disposed = true;
    }

    ~SecureKeyManager()
    {
        Dispose();
    }
}

public class SecureStorage : ISecureStorage
{
    private readonly string _storagePath;
    private readonly SecureKeyManager _keyManager;
    private static readonly TraceSource Logger = new("NekoT.Security") { Switch = { Level = SourceLevels.Warning } };

    private const byte FormatVersionV1 = 1;
    private const byte FormatVersionV2 = 2;

    public SecureStorage(string? storagePath = null)
    {
        _storagePath = storagePath ?? GetDefaultStoragePath();
        _keyManager = SecureKeyManager.Instance;
    }

    private static string GetDefaultStoragePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "NekoT", "secure.dat");
    }

    internal static void SetFileAccessControl(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var security = fileInfo.GetAccessControl();

            using var identity = WindowsIdentity.GetCurrent();
            var currentUser = identity.Owner ?? throw new InvalidOperationException("Cannot get current user identity");

            security.SetAccessRuleProtection(true, false);
            security.ResetAccessRule(new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                AccessControlType.Allow));

            fileInfo.SetAccessControl(security);
        }
        catch (Exception ex)
        {
            Logger.TraceEvent(TraceEventType.Warning, 0, $"Failed to set file ACL for {filePath}: {ex.Message}");
        }
    }

    internal static void SetDirectoryAccessControl(string directoryPath)
    {
        try
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var security = dirInfo.GetAccessControl();

            using var identity = WindowsIdentity.GetCurrent();
            var currentUser = identity.Owner ?? throw new InvalidOperationException("Cannot get current user identity");

            security.SetAccessRuleProtection(true, false);
            security.ResetAccessRule(new FileSystemAccessRule(
                currentUser,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            dirInfo.SetAccessControl(security);
        }
        catch (Exception ex)
        {
            Logger.TraceEvent(TraceEventType.Warning, 0, $"Failed to set directory ACL for {directoryPath}: {ex.Message}");
        }
    }

    public void SaveApiKey(string provider, string apiKey)
    {
        var keys = LoadAllKeys();
        keys[provider] = apiKey;

        var json = JsonSerializer.Serialize(keys);
        var encrypted = Encrypt(json);

        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            SetDirectoryAccessControl(directory);
        }

        File.WriteAllBytes(_storagePath, encrypted);
        SetFileAccessControl(_storagePath);
    }

    public string? GetApiKey(string provider)
    {
        var keys = LoadAllKeys();
        return keys.TryGetValue(provider, out var key) ? key : null;
    }

    public void DeleteApiKey(string provider)
    {
        var keys = LoadAllKeys();
        if (keys.Remove(provider))
        {
            if (keys.Count == 0)
            {
                if (File.Exists(_storagePath))
                {
                    File.Delete(_storagePath);
                }
            }
            else
            {
                var json = JsonSerializer.Serialize(keys);
                var encrypted = Encrypt(json);
                File.WriteAllBytes(_storagePath, encrypted);
            }
        }
    }

    public Dictionary<string, string> LoadAllKeys()
    {
        if (!File.Exists(_storagePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var encrypted = File.ReadAllBytes(_storagePath);
            var json = Decrypt(encrypted);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch (CryptographicException ex)
        {
            Logger.TraceEvent(TraceEventType.Error, 1, $"Failed to decrypt secure storage: {ex.Message}");
            return new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            Logger.TraceEvent(TraceEventType.Critical, 2, $"Unexpected error loading secure storage: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    public bool HasApiKey(string provider)
    {
        return GetApiKey(provider) != null;
    }

    private byte[] Encrypt(string plainText)
    {
        return _keyManager.ExecuteWithKey(key =>
        {
            using var aesGcm = new AesGcm(key, 16);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16];
            var nonce = new byte[12];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);

            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var result = new byte[1 + nonce.Length + tag.Length + cipherBytes.Length];
            result[0] = FormatVersionV2;
            Buffer.BlockCopy(nonce, 0, result, 1, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, 1 + nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, 1 + nonce.Length + tag.Length, cipherBytes.Length);

            CryptographicOperations.ZeroMemory(plainBytes);
            CryptographicOperations.ZeroMemory(cipherBytes);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(nonce);

            return result;
        });
    }

    private string Decrypt(byte[] cipherData)
    {
        if (cipherData == null || cipherData.Length == 0)
            throw new CryptographicException("Invalid cipher data");

        var version = cipherData[0];

        return version switch
        {
            FormatVersionV2 => DecryptV2(cipherData),
            FormatVersionV1 => DecryptV1(cipherData),
            _ => throw new CryptographicException($"Unknown format version: {version}")
        };
    }

    private string DecryptV2(byte[] cipherData)
    {
        return _keyManager.ExecuteWithKey(key =>
        {
            using var aesGcm = new AesGcm(key, 16);

            const int headerSize = 1 + 12 + 16;
            if (cipherData.Length < headerSize)
                throw new CryptographicException("Invalid V2 cipher data length");

            var nonce = new byte[12];
            var tag = new byte[16];
            var cipherBytes = new byte[cipherData.Length - headerSize];

            Buffer.BlockCopy(cipherData, 1, nonce, 0, 12);
            Buffer.BlockCopy(cipherData, 13, tag, 0, 16);
            Buffer.BlockCopy(cipherData, 29, cipherBytes, 0, cipherBytes.Length);

            var plainBytes = new byte[cipherBytes.Length];

            try
            {
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
                return Encoding.UTF8.GetString(plainBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(tag);
                CryptographicOperations.ZeroMemory(cipherBytes);
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        });
    }

    private string DecryptV1(byte[] cipherData)
    {
        return _keyManager.ExecuteWithKey(key =>
        {
            using var aes = Aes.Create();
            aes.Key = key;

            if (cipherData.Length < 17)
                throw new CryptographicException("Invalid V1 cipher data length");

            var iv = new byte[16];
            Buffer.BlockCopy(cipherData, 1, iv, 0, 16);

            var encryptedBytes = new byte[cipherData.Length - 17];
            Buffer.BlockCopy(cipherData, 17, encryptedBytes, 0, encryptedBytes.Length);

            aes.IV = iv;

            try
            {
                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(iv);
                CryptographicOperations.ZeroMemory(encryptedBytes);
            }
        });
    }
}
