using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Tests.TestHelpers;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.Highlighting.TestHelpers;
using LoomEditor.View.Tests.Hover.TestHelpers;
using LoomEditor.View.Tests.Suggest.TestHelpers;

namespace LoomEditor.View.Tests.Lsp;

/// <summary>A real editor with the default features, connected through an <see cref="LspEditorSession"/> to the fake language server.</summary>
/// <remarks>Threading: the test thread is the UI thread; <see cref="PumpUntilAsync"/> runs what the session posted and advances the editor's clock.</remarks>
internal sealed class LspEditorHarness : IAsyncDisposable
{
    public const string Language = "fake";

    private readonly EditorFeatureHost _features;

    private LspEditorHarness(string text, Func<LspEditorHarness, LspEditorSession> createSession, FakeServerHost? host)
    {
        Host = host;
        Scheduler = new ManualEditorScheduler();
        Dispatcher = new ManualUiDispatcher();
        Context = new EditorContext(static _ => new NoopDisposable(), TimeProvider.System, Dispatcher, Scheduler);
        Context.RegisterLanguage(Language);
        Context.Model.Tokenization.SetLanguageId(Language);
        Context.SetText(text);
        Context.SuggestPresenter = Suggest;
        Context.HoverPresenter = Hover;
        Context.Announcer = Announcer;
        _features = new EditorFeatureHost(Context, EditorFeatures.Default);
        Context.ViewModel.SetHasFocus(true);
        Context.ViewModel.SetViewportSize(800, 600);
        Session = createSession(this);
    }

    public EditorContext Context { get; }

    public ManualEditorScheduler Scheduler { get; }

    public ManualUiDispatcher Dispatcher { get; }

    public FakeSuggestPresenter Suggest { get; } = new();

    public FakeHoverPresenter Hover { get; } = new();

    public RecordingAnnouncer Announcer { get; } = new();

    public FakeServerHost? Host { get; }

    public FakeLanguageServer? Server => Host?.Server;

    public LspEditorSession Session { get; }

    public string Text => Context.Model.GetValue();

    /// <summary>The labels of the list on screen, in order.</summary>
    public IReadOnlyList<string> Labels => Suggest.IsVisible ? [.. Suggest.State!.Items.Select(item => item.TextLabel)] : [];

    /// <summary>A harness on an in-process fake server.</summary>
    public static LspEditorHarness InProcess(string text = "", FakeServerOptions? options = null)
    {
        FakeServerHost host = FakeServerHost.StartInProcess(options);
        return new LspEditorHarness(text, h => h.NewSession(host.CreateSessionClientFactory()), host);
    }

    /// <summary>A harness whose session starts <paramref name="command"/> itself (the public constructor).</summary>
    public static LspEditorHarness OnCommand(LspServerCommand command, string text = "")
        => new(text, h => new LspEditorSession(h.Context.Model, h.Context.LanguageFeatures, h.Context.Markers, h.Context.DocumentUri, Language, command, h.Dispatcher.Post), null);

    private LspEditorSession NewSession(Func<LspClient> factory)
        => new(Context.Model, Context.LanguageFeatures, Context.Markers, Context.DocumentUri, Language, factory, Dispatcher.Post);

    public async Task StartAsync()
    {
        Task start = Session.StartAsync(TestContext.Current.CancellationToken);
        await PumpUntilAsync(() => start.IsCompleted, "the session to start");
        await start;
    }

    public void SetText(string text) => Context.SetText(text);

    /// <summary>Makes the hover leave with the mouse (the default hover is sticky and stays until the mouse moves elsewhere).</summary>
    public void DisableStickyHover()
        => _features.Features.OfType<LoomEditor.View.Features.Hover.HoverFeature>().Single().Options = LoomEditor.View.Hover.EditorHoverOptions.Default with { Sticky = false };

    public void MoveCaret(int line, int column)
        => Context.ViewModel.SetSelections("keyboard", [Selection.Collapsed(new Position(line, column))], LoomEditor.View.Cursors.CursorChangeReason.Explicit);

    public void Type(string text)
    {
        foreach (char c in text)
        {
            Context.ViewModel.Type(c.ToString(), "keyboard");
        }
    }

    public void Command(string id) => Assert.True(Context.Commands.Execute(id), $"command {id} is not registered");

    public void MoveMouseTo(int line, int column)
    {
        Position position = new(line, column);
        MouseTarget target = new(MouseTargetType.ContentText, position, column, Range.Collapsed(position), false, null);
        Context.UserInputEvents.EmitMouseMove(new EditorMouseEventArgs(Mouse(left: false, ctrl: false), target));
    }

    public void CtrlClick(int line, int column)
    {
        Position position = new(line, column);
        MouseTarget target = new(MouseTargetType.ContentText, position, column, Range.Collapsed(position), false, null);
        Context.UserInputEvents.EmitMouseUp(new EditorMouseEventArgs(Mouse(left: true, ctrl: true), target));
    }

    /// <summary>Runs the posted actions and advances the clock by 20 ms until <paramref name="condition"/> holds.</summary>
    public async Task PumpUntilAsync(Func<bool> condition, string what, TimeSpan? timeout = null)
    {
        DateTime end = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (true)
        {
            Dispatcher.RunAll();
            Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(20));
            if (condition())
            {
                return;
            }

            if (DateTime.UtcNow > end)
            {
                throw new TimeoutException($"Timed out waiting for {what}");
            }

            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Pumps for a fixed time (for the checks that something does NOT happen).</summary>
    public async Task SettleAsync(int milliseconds = 150)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(milliseconds);
        await PumpUntilAsync(() => DateTime.UtcNow > end, "the editor to settle", TimeSpan.FromSeconds(10));
    }

    /// <summary>Waits until the server saw every change, then pumps what it answered.</summary>
    public async Task SyncAsync()
    {
        await Host!.BarrierAsync();
        await SettleAsync(60);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            Task stop = Session.StopAsync();
            await PumpUntilAsync(() => stop.IsCompleted, "the session to stop");
            await stop;
        }
        catch (Exception)
        {
            // The test already checked what it wanted.
        }

        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        _features.Dispose();
        Context.Dispose();
    }

    private static EditorMouseEvent Mouse(bool left, bool ctrl) => new(5, 5, left, false, false, ctrl, false, false, 4, 0);

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
