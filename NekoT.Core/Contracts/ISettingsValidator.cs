using System.Threading.Tasks;

namespace NekoT.Core.Contracts;

public interface ISettingsValidator
{
    Task<ValidationResult> ValidateAsync(string key, object? value);
    T? Sanitize<T>(string key, T? value);
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public string PropertyName { get; }

    private ValidationResult(bool isValid, string propertyName, string? errorMessage = null)
    {
        IsValid = isValid;
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success(string propertyName) => new(true, propertyName);
    public static ValidationResult Failure(string propertyName, string errorMessage) => new(false, propertyName, errorMessage);
}