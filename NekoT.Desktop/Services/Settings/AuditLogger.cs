using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NekoT.Core.Contracts;

namespace NekoT.Desktop.Services.Settings;

public class AuditLogger : IAuditLogger
{
    private static AuditLogger? _instance;
    private static readonly object _lock = new();

    private readonly string _logPath;
    private readonly List<AuditLogEntry> _recentLogs;
    private const int MaxRecentLogs = 500;

    public static AuditLogger Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new AuditLogger();
                return _instance;
            }
        }
    }

    private AuditLogger()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NekoT");

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _logPath = Path.Combine(appDataPath, "audit.log");
        _recentLogs = new List<AuditLogEntry>();
        LoadRecentLogs();
    }

    public void LogSettingChange(string settingName, object? oldValue, object? newValue)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = AuditAction.SettingChanged,
            SettingName = settingName,
            OldValueHash = oldValue != null ? ComputeHash(oldValue.ToString()) : null,
            NewValueHash = newValue != null ? ComputeHash(newValue.ToString()) : null
        };

        AddEntry(entry);
    }

    public void LogAction(AuditAction action, string? additionalInfo = null)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = action,
            AdditionalInfo = additionalInfo
        };

        AddEntry(entry);
    }

    public void LogValidationFailure(string settingName, string errorMessage, string? inputValue = null)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = AuditAction.ValidationFailed,
            SettingName = settingName,
            AdditionalInfo = $"Error: {errorMessage}, Input: {inputValue}"
        };

        AddEntry(entry);
    }

    public IEnumerable<AuditLogEntry> GetRecentLogs(int count = 100)
    {
        return _recentLogs.OrderByDescending(e => e.Timestamp).Take(count).ToList();
    }

    private void AddEntry(AuditLogEntry entry)
    {
        lock (_lock)
        {
            _recentLogs.Add(entry);

            while (_recentLogs.Count > MaxRecentLogs)
            {
                _recentLogs.RemoveAt(0);
            }

            AppendToLog(entry);
        }
    }

    private void AppendToLog(AuditLogEntry entry)
    {
        try
        {
            var line = JsonSerializer.Serialize(entry);
            File.AppendAllLines(_logPath, new[] { line });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuditLogger] Failed to append log: {ex.Message}");
        }
    }

    private void LoadRecentLogs()
    {
        if (!File.Exists(_logPath))
            return;

        try
        {
            var lines = File.ReadAllLines(_logPath);
            foreach (var line in lines.Reverse().Take(MaxRecentLogs))
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<AuditLogEntry>(line);
                    if (entry != null)
                    {
                        _recentLogs.Add(entry);
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuditLogger] Failed to load logs: {ex.Message}");
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash)[..8];
    }
}