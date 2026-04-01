using System.Threading.Tasks;
using NekoT.Models.Versioning;

namespace NekoT.Core.Contracts;

public interface IVersionService
{
    Task<UpdateCheckResult> CheckForUpdateAsync();
    string GetCurrentVersion();
    Task<bool> ApplyUpdateAsync();
    bool IsUpdateAvailable();
}