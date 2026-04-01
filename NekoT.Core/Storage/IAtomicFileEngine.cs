using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public interface IAtomicFileEngine { Task<bool> WriteAsync<T>(T data, CancellationToken ct = default); Task<T?> ReadAsync<T>(CancellationToken ct = default); Task<bool> ExistsAsync(); Task<bool> BackupAsync(); }