using System;

namespace NekoT.Models.Versioning;

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