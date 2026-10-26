using System.Collections.Concurrent;
using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.FakeServer;

/// <summary>A deterministic language server that speaks the real protocol over two streams (see README.md).</summary>
/// <remarks>
/// Threading: <see cref="RunAsync"/> runs one read loop; messages are handled sequentially in arrival order except delayed
/// requests, which answer from the thread pool. Writes are serialized by a lock. The public getters are thread-safe.
/// </remarks>
public sealed partial class FakeLanguageServer(Stream input, Stream output, FakeServerOptions options, bool exitProcess = false) : IDisposable
{
    private static readonly JsonElement NullElement = JsonElement.Parse("null");

    private readonly LspMessageWriter _writer = new(output);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly object _gate = new();
    private readonly List<string> _received = [];
    private readonly Dictionary<string, JsonElement?> _responses = [];
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _delayed = new();
    private bool _initializeSeen;
    private bool _shutdown;
    private JsonElement? _initializeParams;

    /// <summary>Releases the write lock; call once <see cref="RunAsync"/> has ended.</summary>
    public void Dispose() => _writeLock.Dispose();

    /// <summary>The <c>params</c> of the <c>initialize</c> request (cloned); <c>null</c> before it arrives.</summary>
    public JsonElement? LastInitializeParams
    {
        get
        {
            lock (_gate)
            {
                return _initializeParams;
            }
        }
    }

    /// <summary>The text of the open document (empty when none).</summary>
    public string DocumentText
    {
        get
        {
            lock (_gate)
            {
                return _text;
            }
        }
    }

    /// <summary>The version of the open document.</summary>
    public int DocumentVersion
    {
        get
        {
            lock (_gate)
            {
                return _version;
            }
        }
    }

    /// <summary>The uri of the open document, <c>null</c> when none.</summary>
    public string? DocumentUri
    {
        get
        {
            lock (_gate)
            {
                return _uri;
            }
        }
    }

    /// <summary>Every method name received, in order (a snapshot copy).</summary>
    public IReadOnlyList<string> ReceivedMethods
    {
        get
        {
            lock (_gate)
            {
                return [.. _received];
            }
        }
    }

    /// <summary>The <c>result</c> the client sent for a server request id; <c>null</c> when none or when it was an error.</summary>
    public JsonElement? LastResponseResult(string id)
    {
        lock (_gate)
        {
            return _responses.GetValueOrDefault(id);
        }
    }

