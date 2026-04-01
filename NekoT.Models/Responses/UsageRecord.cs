namespace NekoT.Models.Responses;

public class UsageRecord
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; }
}