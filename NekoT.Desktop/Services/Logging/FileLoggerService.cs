using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;

namespace NekoT.Desktop.Services.Logging;

public class FileLoggerService : ILoggerService
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly string _baseDirectory;
    private LogLevel _minLogLevel = LogLevel.Info;
    private bool _disposed;

    public LogLevel MinLogLevel
    {
        get => _minLogLevel;
        set => _minLogLevel = value;
    }

    public FileLoggerService(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    private FileLogger GetOrCreateLogger(string logFileName)
    {
        return _loggers.GetOrAdd(logFileName, fileName =>
        {
            var logFile = Path.Combine(_baseDirectory, fileName);
            return new FileLogger(logFile);
        });
    }

    public void Log(string category, string message, LogLevel level = LogLevel.Info)
    {
        if (_disposed) return;
        
        if (level < _minLogLevel)
            return;

        var logger = GetOrCreateLogger($"{category}.log");
        var levelStr = level.ToString().ToUpper();
        logger.Log($"[{levelStr}] {message}");
    }

    public void LogError(string category, string message, Exception? ex = null)
    {
        if (_disposed) return;
        
        var logger = GetOrCreateLogger($"{category}.log");
        if (ex != null)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"[ERROR] {message}");
            errorBuilder.AppendLine($"  Exception Type: {ex.GetType().FullName}");
            errorBuilder.AppendLine($"  Message: {ex.Message}");
            
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                errorBuilder.AppendLine($"  Stack Trace:");
                errorBuilder.AppendLine(IndentText(ex.StackTrace, "    "));
            }
            
            var innerEx = ex.InnerException;
            var level = 1;
            while (innerEx != null)
            {
                errorBuilder.AppendLine($"  Inner Exception [{level}]:");
                errorBuilder.AppendLine($"    Type: {innerEx.GetType().FullName}");
                errorBuilder.AppendLine($"    Message: {innerEx.Message}");
                
                if (!string.IsNullOrEmpty(innerEx.StackTrace))
                {
                    errorBuilder.AppendLine($"    Stack Trace:");
                    errorBuilder.AppendLine(IndentText(innerEx.StackTrace, "      "));
                }
                
                innerEx = innerEx.InnerException;
                level++;
            }
            
            logger.Log(errorBuilder.ToString());
        }
        else
        {
            logger.Log($"[ERROR] {message}");
        }
    }

    public void LogInfo(string category, string message)
    {
        Log(category, message, LogLevel.Info);
    }

    public void LogDebug(string category, string message)
    {
        Log(category, message, LogLevel.Debug);
    }

    public void LogWarning(string category, string message)
    {
        Log(category, message, LogLevel.Warning);
    }

    private static string IndentText(string text, string indent)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
            
        var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Select(line => indent + line));
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        foreach (var logger in _loggers.Values)
        {
            try
            {
                logger.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogError] Failed to dispose logger: {ex.Message}");
            }
        }
        _loggers.Clear();
    }
}