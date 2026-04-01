using System.Net;
using System.Net.Sockets;

namespace NekoT.Core.Hosting;

public class ServiceHostManager
{
    private readonly int _defaultPort;
    public ServiceHostManager(int defaultPort = 8787) { _defaultPort = defaultPort; }
    public int DefaultPort => _defaultPort;

    public int FindAvailablePort() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    public bool IsPortAvailable(int port) { try { using var listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); listener.Stop(); return true; } catch { return false; } }

    public async Task<bool> IsServiceRunning(int port)
    {
        try { using var client = new TcpClient(); var connectTask = client.ConnectAsync(IPAddress.Loopback, port); var timeoutTask = Task.Delay(1000); var completedTask = await Task.WhenAny(connectTask, timeoutTask); return completedTask == timeoutTask || client.Connected; }
        catch { return false; }
    }

    public async Task<bool> IsServiceRunning() => await IsServiceRunning(_defaultPort);
    public string GetServiceUrl(int port) => $"http://127.0.0.1:{port}";
    public string GetServiceUrl() => GetServiceUrl(_defaultPort);
    public async Task<int> GetAvailablePortOrDefaultAsync() => await IsServiceRunning(_defaultPort) ? _defaultPort : FindAvailablePort();
}