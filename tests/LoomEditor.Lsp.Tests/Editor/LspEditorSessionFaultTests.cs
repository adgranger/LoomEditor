using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspEditorSessionFaultTests
{
    private static int Count(SessionRig rig)
        => rig.Features.CompletionProviders.Ordered(SessionRig.Language).Count
            + rig.Features.HoverProviders.Ordered(SessionRig.Language).Count
            + rig.Features.DefinitionProviders.Ordered(SessionRig.Language).Count;

    /// <summary>A session on a fake server child process through the public constructor.</summary>
    private static (LspEditorSession Session, SessionRig Rig) OnProcess(LspServerCommand command)
    {
        SessionRig rig = SessionRig.OnFake();
        LspEditorSession session = new(rig.Model, rig.Features, rig.Markers, SessionRig.Uri, SessionRig.Language, command, rig.Post.Post);
        return (session, rig);
    }

    [Fact]
    public async Task InitializeFails_Faulted_StartTaskFaults()
    {
        ScriptedServer server = new ScriptedServer().On("initialize", _ => throw new JsonRpcException(JsonRpcErrorCodes.InternalError, "boom"));
        await using SessionRig rig = SessionRig.OnScripted(server);

        await Assert.ThrowsAnyAsync<Exception>(() => rig.StartAsync());

        Assert.True(await rig.WaitUntilAsync(() => rig.Session.State == LspSessionState.Faulted));
        Assert.Contains("boom", rig.Session.FaultMessage);
        Assert.Equal(0, Count(rig));
    }

    [Fact]
    public async Task InitializeTimeout_Faulted()
    {
        ScriptedServer server = new ScriptedServer().Silent("initialize");
        await using SessionRig rig = SessionRig.OnScripted(server, clientOptions: new LspClientOptions { InitializeTimeout = TimeSpan.FromMilliseconds(300) });

        await Assert.ThrowsAsync<TimeoutException>(() => rig.StartAsync());

        Assert.True(await rig.WaitUntilAsync(() => rig.Session.State == LspSessionState.Faulted));
        Assert.NotNull(rig.Session.FaultMessage);
    }

    [Fact]
    public async Task ServerCrashesWhileRunning_FaultedAndEverythingRemoved()
    {
        LspServerCommand command = FakeServerLocator.GetCommand(new FakeServerOptions { ExitOn = "textDocument/didChange" });
        (LspEditorSession session, SessionRig rig) = OnProcess(command);
        List<string> output = [];
        session.ServerOutput += output.Add;
        try
        {
            await rig.PumpAsync(session.StartAsync(TestContext.Current.CancellationToken));
            Assert.Equal(LspSessionState.Running, session.State);

            rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), "!")]);

            Assert.True(await rig.WaitUntilAsync(() => session.State == LspSessionState.Faulted));
            Assert.NotNull(session.FaultMessage);
            Assert.Equal(0, Count(rig));
            Assert.Empty(rig.Markers.Read("lsp"));
            Assert.Contains(session.FaultMessage, output);
        }
        finally
        {
            await rig.PumpAsync(session.StopAsync());
            await rig.DisposeAsync();
        }
    }

    [Fact]
    public async Task Fault_DoesNotRestart()
    {
        LspServerCommand command = FakeServerLocator.GetCommand(new FakeServerOptions { ExitOn = "textDocument/didChange" });
        (LspEditorSession session, SessionRig rig) = OnProcess(command);
        try
        {
            await rig.PumpAsync(session.StartAsync(TestContext.Current.CancellationToken));
            rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), "!")]);
            Assert.True(await rig.WaitUntilAsync(() => session.State == LspSessionState.Faulted));

            await Assert.ThrowsAsync<InvalidOperationException>(() => session.StartAsync(TestContext.Current.CancellationToken));
            await Task.Delay(200, TestContext.Current.CancellationToken);
            rig.RunPosted();

            Assert.Equal(LspSessionState.Faulted, session.State);
            Assert.Equal(0, Count(rig));
        }
        finally
        {
            await rig.PumpAsync(session.StopAsync());
            await rig.DisposeAsync();
        }
    }

    [Fact]
    public async Task StartProcess_FileNotFound_PublicConstructor_Faulted_AndTaskFaults()
    {
        (LspEditorSession session, SessionRig rig) = OnProcess(new LspServerCommand("this-does-not-exist-xyz", []));
        await using (rig)
        {
            await Assert.ThrowsAsync<LspServerStartException>(() => rig.PumpAsync(session.StartAsync(TestContext.Current.CancellationToken)));

            Assert.Equal(LspSessionState.Faulted, session.State);
            Assert.Contains("this-does-not-exist-xyz", session.FaultMessage);
        }
    }

    [Fact]
    public async Task Stop_AfterFault_Ok()
    {
        ScriptedServer server = new ScriptedServer().Silent("initialize");
        await using SessionRig rig = SessionRig.OnScripted(server, clientOptions: new LspClientOptions { InitializeTimeout = TimeSpan.FromMilliseconds(200) });
        await Assert.ThrowsAsync<TimeoutException>(() => rig.StartAsync());
        Assert.True(await rig.WaitUntilAsync(() => rig.Session.State == LspSessionState.Faulted));

        await rig.StopAsync();

        Assert.Equal(LspSessionState.Stopped, rig.Session.State);
    }

    [Fact]
    public async Task Edits_AfterFault_DoNotThrow()
    {
        ScriptedServer server = new ScriptedServer().Silent("initialize");
        await using SessionRig rig = SessionRig.OnScripted(server, clientOptions: new LspClientOptions { InitializeTimeout = TimeSpan.FromMilliseconds(200) });
        await Assert.ThrowsAsync<TimeoutException>(() => rig.StartAsync());
        Assert.True(await rig.WaitUntilAsync(() => rig.Session.State == LspSessionState.Faulted));

        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), "!")]);

        Assert.Equal(LspSessionState.Faulted, rig.Session.State);
    }

    [Fact]
    public async Task ModelEditedAfterStop_NoNotifications()
    {
        await using SessionRig rig = SessionRig.OnFake("hello");
        await rig.StartAsync();
        await rig.StopAsync();
        await rig.Host!.ServerExit.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        int received = rig.Host.Server!.ReceivedMethods.Count;

        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), "!")]);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(received, rig.Host.Server.ReceivedMethods.Count);
    }
}
