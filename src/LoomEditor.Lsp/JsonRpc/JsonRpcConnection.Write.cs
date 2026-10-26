using System.Text.Json;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.JsonRpc;

internal sealed partial class JsonRpcConnection
{
    private void Enqueue(JsonRpcMessage message, bool throwIfClosed)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message, LspJsonContext.Default.JsonRpcMessage);
        if (!_queue.Writer.TryWrite(bytes) && throwIfClosed)
        {
            throw ClosedException();
        }
    }

    private void SendResponse(JsonElement id, JsonElement? result)
        => Enqueue(new JsonRpcMessage { Id = id, Result = result ?? NullElement }, throwIfClosed: false);

    private void SendError(JsonElement id, int code, string message, JsonElement? data = null)
        => Enqueue(new JsonRpcMessage { Id = id, Error = new JsonRpcError { Code = code, Message = message, Data = data } }, throwIfClosed: false);

    private async Task WriteLoopAsync()
    {
        try
        {
            LspMessageWriter writer = new(output);
            await foreach (byte[] body in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await writer.WriteMessageAsync(body, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            if (!_disposing)
            {
                Close(e);
            }
        }
    }

    /// <summary>Ends the connection once: refuses new sends, faults the pending requests, raises <c>Closed</c>, completes <c>Completion</c>.</summary>
    private void Close(Exception? error)
    {
        Interlocked.Exchange(ref _closed, 1);
        _queue.Writer.TryComplete();
        if (Interlocked.Exchange(ref _closeRaised, 1) != 0)
        {
            return;
        }

        foreach (long id in _pending.Keys)
        {
            if (_pending.TryRemove(id, out TaskCompletionSource<JsonElement?>? tcs))
            {
                tcs.TrySetException(ClosedException(error));
            }
        }

        try
        {
            _lifetime.Cancel();
        }
        catch (Exception)
        {
            // A token callback threw: the connection is closing anyway.
        }

        try
        {
            Closed?.Invoke(error);
        }
        catch (Exception)
        {
            // A handler must not break the shutdown.
        }

        _completion.TrySetResult();
    }
}
