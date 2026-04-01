using System;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Desktop.Utilities;

public static class ThreadSafeHelper
{
    public static T ThreadSafeGet<T>(ref T field, Func<T> getter)
    {
        return Thread.GetData(Thread.GetNamedDataSlot(typeof(T).Name + "_slot")) is T value 
            ? value 
            : getter();
    }

    public static void ThreadSafeSet<T>(ref T field, T value)
    {
        Thread.SetData(Thread.GetNamedDataSlot(typeof(T).Name + "_slot"), value!);
        field = value;
    }

    public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<T>();
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
        var completedTask = await Task.WhenAny(task, tcs.Task);
        return await completedTask;
    }

    public static async Task<bool> WaitAsync(this Task task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
        var completedTask = await Task.WhenAny(task, tcs.Task);
        return completedTask == task;
    }
}