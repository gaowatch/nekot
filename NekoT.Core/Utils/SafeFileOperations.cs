using System;
using System.IO;

namespace NekoT.Core.Utils;

public static class SafeFileOperations
{
    private const int MaxRetryCount = 3;
    private const int RetryDelayMs = 100;

    public static bool WriteAllTextWithRetry(string path, string content)
    {
        for (int i = 0; i < MaxRetryCount; i++)
        {
            try
            {
                File.WriteAllText(path, content);
                return true;
            }
            catch (IOException) when (i < MaxRetryCount - 1)
            {
                System.Threading.Thread.Sleep(RetryDelayMs);
            }
        }
        return false;
    }

    public static string? ReadAllTextWithRetry(string path)
    {
        for (int i = 0; i < MaxRetryCount; i++)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException) when (i < MaxRetryCount - 1)
            {
                System.Threading.Thread.Sleep(RetryDelayMs);
            }
        }
        return null;
    }

    public static bool DeleteWithRetry(string path)
    {
        for (int i = 0; i < MaxRetryCount; i++)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (IOException) when (i < MaxRetryCount - 1)
            {
                System.Threading.Thread.Sleep(RetryDelayMs);
            }
        }
        return false;
    }
}