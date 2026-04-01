using System.Threading.Tasks;
using NekoT.Core.Contracts;

namespace NekoT.Desktop.Services.Settings;

public class SettingsValidator : ISettingsValidator
{
    private static readonly string[] DangerousProtocols = { "javascript:", "vbscript:", "data:", "file:", "ftp:" };

    public Task<ValidationResult> ValidateAsync(string key, object? value) => Task.FromResult(key switch
    {
        "HomePage" => ValidateUrl(value as string, "HomePage", 2048),
        "ProxyUrl" => ValidateProxyUrl(value as string),
        "UserAgent" => ValidateUserAgent(value as string),
        _ => ValidationResult.Success(key)
    });

    public T? Sanitize<T>(string key, T? value) => value;

    private ValidationResult ValidateUrl(string? url, string propertyName, int maxLength)
    {
        if (string.IsNullOrEmpty(url) || url == "about:blank") return ValidationResult.Success(propertyName);
        if (url.Length > maxLength) return ValidationResult.Failure(propertyName, "URL too long");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return ValidationResult.Failure(propertyName, "Invalid URL format");
        if (!uri.Scheme.Equals("http") && !uri.Scheme.Equals("https") && !uri.Scheme.Equals("about"))
            return ValidationResult.Failure(propertyName, "Unsupported protocol");
        return ValidationResult.Success(propertyName);
    }

    private ValidationResult ValidateProxyUrl(string? url) => string.IsNullOrEmpty(url) ? ValidationResult.Success("ProxyUrl") : ValidationResult.Success("ProxyUrl");
    private ValidationResult ValidateUserAgent(string? userAgent) => ValidationResult.Success("UserAgent");
}
