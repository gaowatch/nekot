using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NekoT.Core.Storage;

namespace NekoT.Core.Versioning;

public class SquirrelUpdateService : IVersionService, IDisposable
{
    private readonly string _appName = "NekoT";
    private readonly string _updateUrl;
    private bool _disposed;

    public event EventHandler<UpdateCheckResult>? UpdateAvailable;
    public event EventHandler<string>? UpdateStatusChanged;

    public SquirrelUpdateService(string updateUrl = "https://nekot-ai.github.io/releases/update")
    {
        _updateUrl = updateUrl;
    }

    public string GetCurrentVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        return await Task.FromResult(new UpdateCheckResult { HasUpdate = false });
    }

    public async Task<bool> ApplyUpdateAsync()
    {
        return await Task.FromResult(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}