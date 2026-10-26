using LoomEditor.Core.Threading;

namespace LoomEditor.View.Features.Scheduling;

/// <summary>The way View code consumes the <see cref="Task"/> of a provider: the result comes back on the UI thread, unless it is stale.</summary>
/// <remarks>Threading: <see cref="Then{T}"/> can be called from any thread; the callbacks run on the dispatcher's thread.</remarks>
internal static class UiContinuation
{
    private sealed record State<T>(IUiDispatcher Dispatcher, Action<T> OnSuccess, Action<Exception>? OnError, CancellationToken Token);

    /// <summary>
    /// Runs <paramref name="onSuccess"/> through the dispatcher when the task completed with a result and the token is not cancelled;
    /// <paramref name="onError"/> when it faulted (never for cancellation); nothing at all when the token is cancelled or the task was cancelled.
    /// </summary>
    public static void Then<T>(Task<T> task, IUiDispatcher dispatcher, Action<T> onSuccess, Action<Exception>? onError = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(onSuccess);

        task.ContinueWith(
            static (t, state) => Complete(t, (State<T>)state!),
            new State<T>(dispatcher, onSuccess, onError, token),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void Complete<T>(Task<T> task, State<T> state)
    {
        if (task.IsCanceled || state.Token.IsCancellationRequested)
        {
            return;
        }

        // The token can be cancelled between the completion and the post: it is checked again in the posted action.
        if (task.IsFaulted)
        {
            AggregateException error = task.Exception!;
            if (state.OnError is null || error.InnerException is OperationCanceledException)
            {
                return;
            }

            state.Dispatcher.Post(() =>
            {
                if (!state.Token.IsCancellationRequested)
                {
                    state.OnError(error);
                }
            });
            return;
        }

        T result = task.Result;
        state.Dispatcher.Post(() =>
        {
            if (!state.Token.IsCancellationRequested)
            {
                state.OnSuccess(result);
            }
        });
    }
}
