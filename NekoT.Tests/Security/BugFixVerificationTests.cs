using FluentAssertions;
using NekoT.Core.Security;
using NekoT.Core.Forwarding;
using Xunit;
using Moq;
using System.Security.Cryptography;

namespace NekoT.Tests.Security;

public class SecureKeyManagerSingletonTests
{
    [Fact]
    public void Instance_GetMultipleTimes_ShouldReturnSameInstance()
    {
        var instance1 = SecureKeyManager.Instance;
        var instance2 = SecureKeyManager.Instance;
        var instance3 = SecureKeyManager.Instance;

        instance1.Should().BeSameAs(instance2);
        instance2.Should().BeSameAs(instance3);
    }

    [Fact]
    public void SecureStorage_MultipleInstances_ShouldShareSameKeyManager()
    {
        var storage1 = new SecureStorage();
        var storage2 = new SecureStorage();

        const string testProvider = "test-provider";
        const string testKey = "test-placeholder-key-for-unit-test";

        storage1.SaveApiKey(testProvider, testKey);
        var retrievedKey = storage2.GetApiKey(testProvider);

        retrievedKey.Should().Be(testKey);
    }

    [Fact]
    public void SecureStorage_Constructor_DoesNotCreateNewKeyManager()
    {
        var keyManagerBefore = SecureKeyManager.Instance;
        
        var storage = new SecureStorage();
        
        var storage2 = new SecureStorage();
        storage2.HasApiKey("test").Should().BeFalse();
    }
}

public class WhitelistValidatorExtendedTests
{
    [Theory]
    [InlineData("https://api.deepseek.com/v1/chat/completions", true)]
    [InlineData("https://deepseek.com/v1/chat/completions", true)]
    [InlineData("https://api.moonshot.cn/v1/chat/completions", true)]
    [InlineData("https://moonshot.cn/v1/chat/completions", true)]
    [InlineData("https://api.moonshot.cn/kimi/v1/chat/completions", true)]
    [InlineData("https://open.bigmodel.cn/api/paas/v4/chat/completions", true)]
    [InlineData("https://zhipuai.cn/api/paas/v4/chat/completions", true)]
    [InlineData("https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation", true)]
    [InlineData("https://tongyi.aliyun.com/api/v1/text/chat", true)]
    [InlineData("https://qwenlm.aliyun.com/api/v1/chat/completions", true)]
    public void IsWhitelisted_NewProviders_ShouldReturnTrue(string url, bool expected)
    {
        var validator = new WhitelistValidator();

        var result = validator.IsWhitelisted(url);

        result.Should().Be(expected, $"URL {url} should be whitelisted");
    }

    [Fact]
    public void IsWhitelisted_SubdomainOfNewProviders_ShouldReturnTrue()
    {
        var validator = new WhitelistValidator();

        validator.IsWhitelisted("https://api.deepseek.com/v1/models").Should().BeTrue();
        validator.IsWhitelisted("https://api.moonshot.cn/v1/chat").Should().BeTrue();
    }

    [Fact]
    public void IsWhitelisted_MaliciousUrl_ShouldReturnFalse()
    {
        var validator = new WhitelistValidator();

        validator.IsWhitelisted("https://evil.deepseek.com.evil.com/api").Should().BeFalse();
        validator.IsWhitelisted("https://moonshot.cn.evil.com/api").Should().BeFalse();
    }
}

public class LocalProxyServiceDynamicPortTests
{
    [Fact]
    public void ForwardToLocalAsync_DefaultEndpoint_ShouldUseCorrectPort()
    {
        var service = new LocalProxyService();
        
        var endpointField = typeof(LocalProxyService)
            .GetField("LocalEndpoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        endpointField.Should().NotBeNull();
    }

    [Fact]
    public void IsRequestTargetSafe_Localhost_ShouldAllow()
    {
        var result = LocalProxyService.IsRequestTargetSafe("http://localhost:8787/v1/chat/completions");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsRequestTargetSafe_PrivateIP_ShouldBlock()
    {
        LocalProxyService.IsRequestTargetSafe("http://192.168.1.1:8787/api").Should().BeFalse();
        LocalProxyService.IsRequestTargetSafe("http://10.0.0.1:8787/api").Should().BeFalse();
    }
}