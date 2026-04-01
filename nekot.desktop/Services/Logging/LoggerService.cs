using System;

namespace NekoT.Desktop.Services.Logging;

public static class LoggerService
{
    private static readonly Lazy<FileLoggerService> _instance = new(() =>
        new FileLoggerService(AppDomain.CurrentDomain.BaseDirectory));

    public static ILoggerService Instance => _instance.Value;
}