using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NekoT.Core.Http;
using NekoT.Core.Versioning;
using NekoT.Models.Versioning;

namespace NekoT.Core.Browsing;

public class SquirrelUpdateService : IVersionService
{
    private readonly HttpClient _httpClient;
    private readonly string _updateUrl;
    private readonly string _currentVersion;

    public SquirrelUpdateService(IConfiguration configuration)
    {
        _httpClient = HttpClientManager.GetSharedClient();
        _updateUrl = configuration["Update:Url"] ?? "https://nekot.example.com/update";
        _currentVersion = configuration["App:Version"] ?? "1.0.0";
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        try
        {
            var latestInfo = await _httpClient.GetStringAsync($"{_updateUrl}/latest");
            var latest = System.Text.Json.JsonSerializer.Deserialize<VersionInfo>(latestInfo);

            if (latest == null)
                return new UpdateCheckResult { HasUpdate = false };

            var needsUpdate = CompareVersions(_currentVersion, latest.Version) < 0;
            return new UpdateCheckResult
            {
                HasUpdate = needsUpdate,
                LatestVersion = latest,
                CurrentVersion = _currentVersion
            };
        }
        catch
        {
            return new UpdateCheckResult { HasUpdate = false };
        }
    }

    public string GetCurrentVersion() => _currentVersion;

    public Task<bool> ApplyUpdateAsync()
    {
        return Task.FromResult(false);
    }

    public bool IsUpdateAvailable()
    {
        return CheckForUpdateAsync().Result.HasUpdate;
    }

    private int CompareVersions(string v1, string v2)
    {
        try
        {
            var ver1 = new Version(v1);
            var ver2 = new Version(v2);
            return ver1.CompareTo(ver2);
        }
        catch
        {
            return string.Compare(v1, v2, StringComparison.Ordinal);
        }
    }
}