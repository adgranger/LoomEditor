using System.Collections.Concurrent;
using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>A hand-written server for the tests that need a minimal or misbehaving peer, on a <see cref="MemoryPipe"/>.</summary>
/// <remarks>Threading: handlers run on the server's read loop thread; the recorded lists are safe to read from the test thread.</remarks>
internal sealed class ScriptedServer : IAsyncDisposable
{
    private readonly MemoryPipe _pipe = new();
    private readonly TestPeer _peer;
    private readonly ConcurrentDictionary<string, Func<JsonElement?, object?>> _handlers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonRpcMessage>> _responses = new();
    private readonly ConcurrentQueue<(string Method, JsonElement? Params)> _notifications = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly object _gate = new();
    private readonly List<string> _requestMethods = [];
    private string _capabilitiesJson = "{}";
    private Task _loop = Task.CompletedTask;
    private readonly LspMessageReader _reader;

    public ScriptedServer()
    {
        _peer = new TestPeer(_pipe.ServerInput, _pipe.ServerOutput);
        _reader = new LspMessageReader(_pipe.ServerInput);
        Connection = new JsonRpcConnection(_pipe.ClientInput, _pipe.ClientOutput);
    }

    /// <summary>The client side of the pipe; <see cref="Start"/> starts it too.</summary>
    public JsonRpcConnection Connection { get; }

    /// <summary>Every notification received, in order.</summary>
    public IReadOnlyList<(string Method, JsonElement? Params)> Notifications => [.. _notifications];

    /// <summary>The method of every request received, in order.</summary>
    public IReadOnlyList<string> RequestMethods
    {
        get
        {
            lock (_gate)
            {
                return [.. _requestMethods];
            }
        }
    }

    /// <summary>The capabilities <c>initialize</c> answers with (a JSON object).</summary>
    public ScriptedServer WithCapabilities(string capabilitiesJson)
    {
        _capabilitiesJson = capabilitiesJson;
        return this;
    }

    /// <summary>Answers requests of <paramref name="method"/>: a string is raw JSON, <c>null</c> is null, a <see cref="JsonElement"/> is itself; a thrown <see cref="JsonRpcException"/> becomes an error response.</summary>
    public ScriptedServer On(string method, Func<JsonElement?, object?> handler)
    {
        _handlers[method] = handler;
        return this;
    }

    /// <summary>Never answers requests of <paramref name="method"/>.</summary>
    public ScriptedServer Silent(string method) => On(method, _ => throw new SilentException());

    /// <summary>Starts the client connection and the server loop.</summary>
    public ScriptedServer Start()
    {
        Connection.Start();
        _loop = Task.Run(RunAsync);
        return this;
    }

    public Task SendNotificationAsync(string method, string? paramsJson = null) => WriteAsync(TestPeer.Notification(method, paramsJson));

    /// <summary>Sends a request to the client and returns its response message.</summary>
    public async Task<JsonRpcMessage> SendRequestAsync(string id, string method, string? paramsJson = null)
    {
        TaskCompletionSource<JsonRpcMessage> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _responses[id] = tcs;
        await WriteAsync(TestPeer.Request($"\"{id}\"", method, paramsJson));
        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        _pipe.Dispose();
        try
        {
            await _loop.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception)
        {
            // The loop ends with the pipe.
        }

        _writeLock.Dispose();
    }

    private async Task RunAsync()
    {
        try
        {
            while (true)
            {
                JsonRpcMessage message = await ReadNextAsync();
                if (message.Method is not { } method)
                {
                    if (message.Id is { } responseId && _responses.TryRemove(responseId.ValueKind == JsonValueKind.String ? responseId.GetString()! : responseId.GetRawText(), out TaskCompletionSource<JsonRpcMessage>? tcs))
                    {
                        tcs.TrySetResult(message);
                    }
                }
                else if (message.Id is { } id)
                {
                    await AnswerAsync(id, method, message.Params);
                }
                else
                {
                    _notifications.Enqueue((method, message.Params));
                }
            }
        }
        catch (Exception)
        {
            // The pipe was closed: the test is over.
        }
    }

    private async Task<JsonRpcMessage> ReadNextAsync()
    {
        byte[]? body = await _reader.ReadMessageAsync(CancellationToken.None) ?? throw new EndOfStreamException();
        return JsonSerializer.Deserialize(body, LoomEditor.Lsp.Protocol.LspJsonContext.Default.JsonRpcMessage)!;
    }

    private async Task AnswerAsync(JsonElement id, string method, JsonElement? @params)
    {
        lock (_gate)
        {
            _requestMethods.Add(method);
        }

        try
        {
            object? result = _handlers.TryGetValue(method, out Func<JsonElement?, object?>? handler)
                ? handler(@params)
                : method switch
                {
                    "initialize" => "{\"capabilities\":" + _capabilitiesJson + "}",
                    "shutdown" => null,
                    _ => throw new JsonRpcException(JsonRpcErrorCodes.MethodNotFound, $"Method not found: {method}"),
                };
            JsonElement? element = result switch
            {
                null => null,
                string json => JsonElement.Parse(json),
                JsonElement e => e,
                _ => throw new InvalidOperationException("A handler returns a JSON string, a JsonElement or null"),
            };
            await WriteAsync(new JsonRpcMessage { Id = id, Result = element ?? JsonElement.Parse("null") });
        }
        catch (SilentException)
        {
            // Never answered, on purpose.
        }
        catch (JsonRpcException e)
        {
            await WriteAsync(TestPeer.Error(id, e.Code, e.Message));
        }
    }

    private async Task WriteAsync(JsonRpcMessage message)
    {
        await _writeLock.WaitAsync();
        try
        {
            await _peer.WriteAsync(message);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private sealed class SilentException : Exception;

}
