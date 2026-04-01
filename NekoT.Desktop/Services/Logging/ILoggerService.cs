using System;

namespace NekoT.Desktop.Services.Logging;

public enum LogLevel { Debug, Info, Warning, Error }

public interface ILoggerService : IDisposable
{
    LogLevel MinLogLevel { get; set; }
    void Log(string category, string message, LogLevel level = LogLevel.Info);
    void LogError(string category, string message, Exception? ex = null);
    void LogInfo(string category, string message);
    void LogDebug(string category, string message);
    void LogWarning(string category, string message);
}

public static class LoggerService
{
    private static readonly Lazy<FileLoggerService> _instance = new(() =>
        new FileLoggerService(AppDomain.CurrentDomain.BaseDirectory));

    public static ILoggerService Instance => _instance.Value;
}
