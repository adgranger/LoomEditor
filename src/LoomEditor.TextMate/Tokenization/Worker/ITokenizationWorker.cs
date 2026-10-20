namespace LoomEditor.TextMate.Tokenization.Worker;

/// <summary>
/// What the UI side needs from the tokenization worker: a place to post messages. <see cref="TokenizationWorker"/> is
/// the real one; tests use a stand-in that runs the messages synchronously.
/// </summary>
/// <remarks>Threading: <see cref="Post"/> can be called from any thread; <see cref="IDisposable.Dispose"/> stops the worker.</remarks>
internal interface ITokenizationWorker : IDisposable
{
    /// <summary>Queues a message; messages are handled in order.</summary>
    void Post(WorkerMessage message);
}
