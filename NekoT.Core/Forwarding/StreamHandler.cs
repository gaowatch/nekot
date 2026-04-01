using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NekoT.Models.Responses;

namespace NekoT.Core.Forwarding;

public class StreamHandler
{
    private static readonly TraceSource Logger = new("NekoT.StreamHandler") 
    { 
        Switch = { Level = SourceLevels.Warning } 
    };

    public Task<Usage> HandleStreamAsync(IAsyncEnumerable<string> stream)
    {
        return HandleStreamAsync(stream, CancellationToken.None);
    }

    public async Task<Usage> HandleStreamAsync(IAsyncEnumerable<string> stream, CancellationToken cancellationToken = default)
    {
        var totalUsage = new Usage();

        await foreach (var line in stream.WithCancellation(cancellationToken))
        {
            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..];
                
                if (data == "[DONE]")
                    break;

                try
                {
                    var chunk = JsonSerializer.Deserialize<StreamChunk>(data);
                    if (chunk?.Usage != null)
                    {
                        totalUsage = chunk.Usage;
                    }
                }
                catch (JsonException ex)
                {
                    Logger.TraceEvent(TraceEventType.Warning, 0, 
                        $"Failed to parse stream chunk: {ex.Message}. Data: {(data.Length > 100 ? data.Substring(0, 100) + "..." : data)}");
                }
                catch (Exception ex)
                {
                    Logger.TraceEvent(TraceEventType.Error, 1, 
                        $"Unexpected error processing stream chunk: {ex.Message}");
                }
            }
        }

        return totalUsage;
    }
}