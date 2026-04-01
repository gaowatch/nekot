using System;
using System.Collections.Generic;

namespace NekoT.Core.Contracts;

public interface IAuditLogger
{
    void LogSettingChange(string settingName, object? oldValue, object? newValue);
    void LogAction(AuditAction action, string? additionalInfo = null);
    void LogValidationFailure(string settingName, string errorMessage, string? inputValue = null);
    IEnumerable<AuditLogEntry> GetRecentLogs(int count = 100);
}

public enum AuditAction { SettingChanged, SettingSaved, SettingReset, DataExported, DataImported, DataCleared, SecurityEvent, ValidationFailed }

public class AuditLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AuditAction Action { get; set; }
    public string? SettingName { get; set; }
    public string? OldValueHash { get; set; }
    public string? NewValueHash { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? UserIdentity { get; set; }
}