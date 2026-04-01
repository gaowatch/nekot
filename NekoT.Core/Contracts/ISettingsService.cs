using System;
using System.Threading.Tasks;

namespace NekoT.Core.Contracts;

public interface ISettingsService
{
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    Task<object> LoadAllAsync();
    Task SaveAllAsync(object settings);
    Task SaveBatchAsync(Action<object> updateAction);
}

public class SettingsChangedEventArgs : EventArgs
{
    public string? SettingName { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}