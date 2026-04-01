using System.Text.Json;
using NekoT.Models.Responses;

namespace NekoT.Core.Forwarding;

public class StreamHandler
{
    public async Task<Usage> HandleStreamAsync(IAsyncEnumerable<string> stream, CancellationToken cancellationToken = default)
    {
        var totalUsage = new Usage();

        await foreach (var line in stream.WithCancellation(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                continue;

            var jsonPart = line.Substring(5).Trim();
            if (jsonPart == "[DONE]")
                continue;

            try
            {
                using var doc = JsonDocument.Parse(jsonPart);
                if (doc.RootElement.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("total_tokens", out var totalElem))
                        totalUsage.TotalTokens = totalElem.GetInt32();
                    if (usage.TryGetProperty("prompt_tokens", out var promptElem))
                        totalUsage.PromptTokens = promptElem.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var completionElem))
                        totalUsage.CompletionTokens = completionElem.GetInt32();
                }
            }
            catch { }
        }

        return totalUsage;
    }
}
