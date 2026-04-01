using System;
using System.Collections.ObjectModel;
using NekoT.Core.TokenManagement;

namespace NekoT.Core.Contracts;

public interface ITokenService
{
    int TotalTokens { get; }
    int SessionTokens { get; }
    ObservableCollection<UsageRecord> UsageRecords { get; }

    void RecordUsage(int tokens, string? provider = null, string? requestId = null);
    void ResetSession();
    TokenStatistics GetStatistics();
    System.Collections.Generic.Dictionary<string, int> GetProviderBreakdown();
}