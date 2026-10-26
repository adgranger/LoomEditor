using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Client;

/// <summary>A language server client: handshake, capabilities, server requests, shutdown and crash detection.</summary>
/// <remarks>Threading: thread-safe; events are raised on the read loop thread (the session posts them to the UI thread); ADR 0011 decision 4.</remarks>
internal sealed partial class LspClient : IAsyncDisposable
{
    private static readonly TimeSpan DisposeBudget = TimeSpan.FromSeconds(3);

    private readonly JsonRpcConnection _connection;
    private readonly LspClientOptions _options;
    private readonly object _gate = new();
    private int _state = (int)LspClientState.NotStarted;
    private int _disposed;
    private Exception? _faultReason;
    private ServerCapabilities? _capabilities;

    /// <summary>Wraps a connection that the caller created and started.</summary>
    public LspClient(JsonRpcConnection connection, LspClientOptions options, LspServerProcess? process = null)
    {
        _connection = connection;
        _options = options;
        Process = process;
        connection.RequestHandler = HandleServerRequestAsync;
        connection.NotificationReceived += OnNotification;
        connection.Closed += OnConnectionClosed;
        if (process is not null)
        {
            process.Exited += OnProcessExited;
            process.ErrorLine += line => ServerErrorLine?.Invoke(line);
        }
    }

    /// <summary>Raised on the read loop thread when the state changes.</summary>
    public event Action<LspClientState>? StateChanged;

    /// <summary>Raised on the read loop thread for <c>textDocument/publishDiagnostics</c>.</summary>
    public event Action<PublishDiagnosticsParams>? DiagnosticsReceived;

    /// <summary>Raised on the read loop thread for <c>window/logMessage</c> and <c>window/showMessage</c>.</summary>
    public event EventHandler<LspLogMessageEventArgs>? LogMessageReceived;

    /// <summary>Raised for each line the server process writes to standard error.</summary>
    public event Action<string>? ServerErrorLine;

    public LspServerProcess? Process { get; }

    public LspClientState State => (LspClientState)Volatile.Read(ref _state);

    public Exception? FaultReason
    {
        get
        {
            lock (_gate)
            {
                return _faultReason;
            }
        }
    }

    public ServerCapabilities? Capabilities => Volatile.Read(ref _capabilities);

    public bool SupportsCompletion => Capabilities?.CompletionProvider is not null;

    public IReadOnlyList<string> CompletionTriggerCharacters => Capabilities?.CompletionProvider?.TriggerCharacters ?? [];

    public bool SupportsCompletionResolve => Capabilities?.CompletionProvider?.ResolveProvider == true;

    public bool SupportsHover => LspUnions.IsEnabled(Capabilities?.HoverProvider);

    public bool SupportsDefinition => LspUnions.IsEnabled(Capabilities?.DefinitionProvider);

    public (bool OpenClose, int Change) TextDocumentSync => LspUnions.ParseTextDocumentSync(Capabilities?.TextDocumentSync);

    /// <summary>Starts the server process and a connection over its streams.</summary>
    public static LspClient StartProcess(LspServerCommand command, LspClientOptions options)
    {
        LspServerProcess process = LspServerProcess.Start(command);
        JsonRpcConnection connection = new(process.StandardOutput, process.StandardInput);
        connection.Start();
        return new LspClient(connection, options, process);
    }

