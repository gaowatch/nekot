using System;
using System.IO;
using FluentAssertions;
using NekoT.Core.Security;
using Xunit;

namespace NekoT.Tests.Security;

public class SecureStorageTests : IDisposable
{
    private readonly string _testStoragePath;

    public SecureStorageTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "NekoT", $"secure_test_{Guid.NewGuid()}.dat");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testStoragePath))
            {
                File.Delete(_testStoragePath);
            }
        }
        catch { }
    }

    [Fact]
    public void SaveApiKey_ShouldSaveEncryptedKey()
    {
        var storage = new SecureStorage(_testStoragePath);
        var provider = "MiniMax-M2.5";
        var apiKey = "placeholder-key-for-test";

        storage.SaveApiKey(provider, apiKey);

        File.Exists(_testStoragePath).Should().BeTrue();
        var savedKey = storage.GetApiKey(provider);
        savedKey.Should().Be(apiKey);
    }

    [Fact]
    public void GetApiKey_ShouldReturnNull_WhenKeyNotExists()
    {
        var storage = new SecureStorage(_testStoragePath);

        var result = storage.GetApiKey("NonExistentProvider");

        result.Should().BeNull();
    }

    [Fact]
    public void SaveApiKey_ShouldOverwriteExistingKey()
    {
        var storage = new SecureStorage(_testStoragePath);
        var provider = "MiniMax-M2.5";
        var apiKey1 = "placeholder-key-1";
        var apiKey2 = "placeholder-key-2";

        storage.SaveApiKey(provider, apiKey1);
        storage.SaveApiKey(provider, apiKey2);

        var savedKey = storage.GetApiKey(provider);
        savedKey.Should().Be(apiKey2);
    }

    [Fact]
    public void DeleteApiKey_ShouldRemoveKey()
    {
        var storage = new SecureStorage(_testStoragePath);
        var provider = "MiniMax-M2.5";
        var apiKey = "placeholder-key";
        storage.SaveApiKey(provider, apiKey);

        storage.DeleteApiKey(provider);

        storage.GetApiKey(provider).Should().BeNull();
    }

    [Fact]
    public void HasApiKey_ShouldReturnTrue_WhenKeyExists()
    {
        var storage = new SecureStorage(_testStoragePath);
        var provider = "MiniMax-M2.5";
        var apiKey = "placeholder-key";
        storage.SaveApiKey(provider, apiKey);

        var result = storage.HasApiKey(provider);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasApiKey_ShouldReturnFalse_WhenKeyNotExists()
    {
        var storage = new SecureStorage(_testStoragePath);

        var result = storage.HasApiKey("NonExistentProvider");

        result.Should().BeFalse();
    }

    [Fact]
    public void LoadAllKeys_ShouldReturnEmptyDictionary_WhenNoKeysExist()
    {
        var storage = new SecureStorage(_testStoragePath);

        var keys = storage.LoadAllKeys();

        keys.Should().BeEmpty();
    }

    [Fact]
    public void LoadAllKeys_ShouldReturnAllKeys()
    {
        var storage = new SecureStorage(_testStoragePath);
        storage.SaveApiKey("Provider1", "key1");
        storage.SaveApiKey("Provider2", "key2");

        var keys = storage.LoadAllKeys();

        keys.Count.Should().Be(2);
        keys["Provider1"].Should().Be("key1");
        keys["Provider2"].Should().Be("key2");
    }
}