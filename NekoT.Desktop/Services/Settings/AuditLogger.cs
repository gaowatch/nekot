using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NekoT.Core.Contracts;

namespace NekoT.Desktop.Services.Settings;

public class AuditLogger : IAuditLogger
{
    private readonly ConcurrentQueue<AuditLogEntry> _logs = new();
    private readonly int _maxLogEntries;

    public AuditLogger(int maxLogEntries = 1000) { _maxLogEntries = maxLogEntries; }

    public void LogSettingChange(string settingName, object? oldValue, object? newValue)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = AuditAction.SettingChanged,
            SettingName = settingName,
            OldValueHash = HashSensitiveValue(oldValue),
            NewValueHash = HashSensitiveValue(newValue),
            AdditionalInfo = $"Setting '{settingName}' changed"
        };
        AddLog(entry);
    }

    public void LogAction(AuditAction action, string? additionalInfo = null)
    {
        var entry = new AuditLogEntry { Timestamp = DateTime.UtcNow, Action = action, AdditionalInfo = additionalInfo };
        AddLog(entry);
    }

    public void LogValidationFailure(string settingName, string errorMessage, string? inputValue = null)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = AuditAction.ValidationFailed,
            SettingName = settingName,
            AdditionalInfo = $"Validation failed: {errorMessage}",
            OldValueHash = inputValue != null ? HashSensitiveValue(inputValue) : null
        };
        AddLog(entry);
    }

    public IEnumerable<AuditLogEntry> GetRecentLogs(int count = 100) => _logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();

    private void AddLog(AuditLogEntry entry) { _logs.Enqueue(entry); while (_logs.Count > _maxLogEntries) _logs.TryDequeue(out _); }

    private static string? HashSensitiveValue(object? value)
    {
        if (value == null) return null;
        if (value is bool || value is int || value is double || value is float || value is decimal) return value.ToString();
        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue)) return null;
        if (stringValue.Length <= 3) return "***";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(stringValue);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash.Take(16).ToArray());
    }
}