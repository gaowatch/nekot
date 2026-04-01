using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using NekoT.Models.Responses;

namespace NekoT.Core.Storage;

public class EncryptedDatabase : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly string _password;
    private SqliteConnection? _connection;
    private byte[]? _dynamicSalt;
    private const int SaltVersion = 2;
    private byte[]? _cachedKey;
    private readonly object _keyLock = new object();

    public EncryptedDatabase(string databasePath, string password)
    {
        _connectionString = $"Data Source={databasePath};Pooling=true;Cache=Shared;Max Pool Size=5";
        _password = password;
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();

        var createTableCmd = _connection.CreateCommand();
        createTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Usage (
                Id TEXT PRIMARY KEY,
                Model TEXT NOT NULL,
                PromptTokens INTEGER NOT NULL,
                CompletionTokens INTEGER NOT NULL,
                TotalTokens INTEGER NOT NULL,
                Timestamp TEXT NOT NULL,
                Source TEXT,
                EncryptedData BLOB
            );
            CREATE TABLE IF NOT EXISTS Metadata (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )";
        await createTableCmd.ExecuteNonQueryAsync();

        await InitializeSaltAsync();
    }

    private async Task InitializeSaltAsync()
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Metadata WHERE Key = 'SaltVersion'";
        var result = await cmd.ExecuteScalarAsync();

        if (result != null && int.TryParse(result.ToString(), out var version))
        {
            if (version >= 2)
            {
                var saltCmd = _connection.CreateCommand();
                saltCmd.CommandText = "SELECT Value FROM Metadata WHERE Key = 'Salt'";
                var saltBase64 = await saltCmd.ExecuteScalarAsync() as string;
                if (!string.IsNullOrEmpty(saltBase64))
                {
                    _dynamicSalt = Convert.FromBase64String(saltBase64);
                    return;
                }
            }
        }

        _dynamicSalt = GenerateDynamicSalt();
        await SaveSaltAsync(_dynamicSalt, SaltVersion);
    }

    private static byte[] GenerateDynamicSalt()
    {
        var salt = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private async Task SaveSaltAsync(byte[] salt, int version)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Metadata (Key, Value) VALUES ('SaltVersion', $version);
            INSERT OR REPLACE INTO Metadata (Key, Value) VALUES ('Salt', $salt)";
        cmd.Parameters.AddWithValue("$version", version.ToString());
        cmd.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveUsageAsync(UsageRecord usage)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var dataToEncrypt = $"{usage.TotalTokens}|{usage.PromptTokens}|{usage.CompletionTokens}";
        var encrypted = Encrypt(dataToEncrypt);

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Usage (Id, Model, PromptTokens, CompletionTokens, TotalTokens, Timestamp, Source, EncryptedData)
            VALUES ($id, $model, $prompt, $completion, $total, $timestamp, $source, $encrypted)";

        cmd.Parameters.AddWithValue("$id", usage.Id);
        cmd.Parameters.AddWithValue("$model", usage.Model);
        cmd.Parameters.AddWithValue("$prompt", usage.PromptTokens);
        cmd.Parameters.AddWithValue("$completion", usage.CompletionTokens);
        cmd.Parameters.AddWithValue("$total", usage.TotalTokens);
        cmd.Parameters.AddWithValue("$timestamp", usage.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("$source", usage.Source ?? "");
        cmd.Parameters.AddWithValue("$encrypted", encrypted);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<UsageRecord>> GetUsageByTimeRangeAsync(DateTime start, DateTime end)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var results = new List<UsageRecord>();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Usage WHERE Timestamp >= $start AND Timestamp <= $end ORDER BY Timestamp DESC";
        cmd.Parameters.AddWithValue("$start", start.ToString("O"));
        cmd.Parameters.AddWithValue("$end", end.ToString("O"));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new UsageRecord
            {
                Id = reader.GetString(0),
                Model = reader.GetString(1),
                PromptTokens = reader.GetInt32(2),
                CompletionTokens = reader.GetInt32(3),
                TotalTokens = reader.GetInt32(4),
                Timestamp = DateTime.Parse(reader.GetString(5)),
                Source = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return results;
    }

    private byte[] GetKey(byte[]? salt = null, bool isLegacy = false)
    {
        lock (_keyLock)
        {
            if (_cachedKey != null && salt == null && !isLegacy)
            {
                return _cachedKey;
            }
            
            byte[] derivedSalt;
            if (isLegacy)
            {
                derivedSalt = Array.Empty<byte>();
            }
            else if (salt != null)
            {
                derivedSalt = salt;
            }
            else
            {
                derivedSalt = _dynamicSalt ?? Array.Empty<byte>();
            }
            
            var key = Rfc2898DeriveBytes.Pbkdf2(_password, derivedSalt, 100000, HashAlgorithmName.SHA256, 32);
            
            if (salt == null && !isLegacy)
            {
                _cachedKey = key;
            }
            
            return key;
        }
    }

    private byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = GetKey();
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
