namespace NekoT.Core.Contracts;

public interface IAuditLogger
{
    void LogSettingChange(string settingName, object? oldValue, object? newValue);
    void LogAction(AuditAction action, string? additionalInfo = null);
    void LogValidationFailure(string settingName, string errorMessage, string? inputValue = null);
    IEnumerable<AuditLogEntry> GetRecentLogs(int count = 100);
}

public enum AuditAction
{
    ApplicationStarted,
    ApplicationStopped,
    SettingsChanged,
    SettingsExported,
    SettingsImported,
    UpdateCheck,
    UpdateDownloaded,
    UpdateInstalled,
    LanguageChanged,
    ProxyStarted,
    ProxyStopped,
    ProviderAdded,
    ProviderRemoved,
    ProviderUpdated,
    DataCleared,
    ErrorOccurred
}

public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public AuditAction Action { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? SettingName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ErrorMessage { get; set; }
}