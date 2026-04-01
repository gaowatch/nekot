namespace NekoT.Desktop.ViewModels;

public static class ForwardingServiceConfig
{
    public const string ProxyAddress = "http://127.0.0.1:8787";
    public const string StatsEndpoint = "http://127.0.0.1:8788/stats";
    public const int StatsPollingIntervalMs = 1000;
    public const int HttpClientTimeoutSeconds = 2;
    public const string ServiceType = "ForwardingService";
}