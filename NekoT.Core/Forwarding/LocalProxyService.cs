using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using NekoT.Core.Configuration;
using NekoT.Core.Http;
using NekoT.Models.Requests;
using NekoT.Models.Responses;

namespace NekoT.Core.Forwarding;

public class LocalProxyService
{
    private readonly HttpClient _httpClient;
    
    private static readonly TraceSource Logger = new("NekoT.LocalProxy") 
    { 
        Switch = { Level = SourceLevels.Warning } 
    };

    private static readonly string[] BlockedHostNames = AppConstants.BlockedHosts.MetadataEndpoints;

    public LocalProxyService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? HttpClientManager.GetSharedClient();
    }

    public async Task<ChatCompletionResponse> ForwardToLocalAsync(ChatCompletionRequest request)
    {
        var localEndpoint = AppConstants.Network.LocalEndpoint;
        var forwardRequest = new
        {
            model = request.Model,
            messages = request.Messages,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(localEndpoint, forwardRequest);

        if (!response.IsSuccessStatusCode)
        {
            return new ChatCompletionResponse
            {
                Usage = null,
                Error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
            };
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
        return result ?? new ChatCompletionResponse { Usage = null, Error = "Failed to parse response" };
    }

    public class SafeHttpMessageHandler : DelegatingHandler
    {
        private readonly bool _skipLocalLoopbackCheck;

        public SafeHttpMessageHandler(bool skipLocalLoopbackCheck = false) 
            : base(new HttpClientHandler())
        {
            _skipLocalLoopbackCheck = skipLocalLoopbackCheck;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri == null)
                throw new SecurityException("Invalid request URI");

            await ValidateAndResolveTargetAsync(uri, cancellationToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task ValidateAndResolveTargetAsync(Uri uri, CancellationToken cancellationToken)
        {
            var host = uri.Host.ToLowerInvariant();

            foreach (var blocked in BlockedHostNames)
            {
                if (host.Equals(blocked, StringComparison.OrdinalIgnoreCase))
                    throw new SecurityException($"Blocked hostname: {host}");
            }

            if (IPAddress.TryParse(host, out var ipAddress))
            {
                if (IsPrivateOrReservedIP(ipAddress, skipLocalLoopback: _skipLocalLoopbackCheck))
                    throw new SecurityException($"Access to private/reserved IP blocked: {ipAddress}");
                return;
            }

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new SecurityException($"DNS resolution failed for {host}: {ex.Message}");
            }

            foreach (var addr in addresses)
            {
                if (IsPrivateOrReservedIP(addr, skipLocalLoopback: _skipLocalLoopbackCheck))
                {
                    throw new SecurityException(
                        $"DNS rebinding attack detected: {host} resolves to private/reserved IP {addr}");
                }
            }

            if (!IsDomainSafe(host))
                throw new SecurityException($"Unsafe domain: {host}");
        }
    }

    [Obsolete("此方法存在TOCTOU漏洞，请使用SafeHttpMessageHandler进行连接时验证。此方法仅供内部使用，将在未来版本中移除。")]
    internal static bool IsRequestTargetSafe(string url, bool validateDnsRebinding = true, bool skipLocalLoopback = false)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            foreach (var blocked in BlockedHostNames)
            {
                if (host.Equals(blocked, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                if (validateDnsRebinding && !IsDomainSafe(host))
                    return false;

                try
                {
                    var hostEntry = Dns.GetHostEntry(host);
                    foreach (var ip in hostEntry.AddressList)
                    {
                        if (IsPrivateOrReservedIP(ip, skipLocalLoopback))
                            return false;
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }

            return !IsPrivateOrReservedIP(ipAddress, skipLocalLoopback);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDomainSafe(string host)
    {
        if (IPAddress.TryParse(host, out _))
            return false;

        var dangerousPatterns = new[]
        {
            ".local", ".internal", ".private", "localhost"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (host.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    internal static bool IsPrivateOrReservedIP(IPAddress ip, bool skipLocalLoopback = false)
    {
        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            uint ipUint = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);

            if ((ipUint & 0xFF000000) == 0x00000000) return !skipLocalLoopback;
            if ((ipUint & 0xFF000000) == 0x7F000000) return !skipLocalLoopback;
            if ((ipUint & 0xFF000000) == 0x0A000000) return true;
            if ((ipUint & 0xFFF00000) == 0xAC100000) return true;
            if ((ipUint & 0xFFFF0000) == 0xC0A80000) return true;
            if ((ipUint & 0xFFFF0000) == 0xA9FE0000) return true;
            if (ipUint == 0xFFFFFFFF) return true;
            if ((ipUint & 0xFFC00000) == 0x64400000) return true;
            if ((ipUint & 0xFFFFFF00) == 0xC0000200) return true;
            if ((ipUint & 0xFFFFFF00) == 0xC6336400) return true;
            if ((ipUint & 0xFFFFFF00) == 0xCB007100) return true;
            if ((ipUint & 0xFFFFFF00) == 0xC0000000) return true;

            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.Equals(IPAddress.IPv6Loopback)) return !skipLocalLoopback;
            if (ip.Equals(IPAddress.IPv6None)) return true;
            if (bytes[0] == 0xfe && (bytes[1] & 0xC0) == 0x80) return true;
            if ((bytes[0] & 0xFE) == 0xfc) return true;
            if ((bytes[0] & 0xFF) == 0xff) return true;
            if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8) return true;

            return false;
        }

        return true;
    }
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}