using System.Net.Http;

namespace NekoT.Core.Http;

public sealed class HttpClientManager
{
    private static readonly Lazy<HttpClientManager> _instance = new(() => new HttpClientManager());
    private readonly HttpClient _httpClient;

    public static HttpClientManager Instance => _instance.Value;
    public HttpClient HttpClient => _httpClient;

    private HttpClientManager()
    {
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 100,
            EnableMultipleHttp2Connections = true
        })
        {
            Timeout = TimeSpan.FromSeconds(100)
        };
    }

    public static HttpClient GetSharedClient() => Instance.HttpClient;
}