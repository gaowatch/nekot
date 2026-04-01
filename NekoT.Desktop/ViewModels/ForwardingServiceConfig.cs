using System.Collections.Generic;

namespace NekoT.Desktop.ViewModels;

public class ForwardingServiceConfig
{
    public List<ProviderEndpoint> Endpoints { get; set; } = new();
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
}

public class ProviderEndpoint
{
    public string ProviderName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}