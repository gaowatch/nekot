namespace NekoT.Core.Storage;

public interface IPersistenceService
{
    bool IsDirty { get; }
    DateTime LastSaveTime { get; }
    DateTime CurrentDate { get; }
    void MarkDirty();
    void ClearDirty();
    Task SaveIfDirtyAsync(CancellationToken cancellationToken = default);
    Task LoadAsync(CancellationToken cancellationToken = default);
    bool HasDateChanged();
    void OnDateChanged(Action onDateChanged);
}