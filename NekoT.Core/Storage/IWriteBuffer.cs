namespace NekoT.Core.Storage;

public interface IWriteBuffer<T>
{
    int Count { get; }
    bool IsDirty { get; }
    void Add(T item);
    void AddRange(IEnumerable<T> items);
    void Clear();
    IEnumerable<T> GetAll();
    Task FlushAsync(CancellationToken cancellationToken = default);
    void MarkDirty();
    void ClearDirty();
}