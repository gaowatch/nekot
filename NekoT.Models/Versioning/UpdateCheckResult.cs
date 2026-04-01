namespace NekoT.Models.Versioning;

public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public VersionInfo? LatestVersion { get; set; }
    public string? CurrentVersion { get; set; }
    public bool IsForceUpdate { get; set; }
    public string? UpdateMessage { get; set; }
}

public class VersionInfo
{
    public string Version { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
}