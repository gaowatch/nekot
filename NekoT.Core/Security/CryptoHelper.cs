using System;
using System.Security.Cryptography;
using System.Text;

namespace NekoT.Core.Security;

public static class CryptoHelper
{
    public static byte[] DeriveKey(string password, byte[] salt, int iterations = 100000, int keySize = 32)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keySize);
    }

    public static byte[] GenerateSalt(int size = 16)
    {
        var salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    public static string HashPassword(string password, byte[] salt, int iterations = 100000)
    {
        var hash = DeriveKey(password, salt, iterations);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string hash, byte[] salt, int iterations = 100000)
    {
        var computedHash = HashPassword(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash));
    }
}