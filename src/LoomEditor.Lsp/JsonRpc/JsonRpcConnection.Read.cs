using System.Text.Json;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.JsonRpc;

internal sealed partial class JsonRpcConnection
{
    private async Task ReadLoopAsync()
    {
        Exception? error = null;
        try
        {
            LspMessageReader reader = new(input);
            while (await reader.ReadMessageAsync(_readCancellation.Token).ConfigureAwait(false) is { } body)
            {
                Dispatch(body);
            }
        }
        catch (Exception e)
        {
            error = _disposing ? null : e;
        }

        Close(error);
    }

    private void Dispatch(byte[] body)
    {
        JsonRpcMessage? message;
        try
        {
            message = JsonSerializer.Deserialize(body, LspJsonContext.Default.JsonRpcMessage);
        }
        catch (JsonException e)
        {
            RaiseProtocolError(e);
            return;
        }

        if (message is null)
        {
            RaiseProtocolError(new LspProtocolException("The message is not an object"));
            return;
        }

        try
        {
            if (message.Method is null)
            {
                CompleteRequest(message);
            }
            else if (message.Method == "$/cancelRequest")
            {
                CancelServerRequest(message.Params);
            }
            else if (message.Id is { } id)
            {
                StartServerRequest(id, message);
            }
            else
            {
                NotificationReceived?.Invoke(message.Method, message.Params);
            }
        }
        catch (Exception)
        {
            // A handler failure must not kill the read loop.
        }
    }

    private void RaiseProtocolError(Exception e)
    {
        try
        {
            ProtocolError?.Invoke(e);
        }
        catch (Exception)
        {
            // Same rule as for notification handlers.
        }
    }

    private void CompleteRequest(JsonRpcMessage response)
    {
        if (response.Id is not { ValueKind: JsonValueKind.Number } id
            || !id.TryGetInt64(out long key)
            || !_pending.TryRemove(key, out TaskCompletionSource<JsonElement?>? tcs))
        {
            return;
        }

        if (response.Error is { } error)
        {
            tcs.TrySetException(new JsonRpcException(error.Code, error.Message, error.Data));
        }
        else
        {
            tcs.TrySetResult(response.Result);
        }
    }

    private void CancelServerRequest(JsonElement? @params)
    {
        if (@params is { ValueKind: JsonValueKind.Object } p
            && p.TryGetProperty("id", out JsonElement id)
            && _serverRequests.TryGetValue(id.GetRawText(), out CancellationTokenSource? cts))
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The request just finished.
            }
        }
    }

    private void StartServerRequest(JsonElement id, JsonRpcMessage request)
    {
        // Registered before the handler runs so that a following $/cancelRequest always finds it.
        string key = id.GetRawText();
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _serverRequests[key] = cts;
        _ = Task.Run(() => HandleRequestAsync(id, key, request, cts));
    }

    private async Task HandleRequestAsync(JsonElement id, string key, JsonRpcMessage request, CancellationTokenSource cts)
    {
        try
        {
            if (RequestHandler is not { } handler)
            {
                SendError(id, JsonRpcErrorCodes.MethodNotFound, $"Method not found: {request.Method}");
                return;
            }

            SendResponse(id, await handler(request.Method!, request.Params, cts.Token).ConfigureAwait(false));
        }
        catch (JsonRpcException e)
        {
            SendError(id, e.Code, e.Message, e.ErrorData);
        }
        catch (Exception e)
        {
            SendError(id, JsonRpcErrorCodes.InternalError, e.Message);
        }
        finally
        {
            _serverRequests.TryRemove(key, out _);
            cts.Dispose();
        }
    }
}
