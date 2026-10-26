using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Client;

public class LspClientFailureTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Initialize_ServerReturnsError_Faulted_Throws()
    {
        await using ScriptedServer server = new ScriptedServer().On(LspMethods.Initialize, _ => throw new JsonRpcException(-32603, "no way")).Start();
        await using LspClient client = new(server.Connection, new LspClientOptions());

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => client.InitializeAsync(TestContext.Current.CancellationToken));

        Assert.Equal("no way", e.Message);
        Assert.Equal(LspClientState.Faulted, client.State);
        Assert.Same(e, client.FaultReason);
    }

    [Fact]
    public async Task Initialize_Timeout_Faulted_TimeoutException()
    {
        await using ScriptedServer server = new ScriptedServer().Silent(LspMethods.Initialize).Start();
        await using LspClient client = new(server.Connection, new LspClientOptions { InitializeTimeout = TimeSpan.FromMilliseconds(300) });

        TimeoutException e = await Assert.ThrowsAsync<TimeoutException>(() => client.InitializeAsync(TestContext.Current.CancellationToken));

        Assert.Contains("did not answer initialize", e.Message);
        Assert.Contains("300", e.Message);
        Assert.Equal(LspClientState.Faulted, client.State);
    }

    [Fact]
    public async Task Initialize_Cancelled_Faulted()
    {
        await using ScriptedServer server = new ScriptedServer().Silent(LspMethods.Initialize).Start();
        await using LspClient client = new(server.Connection, new LspClientOptions());
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.InitializeAsync(cts.Token));

        Assert.Equal(LspClientState.Faulted, client.State);
    }

    [Fact]
    public async Task Initialize_ServerExitsDuringHandshake_Faulted()
    {
        await using ProcessRig rig = new(new FakeServerOptions { ExitOn = LspMethods.Initialize, Stderr = "boom" });

        LspClientException e = await Assert.ThrowsAsync<LspClientException>(() => rig.Client.InitializeAsync(TestContext.Current.CancellationToken));

        Assert.Contains("boom", e.Message);
        Assert.Equal(LspClientState.Faulted, rig.Client.State);
    }

    [Fact]
    public async Task Crash_WhileRunning_Faulted_EventRaised_ProcessGone()
    {
        await using ProcessRig rig = new(new FakeServerOptions { ExitOn = LspMethods.DidOpen });
        TaskCompletionSource faulted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Client.StateChanged += state =>
        {
            if (state == LspClientState.Faulted)
            {
                faulted.TrySetResult();
            }
        };
        await rig.Client.InitializeAsync(TestContext.Current.CancellationToken);

        rig.Client.Notify(
            LspMethods.DidOpen,
            new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = "file:///t.txt", LanguageId = "fake", Version = 1, Text = "x" } },
            LspJsonContext.Default.DidOpenTextDocumentParams);

        await faulted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Contains("closed the connection", rig.Client.FaultReason!.Message);
        await rig.Client.Process!.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.True(rig.Client.Process.HasExited);
    }

    [Fact]
    public async Task Faulted_Request_Throws()
    {
        await using ScriptedServer server = new ScriptedServer().On(LspMethods.Initialize, _ => throw new JsonRpcException(-32603, "x")).Start();
        await using LspClient client = new(server.Connection, new LspClientOptions());
        await Assert.ThrowsAsync<JsonRpcException>(() => client.InitializeAsync(TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<LspClientException>(() => client.RequestRawAsync(LspMethods.Hover, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Faulted_Notify_Throws()
    {
        await using ScriptedServer server = new ScriptedServer().On(LspMethods.Initialize, _ => throw new JsonRpcException(-32603, "x")).Start();
        await using LspClient client = new(server.Connection, new LspClientOptions());
        await Assert.ThrowsAsync<JsonRpcException>(() => client.InitializeAsync(TestContext.Current.CancellationToken));

        Assert.Throws<LspClientException>(() => client.Notify(
            LspMethods.DidClose,
            new DidCloseTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = "u" } },
            LspJsonContext.Default.DidCloseTextDocumentParams));
    }

    [Fact]
    public void StartProcess_FileNotFound_ThrowsStartException()
    {
        LspServerStartException e = Assert.Throws<LspServerStartException>(
            () => LspClient.StartProcess(new LspServerCommand("definitely-not-a-server-" + Guid.NewGuid().ToString("N"), []), new LspClientOptions()));

        Assert.Contains("definitely-not-a-server", e.Message);
        Assert.NotNull(e.InnerException);
    }

    [Fact]
    public void StartProcess_WorkingDirectoryNotFound_ThrowsStartException()
    {
        LspServerCommand command = FakeServerLocator.GetCommand() with { WorkingDirectory = Path.Combine(Path.GetTempPath(), "no-such-dir-" + Guid.NewGuid().ToString("N")) };

        Assert.Throws<LspServerStartException>(() => LspClient.StartProcess(command, new LspClientOptions()));
    }
}
