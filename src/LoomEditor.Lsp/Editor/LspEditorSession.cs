using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Lsp.Client;

namespace LoomEditor.Lsp.Editor;

/// <summary>Connects one editor document to a language server: starts it, opens the document, registers the completion, hover and definition
/// providers the server supports and shows its diagnostics as markers. <see cref="StopAsync"/> undoes all of it.</summary>
/// <remarks>Threading: see ADR 0011 decision 4. UI thread only for the public members; the events are raised on the UI thread.</remarks>
public sealed partial class LspEditorSession : IAsyncDisposable
{
    private readonly TextModel _model;
    private readonly LanguageFeaturesService _features;
    private readonly MarkerService _markers;
    private readonly string _documentUri;
    private readonly string _languageId;
    private readonly Func<LspClient> _clientFactory;
    private readonly Func<Action, bool> _post;
    private readonly LspSessionOptions _options;
    private readonly List<IDisposable> _registrations = [];
    private int _state = (int)LspSessionState.NotStarted;
    private volatile string? _faultMessage;
    private LspClient? _client;
    private LspDocumentSync? _sync;
    private LspDiagnosticsBinder? _binder;
    private CancellationTokenSource? _startCts;
    private Task? _stopTask;

    /// <summary>Creates a session that starts the server described by <paramref name="command"/>.</summary>
    /// <param name="model">The text model of the document.</param>
    /// <param name="features">The editor's language features service.</param>
    /// <param name="markers">The editor's marker service.</param>
    /// <param name="documentUri">The URI of the document sent to the server.</param>
    /// <param name="languageId">The language id of the document, also the selector of the providers.</param>
    /// <param name="command">How to start the server.</param>
    /// <param name="post">Runs an action on the UI thread; returns <c>false</c> when the UI is gone.</param>
    /// <param name="options">Optional settings.</param>
    public LspEditorSession(
        TextModel model,
        LanguageFeaturesService features,
        MarkerService markers,
        string documentUri,
        string languageId,
        LspServerCommand command,
        Func<Action, bool> post,
        LspSessionOptions? options = null)
        : this(model, features, markers, documentUri, languageId, CreateProcessFactory(command, options ?? new LspSessionOptions()), post, options)
    {
    }

    internal LspEditorSession(
        TextModel model,
        LanguageFeaturesService features,
        MarkerService markers,
        string documentUri,
        string languageId,
        Func<LspClient> clientFactory,
        Func<Action, bool> post,
        LspSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(markers);
        ArgumentNullException.ThrowIfNull(documentUri);
        ArgumentNullException.ThrowIfNull(languageId);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(post);
        _model = model;
        _features = features;
        _markers = markers;
        _documentUri = documentUri;
        _languageId = languageId;
        _clientFactory = clientFactory;
        _post = post;
        _options = options ?? new LspSessionOptions();
    }

    /// <summary>Raised on the UI thread when <see cref="State"/> changes.</summary>
    public event Action<LspSessionState>? StateChanged;

    /// <summary>Raised on the UI thread for each log message, show message and standard error line of the server, as a text line for the host's log.</summary>
    public event Action<string>? ServerOutput;

    /// <summary>The current state.</summary>
    public LspSessionState State => (LspSessionState)Volatile.Read(ref _state);

    /// <summary>Why the session is <see cref="LspSessionState.Faulted"/>; <c>null</c> otherwise.</summary>
    public string? FaultMessage => _faultMessage;

    /// <summary>Starts the server and connects the editor. The task completes once the document is open and the providers are registered.</summary>
    /// <param name="token">Cancels the start.</param>
    /// <exception cref="InvalidOperationException">The session was already started.</exception>
    public async Task StartAsync(CancellationToken token = default)
    {
        if (Interlocked.CompareExchange(ref _state, (int)LspSessionState.Starting, (int)LspSessionState.NotStarted) != (int)LspSessionState.NotStarted)
        {
            throw new InvalidOperationException("The session was already started");
        }

        StateChanged?.Invoke(LspSessionState.Starting);
        _startCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        LspClient client;
        try
        {
            client = _clientFactory();
        }
        catch (LspServerStartException e)
        {
            Fault(e);
            throw;
        }

        _client = client;
        client.LogMessageReceived += OnLogMessage;
        client.ServerErrorLine += line => PostOutput("[stderr] " + line);
        client.StateChanged += state =>
        {
            if (state == LspClientState.Faulted)
            {
                _post(() => Fault(client.FaultReason));
            }
        };

        try
        {
            await client.InitializeAsync(_startCts.Token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _post(() => Fault(e));
            throw;
        }

        TaskCompletionSource done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_post(() => FinishStart(client, done)))
        {
            await done.Task.ConfigureAwait(false);
            return;
        }

        // The UI is gone: nothing to connect, just shut the server down.
        Volatile.Write(ref _state, (int)LspSessionState.Stopped);
        await ShutDownAsync(client).ConfigureAwait(false);
    }

    /// <summary>Stops the server and removes everything the session added to the editor. Idempotent; concurrent calls share one task.</summary>
    public Task StopAsync()
    {
        if (_stopTask is not null)
        {
            return _stopTask;
        }

        if (State is LspSessionState.NotStarted or LspSessionState.Stopped)
        {
            return Task.CompletedTask;
        }

        return _stopTask = StopCoreAsync();
    }

    /// <summary>Same as <see cref="StopAsync"/>.</summary>
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private static Func<LspClient> CreateProcessFactory(LspServerCommand command, LspSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(command);
        LspClientOptions clientOptions = new()
        {
            RootUri = options.RootUri,
            ClientVersion = options.ClientVersion,
            InitializeTimeout = options.InitializeTimeout,
        };
        return () => LspClient.StartProcess(command, clientOptions);
    }

    private void SetState(LspSessionState state)
    {
        Volatile.Write(ref _state, (int)state);
        StateChanged?.Invoke(state);
    }

    private void OnLogMessage(object? sender, LspLogMessageEventArgs e)
    {
        string type = e.Type switch { 1 => "error", 2 => "warning", 3 => "info", _ => "log" };
        PostOutput((e.IsShowMessage ? "(notification) " : "") + $"[{type}] {e.Message}");
    }

    private void PostOutput(string line) => _post(() => ServerOutput?.Invoke(line));
}
