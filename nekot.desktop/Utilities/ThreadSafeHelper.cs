using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace NekoT.Desktop.Utilities;

public static class ThreadSafeHelper
{
    public static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    public static Task RunOnUiThreadAsync(Func<Task> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return func();
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }

    public static void ObserveExceptions(this Task task, Action<Exception>? onError = null)
    {
        task.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                var exception = t.Exception.Flatten();
                System.Diagnostics.Debug.WriteLine($"[ThreadSafeHelper] Observed exception: {exception}");
                onError?.Invoke(exception);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public static void SafeFireAndForget(this Task task, Action<Exception>? onError = null)
    {
        task.ObserveExceptions(onError);
    }
}