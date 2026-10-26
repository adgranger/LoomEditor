using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>An <see cref="LspEditorSession"/> on an in-process fake or scripted server, with a <see cref="ManualPost"/> as the UI thread.</summary>
/// <remarks>Threading: the test thread is the UI thread; <see cref="PumpAsync"/> runs the posted actions on it.</remarks>
internal sealed class SessionRig : IAsyncDisposable
{
    public const string Uri = "file:///t.txt";
    public const string Language = "csharp";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly ScriptedServer? _scripted;

    private SessionRig(string text, Func<Action, bool>? post, FakeServerHost? host, ScriptedServer? scripted, LspClientOptions? clientOptions, LspSessionOptions? options)
    {
        Host = host;
        _scripted = scripted;
        Model = new TextModel(text, DefaultEndOfLine.LF);
        Func<LspClient> factory = host is not null
            ? host.CreateSessionClientFactory(clientOptions)
            : () => new LspClient(scripted!.Connection, clientOptions ?? new LspClientOptions());
        Session = new LspEditorSession(Model, Features, Markers, Uri, Language, factory, post ?? Post.Post, options);
        Session.StateChanged += state =>
        {
            States.Add(state);
            StateThreads.Add(Environment.CurrentManagedThreadId);
        };
        Session.ServerOutput += Output.Add;
    }

    public FakeServerHost? Host { get; }

    public TextModel Model { get; }

    public LanguageFeaturesService Features { get; } = new();

    public MarkerService Markers { get; } = new();

    public ManualPost Post { get; } = new();

    public LspEditorSession Session { get; }

    public List<LspSessionState> States { get; } = [];

    public List<int> StateThreads { get; } = [];

    public HashSet<int> PumpThreads { get; } = [];

    public List<string> Output { get; } = [];

    public static SessionRig OnFake(string text = "hello", FakeServerOptions? options = null, LspSessionOptions? sessionOptions = null)
        => new(text, null, FakeServerHost.StartInProcess(options), null, null, sessionOptions);

    public static SessionRig OnScripted(ScriptedServer server, string text = "hello", LspClientOptions? clientOptions = null)
    {
        server.Start();
        return new SessionRig(text, null, null, server, clientOptions, null);
    }

    /// <summary>Runs the posted actions on this thread until <paramref name="task"/> completes, then rethrows its failure.</summary>
    public async Task PumpAsync(Task task)
    {
        DateTime end = DateTime.UtcNow + Timeout;
        while (!task.IsCompleted)
        {
            RunPosted();
            if (DateTime.UtcNow > end)
            {
                throw new TimeoutException("The session task did not complete in time");
            }

            await Task.Delay(5, TestContext.Current.CancellationToken);
        }

        RunPosted();
        await task;
    }

    /// <summary>Runs what is queued, on this thread.</summary>
    public void RunPosted()
    {
        PumpThreads.Add(Environment.CurrentManagedThreadId);
        Post.RunAll();
    }

    public Task StartAsync() => PumpAsync(Session.StartAsync(TestContext.Current.CancellationToken));

    public Task StopAsync() => PumpAsync(Session.StopAsync());

    public async Task<bool> WaitUntilAsync(Func<bool> condition)
    {
        DateTime end = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            RunPosted();
            if (DateTime.UtcNow > end)
            {
                return false;
            }

            await Task.Delay(5, TestContext.Current.CancellationToken);
        }

        return true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await PumpAsync(Session.StopAsync());
        }
        catch (Exception)
        {
            // The test already checked what it wanted.
        }

        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        if (_scripted is not null)
        {
            await _scripted.DisposeAsync();
        }
    }
}
