using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using NekoT.Core.Contracts;
using NekoT.Desktop.Services;
using NekoT.Models.Versioning;

namespace NekoT.Desktop.Update;

public class UpdateCheckScheduler
{
    private readonly IVersionService _versionService;
    private readonly UserSettingsService _userSettings;
    private DispatcherTimer? _checkTimer;

    public event EventHandler<UpdateCheckResult>? UpdateAvailable;

    public UpdateCheckScheduler(IVersionService versionService)
    {
        _versionService = versionService;
        _userSettings = UserSettingsService.Instance;
    }

    public void StartPeriodicCheck()
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            await CheckForUpdateAsync();
        });

        _checkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(24)
        };
        _checkTimer.Tick += async (s, e) => await CheckForUpdateAsync();
        _checkTimer.Start();
    }

    public async Task CheckForUpdateAsync()
    {
        var lastCheck = _userSettings.LastUpdateCheckTime;
        if (lastCheck.HasValue && (DateTime.Now - lastCheck.Value).TotalHours < 24)
        {
            return;
        }

        var result = await _versionService.CheckForUpdateAsync();
        _userSettings.LastUpdateCheckTime = DateTime.Now;

        if (result.HasUpdate && !IsVersionSkipped(result.LatestVersion?.Version))
        {
            UpdateAvailable?.Invoke(this, result);
        }
    }

    private bool IsVersionSkipped(string? version)
    {
        if (string.IsNullOrEmpty(version))
            return false;

        return _userSettings.SkippedVersions.Contains(version);
    }

    public void SkipVersion(string version)
    {
        _userSettings.SkippedVersions.Add(version);
    }
}