    /// <summary>Serves the connection until it ends; returns the exit code (0 after shutdown, 1 otherwise, 3 for <c>ExitOn</c>).</summary>
    public async Task<int> RunAsync(CancellationToken token)
    {
        if (options.Stderr is { } stderr)
        {
            await Console.Error.WriteLineAsync(stderr.AsMemory(), token).ConfigureAwait(false);
        }

        if (options.EchoEnv is { } name)
        {
            await Console.Error.WriteLineAsync($"{name}={Environment.GetEnvironmentVariable(name)}".AsMemory(), token).ConfigureAwait(false);
        }

        if (options.EchoCwd)
        {
            await Console.Error.WriteLineAsync($"cwd={Environment.CurrentDirectory}".AsMemory(), token).ConfigureAwait(false);
        }

        try
        {
            LspMessageReader reader = new(input);
            while (await reader.ReadMessageAsync(token).ConfigureAwait(false) is { } body)
            {
                JsonRpcMessage? message;
                try
                {
                    message = JsonSerializer.Deserialize(body, LspJsonContext.Default.JsonRpcMessage);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (message is not null && await HandleAsync(message).ConfigureAwait(false) is { } code)
                {
                    return code;
                }
            }
        }
        catch (Exception e) when (e is IOException or LspProtocolException or ObjectDisposedException or OperationCanceledException)
        {
            // The peer went away or the run was cancelled: end like a closed input.
        }

        return _shutdown ? 0 : 1;
    }

    private async Task<int?> HandleAsync(JsonRpcMessage message)
    {
        if (message.Method is not { } method)
        {
            RecordResponse(message);
            return null;
        }

        Record(method);
        if (method == options.ExitOn)
        {
            if (exitProcess)
            {
                Environment.Exit(3);
            }

            return 3;
        }

        if (method == LspMethods.CancelRequest)
        {
            await CancelAsync(message.Params).ConfigureAwait(false);
        }
        else if (message.Id is { } id)
        {
            await HandleRequestAsync(id, method, message.Params).ConfigureAwait(false);
        }
        else if (method == LspMethods.Exit)
        {
            return _shutdown ? 0 : 1;
        }
        else if (_initializeSeen)
        {
            await HandleNotificationAsync(method, message.Params).ConfigureAwait(false);
        }

        return null;
    }

    private async Task HandleNotificationAsync(string method, JsonElement? @params)
    {
        switch (method)
        {
            case LspMethods.Initialized:
                if (options.Chatty)
                {
                    await SendChattyAsync().ConfigureAwait(false);
                }

                break;
            case LspMethods.DidOpen:
                await DidOpenAsync(@params).ConfigureAwait(false);
                break;
            case LspMethods.DidChange:
                await DidChangeAsync(@params).ConfigureAwait(false);
                break;
            case LspMethods.DidClose:
                await DidCloseAsync(@params).ConfigureAwait(false);
                break;
            default:
                // An unknown notification is ignored.
                break;
        }
    }

    private async Task HandleRequestAsync(JsonElement id, string method, JsonElement? @params)
    {
        Func<JsonElement?>? feature = null;
        JsonElement? result = null;
        try
        {
            if (method == LspMethods.Initialize)
            {
                lock (_gate)
                {
                    _initializeParams = @params?.Clone();
                }

                result = Initialize();
            }
            else if (!_initializeSeen)
            {
                throw new JsonRpcException(JsonRpcErrorCodes.ServerNotInitialized, "Server not initialized");
            }
            else if (method == LspMethods.Shutdown)
            {
                _shutdown = true;
            }
            else if (_shutdown)
            {
                throw new JsonRpcException(JsonRpcErrorCodes.InvalidRequest, "Server is shutting down");
            }
            else
            {
                feature = FindFeature(method, @params);
            }
        }
        catch (JsonRpcException e)
        {
            await SendErrorAsync(id, e.Code, e.Message).ConfigureAwait(false);
            return;
        }

        if (feature is null)
        {
            await SendResponseAsync(id, result).ConfigureAwait(false);
        }
        else if (options.DelayMs > 0)
        {
            StartDelayed(id, feature);
        }
        else
        {
            await RespondAsync(id, feature).ConfigureAwait(false);
        }
    }

    private JsonElement? Initialize()
    {
        if (_initializeSeen)
        {
            throw new JsonRpcException(JsonRpcErrorCodes.InvalidRequest, "Server already initialized");
        }

        _initializeSeen = true;
        int change = options.Sync switch
        {
            "none" => LspTextDocumentSyncKind.None,
            "full" => LspTextDocumentSyncKind.Full,
            _ => LspTextDocumentSyncKind.Incremental,
        };
        InitializeResult result = new()
        {
            Capabilities = new ServerCapabilities
            {
                PositionEncoding = LspPositionEncoding.Utf16,
                TextDocumentSync = JsonElement.Parse($"{{\"openClose\":true,\"change\":{change}}}"),
                CompletionProvider = new CompletionOptions { TriggerCharacters = ["."], ResolveProvider = true },
                HoverProvider = JsonElement.Parse("true"),
                DefinitionProvider = JsonElement.Parse("true"),
            },
            ServerInfo = new ServerInfoDto { Name = "LoomEditor.FakeServer", Version = "1.0" },
        };
        return JsonSerializer.SerializeToElement(result, LspJsonContext.Default.InitializeResult);
    }

    private void StartDelayed(JsonElement id, Func<JsonElement?> feature)
    {
        string key = id.GetRawText();
        CancellationTokenSource cts = new();
        _delayed[key] = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(options.DelayMs, cts.Token).ConfigureAwait(false);
                // A cancel that removed the entry has already answered.
                if (_delayed.TryRemove(key, out _))
                {
                    await RespondAsync(id, feature).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException)
            {
                // Cancelled (answered by the cancel) or the peer went away.
            }
            finally
            {
                cts.Dispose();
            }
        });
    }

    private async Task CancelAsync(JsonElement? @params)
    {
        if (@params is { ValueKind: JsonValueKind.Object } p
            && p.TryGetProperty("id", out JsonElement id)
            && _delayed.TryRemove(id.GetRawText(), out CancellationTokenSource? cts))
        {
            await SendErrorAsync(id, JsonRpcErrorCodes.RequestCancelled, "Request cancelled").ConfigureAwait(false);
            try
            {
                await cts.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // The delayed task just finished.
            }
        }
    }

    private async Task RespondAsync(JsonElement id, Func<JsonElement?> feature)
    {
        try
        {
            await SendResponseAsync(id, feature()).ConfigureAwait(false);
        }
        catch (JsonRpcException e)
        {
            await SendErrorAsync(id, e.Code, e.Message).ConfigureAwait(false);
        }
    }

    private void Record(string entry)
    {
        lock (_gate)
        {
            _received.Add(entry);
        }
    }

    private void RecordResponse(JsonRpcMessage message)
    {
        if (message.Id is not { } id)
        {
            return;
        }

        string key = id.ValueKind == JsonValueKind.String ? id.GetString()! : id.GetRawText();
        lock (_gate)
        {
            _received.Add($"response({key})");
            _responses[key] = message.Error is null ? message.Result : null;
        }
    }

    private Task SendResponseAsync(JsonElement id, JsonElement? result)
        => WriteAsync(new JsonRpcMessage { Id = id, Result = result ?? NullElement });

    private Task SendErrorAsync(JsonElement id, int code, string text)
        => WriteAsync(new JsonRpcMessage { Id = id, Error = new JsonRpcError { Code = code, Message = text } });

    private Task SendNotificationAsync(string method, JsonElement @params)
        => WriteAsync(new JsonRpcMessage { Method = method, Params = @params });

    private Task SendRequestAsync(string id, string method, string paramsJson)
        => WriteAsync(new JsonRpcMessage { Id = JsonElement.Parse($"\"{id}\""), Method = method, Params = JsonElement.Parse(paramsJson) });

    private async Task WriteAsync(JsonRpcMessage message)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, LspJsonContext.Default.JsonRpcMessage);
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await _writer.WriteMessageAsync(body, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task SendChattyAsync()
    {
        await SendNotificationAsync(LspMethods.LogMessage, JsonElement.Parse("""{"type":3,"message":"fake server ready"}""")).ConfigureAwait(false);
        await SendRequestAsync("cfg-1", LspMethods.WorkspaceConfiguration, """{"items":[{"section":"fake"}]}""").ConfigureAwait(false);
        await SendRequestAsync("reg-1", LspMethods.RegisterCapability, """{"registrations":[]}""").ConfigureAwait(false);
        await SendRequestAsync("wdp-1", LspMethods.WorkDoneProgressCreate, """{"token":"t1"}""").ConfigureAwait(false);
        await SendRequestAsync("edit-1", LspMethods.ApplyEdit, """{"edit":{"changes":{}}}""").ConfigureAwait(false);
        await SendNotificationAsync(LspMethods.ShowMessage, JsonElement.Parse("""{"type":2,"message":"fake warning"}""")).ConfigureAwait(false);
    }
}
