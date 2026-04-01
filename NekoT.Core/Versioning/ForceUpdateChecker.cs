using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NekoT.Core.Http;
using NekoT.Models.Versioning;

namespace NekoT.Core.Versioning;

public class ForceUpdateChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _forceUpdateApiUrl;

    public ForceUpdateChecker(IConfiguration configuration)
    {
        _httpClient = HttpClientManager.GetSharedClient();
        _forceUpdateApiUrl = configuration["Update:ForceUpdateApi"] 
            ?? throw new InvalidOperationException("Update:ForceUpdateApi 配置缺失");
    }

    public async Task<UpdateCheckResult> CheckForceUpdateAsync(string currentVersion)
    {
        try
        {
            var url = $"{_forceUpdateApiUrl}?current_version={currentVersion}";
            var versionInfo = await _httpClient.GetFromJsonAsync<VersionInfo>(url);

            if (versionInfo == null)
            {
                return new UpdateCheckResult { HasUpdate = false };
            }

            var comparison = CompareVersions(currentVersion, versionInfo.Version);
            bool isForceUpdate = versionInfo.UpdateType == UpdateType.Forced ||
                (!string.IsNullOrEmpty(versionInfo.ForceUpdateFromVersion) &&
                 CompareVersions(currentVersion, versionInfo.ForceUpdateFromVersion) <= 0);

            return new UpdateCheckResult
            {
                HasUpdate = comparison < 0,
                LatestVersion = versionInfo,
                CurrentVersion = currentVersion,
                IsForceUpdate = isForceUpdate,
                UpdateMessage = GenerateUpdateMessage(versionInfo, isForceUpdate)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ForceUpdateChecker] 检查失败: {ex.Message}");
            return new UpdateCheckResult { HasUpdate = false };
        }
    }

    private int CompareVersions(string v1, string v2)
    {
        try
        {
            var version1 = new Version(v1);
            var version2 = new Version(v2);
            return version1.CompareTo(version2);
        }
        catch
        {
            return string.Compare(v1, v2, StringComparison.Ordinal);
        }
    }

    private string GenerateUpdateMessage(VersionInfo version, bool isForce)
    {
        var header = isForce ? "发现重要更新（必须更新）" : "发现新版本";
        return $"{header} {version.Version}\n\n{version.ReleaseNotes}";
    }
}