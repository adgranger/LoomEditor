using LoomEditor.Core.Model.Tokens;

namespace LoomEditor.View.Highlighting;

/// <summary>
/// A scheduler that runs nothing: the model only tokenizes the lines that are asked for. The background worker does
/// the rest of the document; this one is the fallback for models too large to sync and for editors without a dispatcher.
/// </summary>
/// <remarks>Threading: stateless, thread-safe.</remarks>
internal sealed class NoopTokenizationScheduler : ITokenizationScheduler
{
    /// <summary>The only instance.</summary>
    public static readonly NoopTokenizationScheduler Instance = new();

    /// <inheritdoc />
    public IDisposable RunWhenIdle(Action<IdleDeadline> callback) => new Handle();

    /// <inheritdoc />
    public void Post(Action callback)
    {
    }

    private sealed class Handle : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
