using System;

namespace NekoT.Models.Versioning;

public enum UpdateType { Optional = 0, Recommended = 1, Forced = 2 }

public class VersionInfo
{
    public string Version { get; set; } = "0.1.0";
    public DateTime ReleaseDate { get; set; }
    public string? ReleaseNotes { get; set; }
    public UpdateType UpdateType { get; set; } = UpdateType.Optional;
    public string? MinVersion { get; set; }
    public string? ForceUpdateFromVersion { get; set; }
    public string? GitHubReleaseUrl { get; set; }
}

public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public VersionInfo? LatestVersion { get; set; }
    public bool IsForceUpdate { get; set; }
    public string? UpdateMessage { get; set; }
}