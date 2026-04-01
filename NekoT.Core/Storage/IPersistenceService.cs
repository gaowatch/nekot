using System;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public interface IPersistenceService { void MarkDirty(TokenUsageData data); Task<TokenUsageData> LoadAsync(); Task OnShutdownAsync(); event EventHandler? DayChanged; }

public class TokenUsageData { public int Version { get; set; } = 1; public int LatestTokenCount { get; set; } public int TodayTokenCount { get; set; } public int TodayRequestCount { get; set; } public decimal SessionCost { get; set; } public decimal TotalCost { get; set; } public System.Collections.Generic.List<BarDataPointInfo>? BarDataPoints { get; set; } public DateTime LastSavedTime { get; set; } public DateTime LastRecordDate { get; set; } }

public class BarDataPointInfo { public DateTime Time { get; set; } public int TokenCount { get; set; } public string? Label { get; set; } }