    /// <summary>The <c>initialize</c> / <c>initialized</c> handshake; any failure faults the client, kills the process and is rethrown.</summary>
    public async Task<InitializeResult> InitializeAsync(CancellationToken token = default)
    {
        if (!TryTransition(LspClientState.NotStarted, LspClientState.Initializing))
        {
            throw new InvalidOperationException("The client was already initialized");
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(_options.InitializeTimeout);
        try
        {
            if (_connection.Completion.IsCompleted || Process is { HasExited: true })
            {
                throw new JsonRpcConnectionClosedException("The language server is not running");
            }

            InitializeParams request = new()
            {
                ProcessId = Environment.ProcessId,
                ClientInfo = new ClientInfoDto { Name = _options.ClientName, Version = _options.ClientVersion },
                RootUri = _options.RootUri,
                Capabilities = LspClientCapabilities.Create(),
                InitializationOptions = _options.InitializationOptions,
            };
            InitializeResult result = await _connection.SendRequestAsync(
                LspMethods.Initialize, request, LspJsonContext.Default.InitializeParams, LspJsonContext.Default.InitializeResult, timeout.Token)
                .ConfigureAwait(false) ?? throw new LspClientException("The language server answered initialize with null");
            Volatile.Write(ref _capabilities, result.Capabilities);
            _connection.SendNotification(LspMethods.Initialized, new InitializedParams(), LspJsonContext.Default.InitializedParams);
            if (!TryTransition(LspClientState.Initializing, LspClientState.Running))
            {
                throw FaultReason ?? new LspClientException("The language server stopped during the handshake");
            }

            return result;
        }
        catch (Exception e)
        {
            Exception failure = await DescribeInitializeFailureAsync(e, token).ConfigureAwait(false);
            Fault(failure);
            if (ReferenceEquals(failure, e))
            {
                throw;
            }

            throw failure;
        }
    }

    public Task<TResult?> RequestAsync<TParams, TResult>(
        string method, TParams @params, JsonTypeInfo<TParams> paramsInfo, JsonTypeInfo<TResult> resultInfo, CancellationToken token = default)
    {
        ThrowIfNotRunning();
        return _connection.SendRequestAsync(method, @params, paramsInfo, resultInfo, token);
    }

    public Task<JsonElement?> RequestRawAsync(string method, JsonElement? @params, CancellationToken token = default)
    {
        ThrowIfNotRunning();
        return _connection.SendRequestAsync(method, @params, token);
    }

    /// <summary>Queues a notification; never blocks. A connection that closed meanwhile is reported by <see cref="State"/>.</summary>
    public void Notify<TParams>(string method, TParams @params, JsonTypeInfo<TParams> paramsInfo)
    {
        ThrowIfNotRunning();
        try
        {
            _connection.SendNotification(method, @params, paramsInfo);
        }
        catch (JsonRpcConnectionClosedException)
        {
            // The fault is reported by the state.
        }
    }

    /// <summary>Polite shutdown (<c>shutdown</c>, <c>exit</c>), then a kill if the server does not obey. Ends in <see cref="LspClientState.Stopped"/>.</summary>
    public async Task ShutdownAsync(CancellationToken token = default)
    {
        if (!TryTransition(LspClientState.Running, LspClientState.ShuttingDown))
        {
            return;
        }

        try
        {
            using (CancellationTokenSource step = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                step.CancelAfter(_options.ShutdownTimeout);
                try
                {
                    await _connection.SendRequestAsync(LspMethods.Shutdown, null, step.Token).ConfigureAwait(false);
                }
                catch (Exception e) when (!token.IsCancellationRequested && e is not OutOfMemoryException)
                {
                    // A silent, failing or gone server: go on to exit and kill.
                }
            }

            try
            {
                _connection.SendNotification(LspMethods.Exit);
            }
            catch (JsonRpcConnectionClosedException)
            {
                // Already gone.
            }

            if (Process is { } process)
            {
                using CancellationTokenSource step = CancellationTokenSource.CreateLinkedTokenSource(token);
                step.CancelAfter(_options.ShutdownTimeout);
                try
                {
                    await process.WaitForExitAsync(step.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    // Did not exit in time: killed below.
                }
            }
        }
        finally
        {
            Process?.Kill();
            SetState(LspClientState.Stopped, onlyFrom: LspClientState.ShuttingDown);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (State == LspClientState.Running)
        {
            using CancellationTokenSource budget = new(DisposeBudget);
            try
            {
                await ShutdownAsync(budget.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Whatever happens, the cleanup below runs.
            }
        }

        if (State != LspClientState.Faulted)
        {
            SetState(LspClientState.Stopped);
        }

        await _connection.DisposeAsync().ConfigureAwait(false);
        if (Process is { } process)
        {
            await process.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void ThrowIfNotRunning()
    {
        if (State != LspClientState.Running)
        {
            throw new LspClientException("The language server is not running");
        }
    }

    private bool TryTransition(LspClientState from, LspClientState to)
    {
        if (Interlocked.CompareExchange(ref _state, (int)to, (int)from) != (int)from)
        {
            return false;
        }

        RaiseStateChanged(to);
        return true;
    }

    private void SetState(LspClientState to, LspClientState? onlyFrom = null)
    {
        lock (_gate)
        {
            if (State == to || (onlyFrom is { } from && State != from))
            {
                return;
            }

            Volatile.Write(ref _state, (int)to);
        }

        RaiseStateChanged(to);
    }

    private void RaiseStateChanged(LspClientState state)
    {
        try
        {
            StateChanged?.Invoke(state);
        }
        catch (Exception)
        {
            // A handler must not break the lifecycle.
        }
    }

    /// <summary>Initializing or Running → Faulted, once; kills the process.</summary>
    private void Fault(Exception reason)
    {
        lock (_gate)
        {
            if (State is not (LspClientState.Initializing or LspClientState.Running))
            {
                return;
            }

            _faultReason = reason;
            Volatile.Write(ref _state, (int)LspClientState.Faulted);
        }

        Process?.Kill();
        RaiseStateChanged(LspClientState.Faulted);
    }

    private void OnConnectionClosed(Exception? error)
        => Fault(new LspClientException(Describe("The language server closed the connection"), error));

    private void OnProcessExited(int exitCode)
        => Fault(new LspClientException(Describe($"The language server closed the connection (the process exited with code {exitCode})")));

    private string Describe(string message)
    {
        if (Process is not { RecentErrorLines: { Count: > 0 } lines })
        {
            return message;
        }

        return message + " Last server output: " + string.Join(" | ", lines.TakeLast(10));
    }

    private async Task<Exception> DescribeInitializeFailureAsync(Exception e, CancellationToken token)
    {
        if (e is OperationCanceledException && !token.IsCancellationRequested)
        {
            return new TimeoutException(Describe($"The language server did not answer initialize in {_options.InitializeTimeout}"));
        }

        if (e is JsonRpcConnectionClosedException)
        {
            if (Process is { } process)
            {
                process.Kill();
                try
                {
                    await process.ErrorLoopCompleted.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The stderr lines are a bonus.
                }
            }

            return new LspClientException(Describe("The language server closed the connection during initialize"), e);
        }

        return e;
    }

    private void OnNotification(string method, JsonElement? @params)
    {
        switch (method)
        {
            case LspMethods.PublishDiagnostics:
                if (Decode(@params, LspJsonContext.Default.PublishDiagnosticsParams) is { } diagnostics)
                {
                    DiagnosticsReceived?.Invoke(diagnostics);
                }

                break;
            case LspMethods.LogMessage:
                if (Decode(@params, LspJsonContext.Default.LogMessageParams) is { } log)
                {
                    LogMessageReceived?.Invoke(this, new LspLogMessageEventArgs(log.Type, log.Message, isShowMessage: false));
                }

                break;
            case LspMethods.ShowMessage:
                if (Decode(@params, LspJsonContext.Default.ShowMessageParams) is { } shown)
                {
                    LogMessageReceived?.Invoke(this, new LspLogMessageEventArgs(shown.Type, shown.Message, isShowMessage: true));
                }

                break;
            default:
                // $/progress, telemetry and the rest are ignored.
                break;
        }
    }

    private static T? Decode<T>(JsonElement? @params, JsonTypeInfo<T> info)
        where T : class
    {
        try
        {
            return @params?.Deserialize(info);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
