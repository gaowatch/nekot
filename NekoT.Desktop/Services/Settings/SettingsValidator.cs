using System;
using System.Linq;
using System.Threading.Tasks;
using NekoT.Core.Contracts;
using Res = NekoT.Desktop.Resources.Strings;

namespace NekoT.Desktop.Services.Settings;

public class SettingsValidator : ISettingsValidator
{
    private static readonly string[] DangerousProtocols = { "javascript:", "vbscript:", "data:", "file:", "ftp:" };

    public Task<ValidationResult> ValidateAsync(string key, object? value)
    {
        return Task.FromResult(key switch
        {
            "HomePage" => ValidateUrl(value as string, "HomePage", 2048),
            "ProxyUrl" => ValidateProxyUrl(value as string),
            "UserAgent" => ValidateUserAgent(value as string),
            _ => ValidationResult.Success(key)
        });
    }

    public T? Sanitize<T>(string key, T? value)
    {
        if (value == null) return default;
        return key switch
        {
            "HomePage" or "ProxyUrl" => (T?)(object?)SanitizeUrl(value?.ToString()),
            "UserAgent" => (T?)(object?)SanitizeUserAgent(value?.ToString()),
            _ => value
        };
    }

    private ValidationResult ValidateUrl(string? url, string propertyName, int maxLength)
    {
        if (string.IsNullOrEmpty(url) || url == "about:blank") return ValidationResult.Success(propertyName);
        if (url.Length > maxLength) return ValidationResult.Failure(propertyName, string.Format(Res.Validation_UrlTooLong, maxLength));
        var lowerUrl = url.ToLowerInvariant();
        foreach (var protocol in DangerousProtocols) { if (lowerUrl.StartsWith(protocol)) return ValidationResult.Failure(propertyName, string.Format(Res.Validation_UrlInvalidProtocol, protocol)); }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return ValidationResult.Failure(propertyName, Res.Validation_UrlInvalidFormat);
        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase)) return ValidationResult.Failure(propertyName, Res.Validation_ProtocolNotSupported);
        return ValidationResult.Success(propertyName);
    }

    private ValidationResult ValidateProxyUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return ValidationResult.Success("ProxyUrl");
        if (url.Length > 512) return ValidationResult.Failure("ProxyUrl", Res.Validation_ProxyUrlTooLong);
        var lowerUrl = url.ToLowerInvariant();
        foreach (var protocol in DangerousProtocols) { if (lowerUrl.StartsWith(protocol)) return ValidationResult.Failure("ProxyUrl", string.Format(Res.Validation_ProxyUrlInvalidProtocol, protocol)); }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return ValidationResult.Failure("ProxyUrl", Res.Validation_ProxyUrlInvalidFormat);
        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("socks", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("socks5", StringComparison.OrdinalIgnoreCase)) return ValidationResult.Failure("ProxyUrl", Res.Validation_ProxyProtocolNotSupported);
        return ValidationResult.Success("ProxyUrl");
    }

    private ValidationResult ValidateUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return ValidationResult.Success("UserAgent");
        if (userAgent.Length > 500) return ValidationResult.Failure("UserAgent", Res.Validation_UserAgentTooLong);
        var dangerousChars = new[] { '<', '>', '"', '\'', '\n', '\r', '\0' };
        if (userAgent.IndexOfAny(dangerousChars) >= 0) return ValidationResult.Failure("UserAgent", Res.Validation_UserAgentInvalidChars);
        return ValidationResult.Success("UserAgent");
    }

    private string? SanitizeUrl(string? url) { if (string.IsNullOrWhiteSpace(url)) return url; return url.Trim().Replace("\0", string.Empty); }
    private string? SanitizeUserAgent(string? userAgent) { if (string.IsNullOrWhiteSpace(userAgent)) return userAgent; var sanitized = new string(userAgent.Where(c => !char.IsControl(c) && c != '<' && c != '>' && c != '"' && c != '\'').ToArray()); return sanitized.Length > 500 ? sanitized.Substring(0, 500) : sanitized; }
}