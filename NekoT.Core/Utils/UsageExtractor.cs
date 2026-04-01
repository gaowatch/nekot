using System;
using System.Text.Json;

namespace NekoT.Core.Utils;

public static class UsageExtractor
{
    public static (int input, int output, int total) ExtractFromResponse(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;
            if (root.TryGetProperty("usage", out var usage))
            {
                int input = 0, output = 0, total = 0;
                if (usage.TryGetProperty("prompt_tokens", out var pt)) input = pt.GetInt32();
                else if (usage.TryGetProperty("input_tokens", out var it)) input = it.GetInt32();
                else if (usage.TryGetProperty("input", out var inp)) input = inp.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct)) output = ct.GetInt32();
                else if (usage.TryGetProperty("output_tokens", out var ot)) output = ot.GetInt32();
                else if (usage.TryGetProperty("output", out var outp)) output = outp.GetInt32();
                if (usage.TryGetProperty("total_tokens", out var tt)) total = tt.GetInt32();
                else if (usage.TryGetProperty("total", out var tot)) total = tot.GetInt32();
                else total = input + output;
                return (input, output, total);
            }
        }
        catch { }
        return (0, 0, 0);
    }
}