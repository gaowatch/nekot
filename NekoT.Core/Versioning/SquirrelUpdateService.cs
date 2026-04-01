using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NekoT.Core.Contracts;
using NekoT.Models.Versioning;
using Squirrel;

namespace NekoT.Core.Versioning;

public class SquirrelUpdateService : IVersionService, IDisposable
{
    private readonly UpdateManager? _updateManager;
    private readonly IConfiguration _configuration;
    private UpdateInfo? _lastUpdateInfo;
    private bool _disposed;

    public SquirrelUpdateService(IConfiguration configuration)
    {
        _configuration = configuration;
        var updateUrl = configuration["Update:Url"] ?? "https://github.com/gaowatch/nekot/releases";
        try { _updateManager = new UpdateManager(updateUrl); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SquirrelUpdateService] Failed to create UpdateManager: {ex.Message}"); _updateManager = null; }
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        if (_updateManager == null) return new UpdateCheckResult { HasUpdate = false };
        try { _lastUpdateInfo = await _updateManager.CheckForUpdate(); return new UpdateCheckResult { HasUpdate = _lastUpdateInfo.ReleasesToApply.Count > 0, CurrentVersion = GetCurrentVersion(), LatestVersion = new VersionInfo { Version = _lastUpdateInfo.FutureReleaseEntry?.Version.ToString() ?? "Unknown", ReleaseDate = DateTime.Now }, IsForceUpdate = false }; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SquirrelUpdateService] 检查更新失败: {ex.Message}"); return new UpdateCheckResult { HasUpdate = false }; }
    }

    public string GetCurrentVersion() => _configuration["Application:Version"] ?? "1.0.0";

    public async Task<bool> ApplyUpdateAsync()
    {
        if (_updateManager == null) return false;
        try { if (_lastUpdateInfo == null || _lastUpdateInfo.ReleasesToApply.Count == 0) await CheckForUpdateAsync(); if (_lastUpdateInfo != null && _lastUpdateInfo.ReleasesToApply.Count > 0) { await _updateManager.UpdateApp(); return true; } return false; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SquirrelUpdateService] 应用更新失败: {ex.Message}"); return false; }
    }

    public bool IsUpdateAvailable() => _lastUpdateInfo != null && _lastUpdateInfo.ReleasesToApply.Count > 0;
    public void Dispose() { if (!_disposed) { _updateManager?.Dispose(); _disposed = true; } }
}