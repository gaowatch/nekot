namespace NekoT.Models.Versioning;

public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public VersionInfo? LatestVersion { get; set; }
    public string? CurrentVersion { get; set; }
    public bool IsForceUpdate { get; set; }
    public string? UpdateMessage { get; set; }
}