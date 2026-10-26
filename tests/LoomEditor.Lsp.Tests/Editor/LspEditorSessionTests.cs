using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspEditorSessionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static int Count(SessionRig rig, string language = SessionRig.Language)
        => rig.Features.CompletionProviders.Ordered(language).Count
            + rig.Features.HoverProviders.Ordered(language).Count
            + rig.Features.DefinitionProviders.Ordered(language).Count;

    [Fact]
    public async Task Start_RegistersAllThreeProviders_ForTheLanguage()
    {
        await using SessionRig rig = SessionRig.OnFake();

        await rig.StartAsync();

        Assert.Single(rig.Features.CompletionProviders.Ordered(SessionRig.Language));
        Assert.Single(rig.Features.HoverProviders.Ordered(SessionRig.Language));
        Assert.Single(rig.Features.DefinitionProviders.Ordered(SessionRig.Language));
        Assert.Equal(0, Count(rig, "other"));
    }

    [Fact]
    public async Task Start_OpensTheDocument()
    {
        await using SessionRig rig = SessionRig.OnFake("let x = 1;");

        await rig.StartAsync();
        await rig.Host!.BarrierAsync();

        Assert.Equal(rig.Model.GetValue(), rig.Host.Server!.DocumentText);
        Assert.Equal(SessionRig.Uri, rig.Host.Server.DocumentUri);
    }

    [Fact]
    public async Task Start_StateSequence()
    {
        await using SessionRig rig = SessionRig.OnFake();

        await rig.StartAsync();

        Assert.Equal([LspSessionState.Starting, LspSessionState.Running], rig.States);
        Assert.Equal(LspSessionState.Running, rig.Session.State);
        Assert.Contains(rig.StateThreads[1], rig.PumpThreads);
    }

    [Fact]
    public async Task Start_TypedDuringInitialize_OpensWithTheLatestText()
    {
        using ManualResetEventSlim gate = new();
        ScriptedServer server = new ScriptedServer()
            .WithCapabilities("""{"textDocumentSync":1}""")
            .On("initialize", _ =>
            {
                gate.Wait(Timeout);
                return """{"capabilities":{"textDocumentSync":1}}""";
            });
        await using SessionRig rig = SessionRig.OnScripted(server, "start");

        Task start = rig.Session.StartAsync(TestContext.Current.CancellationToken);
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), " typed")]);
        gate.Set();
        await rig.PumpAsync(start);
        Assert.True(await rig.WaitUntilAsync(() => server.Notifications.Any(n => n.Method == LspMethods.DidOpen)));

        JsonElement open = server.Notifications.First(n => n.Method == LspMethods.DidOpen).Params!.Value;
        Assert.Equal("start typed", open.GetProperty("textDocument").GetProperty("text").GetString());
    }

    [Fact]
    public async Task Start_OnlyHoverCapability_OnlyHoverRegistered()
    {
        ScriptedServer server = new ScriptedServer().WithCapabilities("""{"textDocumentSync":1,"hoverProvider":true}""");
        await using SessionRig rig = SessionRig.OnScripted(server);

        await rig.StartAsync();

        Assert.Empty(rig.Features.CompletionProviders.Ordered(SessionRig.Language));
        Assert.Single(rig.Features.HoverProviders.Ordered(SessionRig.Language));
        Assert.Empty(rig.Features.DefinitionProviders.Ordered(SessionRig.Language));
    }

    [Fact]
    public async Task Start_NoSyncCapability_RegistersNothing_StillRunning()
    {
        ScriptedServer server = new ScriptedServer().WithCapabilities("""{"hoverProvider":true}""");
        await using SessionRig rig = SessionRig.OnScripted(server);

        await rig.StartAsync();

        Assert.Equal(LspSessionState.Running, rig.Session.State);
        Assert.Equal(0, Count(rig));
        Assert.Contains("The server did not declare text document synchronization: language features are off.", rig.Output);
    }

    [Fact]
    public async Task Start_Twice_Throws()
    {
        await using SessionRig rig = SessionRig.OnFake();
        await rig.StartAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => rig.Session.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Start_PostRefused_ShutsTheServerDown()
    {
        await using SessionRig rig = SessionRig.OnFake();
        rig.Post.Accepting = false;

        await rig.Session.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await rig.Host!.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        Assert.Equal(0, Count(rig));
        Assert.Contains("shutdown", rig.Host.Server!.ReceivedMethods);
        Assert.Contains("exit", rig.Host.Server.ReceivedMethods);
        Assert.Equal(LspSessionState.Stopped, rig.Session.State);
    }

    [Fact]
    public async Task Providers_Work_EndToEnd()
    {
        await using SessionRig rig = SessionRig.OnFake("def alpha\nalpha foo");
        await rig.StartAsync();
        CompletionContext invoked = new(CompletionTriggerKind.Invoke);

        CompletionList? list = await rig.Features.CompletionProviders.Ordered(SessionRig.Language)[0]
            .ProvideCompletionItemsAsync(rig.Model, new Position(2, 1), invoked, TestContext.Current.CancellationToken);
        Hover? hover = await rig.Features.HoverProviders.Ordered(SessionRig.Language)[0]
            .ProvideHoverAsync(rig.Model, new Position(1, 6), TestContext.Current.CancellationToken);
        IReadOnlyList<LocationLink>? links = await rig.Features.DefinitionProviders.Ordered(SessionRig.Language)[0]
            .ProvideDefinitionAsync(rig.Model, new Position(2, 2), TestContext.Current.CancellationToken);

        Assert.NotEmpty(list!.Suggestions);
        Assert.Contains("**alpha**", hover!.Contents[0].Value);
        Assert.Equal(new Range(1, 5, 1, 10), Assert.Single(links!).Range);
    }

    [Fact]
    public async Task Diagnostics_ReachTheMarkerService()
    {
        await using SessionRig rig = SessionRig.OnFake("ERROR");
        await rig.StartAsync();

        await rig.Host!.BarrierAsync();
        Assert.True(await rig.WaitUntilAsync(() => rig.Markers.Read("lsp").Count == 1));
        Assert.Single(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Edits_AreSynced()
    {
        await using SessionRig rig = SessionRig.OnFake("hello");
        await rig.StartAsync();

        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), " world")]);
        await rig.Host!.BarrierAsync();

        Assert.Equal("hello world", rig.Host.Server!.DocumentText);
    }

    [Fact]
    public async Task Stop_UnregistersClearsAndShutsDown()
    {
        await using SessionRig rig = SessionRig.OnFake("ERROR");
        await rig.StartAsync();
        Assert.True(await rig.WaitUntilAsync(() => rig.Markers.Read("lsp").Count == 1));

        await rig.StopAsync();
        await rig.Host!.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        Assert.Equal(0, Count(rig));
        Assert.Empty(rig.Markers.Read("lsp"));
        Assert.Equal(LspSessionState.Stopped, rig.Session.State);
        IReadOnlyList<string> received = rig.Host.Server!.ReceivedMethods;
        Assert.True(received.ToList().IndexOf(LspMethods.DidClose) < received.ToList().IndexOf("shutdown"));
        Assert.Contains("exit", received);
    }

    [Fact]
    public async Task Stop_Twice_Ok()
    {
        await using SessionRig rig = SessionRig.OnFake();
        await rig.StartAsync();

        Task first = rig.Session.StopAsync();
        Task second = rig.Session.StopAsync();
        await rig.PumpAsync(first);
        await rig.PumpAsync(second);
        await rig.StopAsync();

        Assert.Same(first, second);
        Assert.Equal(LspSessionState.Stopped, rig.Session.State);
    }

    [Fact]
    public async Task Stop_WhileStarting_Cancels()
    {
        ScriptedServer server = new ScriptedServer().Silent("initialize");
        await using SessionRig rig = SessionRig.OnScripted(server);

        Task start = rig.Session.StartAsync(TestContext.Current.CancellationToken);
        await rig.StopAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rig.PumpAsync(start));
        Assert.Equal(LspSessionState.Stopped, rig.Session.State);
        Assert.Equal(0, Count(rig));
    }

    [Fact]
    public async Task Dispose_IsStop()
    {
        await using SessionRig rig = SessionRig.OnFake();
        await rig.StartAsync();

        await rig.PumpAsync(rig.Session.DisposeAsync().AsTask());

        Assert.Equal(LspSessionState.Stopped, rig.Session.State);
        Assert.Equal(0, Count(rig));
    }

    [Fact]
    public async Task ServerOutput_LogAndShowMessages()
    {
        await using SessionRig rig = SessionRig.OnFake(options: new FakeServerOptions { Chatty = true });
        await rig.StartAsync();

        Assert.True(await rig.WaitUntilAsync(() => rig.Output.Contains("(notification) [warning] fake warning")));
        Assert.Contains("[info] fake server ready", rig.Output);
    }

    [Fact]
    public async Task ServerOutput_Stderr()
    {
        LspServerCommand command = FakeServerLocator.GetCommand(new FakeServerOptions { Stderr = "hello" });
        SessionRig rig = SessionRig.OnFake();
        LspEditorSession session = new(rig.Model, rig.Features, rig.Markers, SessionRig.Uri, SessionRig.Language, command, rig.Post.Post);
        session.ServerOutput += rig.Output.Add;
        try
        {
            await rig.PumpAsync(session.StartAsync(TestContext.Current.CancellationToken));

            Assert.True(await rig.WaitUntilAsync(() => rig.Output.Contains("[stderr] hello")));
        }
        finally
        {
            await rig.PumpAsync(session.StopAsync());
            await rig.DisposeAsync();
        }
    }
}
