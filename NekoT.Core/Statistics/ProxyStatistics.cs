using System;
using System.Text.Json;

namespace NekoT.Core.Statistics;

public static class ProxyStatistics
{
    private static readonly object _lock = new();
    private static int _totalRequests;
    private static int _totalInputTokens;
    private static int _totalOutputTokens;
    private static readonly DateTime _startTime = DateTime.Now;

    public static void RecordRequest(int inputTokens, int outputTokens)
    {
        lock (_lock)
        {
            _totalRequests++;
            _totalInputTokens += inputTokens;
            _totalOutputTokens += outputTokens;
        }
    }

    public static object GetStats()
    {
        lock (_lock)
        {
            return new
            {
                total_requests = _totalRequests,
                total_input_tokens = _totalInputTokens,
                total_output_tokens = _totalOutputTokens,
                total_tokens = _totalInputTokens + _totalOutputTokens,
                uptime_seconds = (DateTime.Now - _startTime).TotalSeconds
            };
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _totalRequests = 0;
            _totalInputTokens = 0;
            _totalOutputTokens = 0;
        }
    }
}