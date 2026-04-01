using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NekoT.Core.Storage;

public class EncryptedDatabase
{
    private readonly string _dbPath;
    private readonly byte[] _key;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public EncryptedDatabase(string dbPath, byte[] key)
    {
        _dbPath = dbPath;
        _key = key;
    }

    public void Save<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        var encrypted = Encrypt(json);
        File.WriteAllBytes(_dbPath, encrypted);
    }

    public T? Load<T>()
    {
        if (!File.Exists(_dbPath)) return default;
        var encrypted = File.ReadAllBytes(_dbPath);
        var json = Decrypt(encrypted);
        return JsonSerializer.Deserialize<T>(json);
    }

    private byte[] Encrypt(string plainText)
    {
        using var aesGcm = new AesGcm(_key, TagSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[1 + nonce.Length + tag.Length + cipherBytes.Length];
        result[0] = 1;
        Buffer.BlockCopy(nonce, 0, result, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, 1 + nonce.Length + tag.Length, cipherBytes.Length);

        return result;
    }

    private string Decrypt(byte[] cipherData)
    {
        if (cipherData.Length < 1 + NonceSize + TagSize)
            throw new CryptographicException("Invalid cipher data");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[cipherData.Length - 1 - NonceSize - TagSize];

        Buffer.BlockCopy(cipherData, 1, nonce, 0, NonceSize);
        Buffer.BlockCopy(cipherData, 1 + NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(cipherData, 1 + NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}