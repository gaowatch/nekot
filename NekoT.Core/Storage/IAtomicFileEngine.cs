namespace NekoT.Core.Storage;

public interface IAtomicFileEngine
{
    Task WriteAtomicallyAsync(string path, string content, CancellationToken cancellationToken = default);
    Task WriteAtomicallyAsync(string path, byte[] content, CancellationToken cancellationToken = default);
    Task<bool> BackupAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> RestoreBackupAsync(string path, CancellationToken cancellationToken = default);
    bool VerifyChecksum(string path, string expectedChecksum);
    string ComputeChecksum(string path);
    string ComputeChecksum(byte[] data);
}