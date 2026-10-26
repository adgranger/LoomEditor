using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Client;

public class LspClientShutdownTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Shutdown_Handshake_ServerSeesShutdownThenExit_StateStopped()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        await client.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(LspClientState.Stopped, client.State);
        Assert.Equal(0, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Equal(["initialize", "initialized", "shutdown", "exit"], host.Server!.ReceivedMethods);
    }

    [Fact]
    public async Task Shutdown_ServerExitCodeZero()
    {
        await using ProcessRig rig = new();
        await rig.Client.InitializeAsync(TestContext.Current.CancellationToken);

        await rig.Client.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(LspClientState.Stopped, rig.Client.State);
        Assert.Equal(0, rig.Client.Process!.ExitCode);
    }

    [Fact]
    public async Task Shutdown_WhenNotRunning_NoOp()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, new LspClientOptions());

        await client.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(LspClientState.NotStarted, client.State);
    }

    [Fact]
    public async Task Shutdown_ServerSilent_TimesOutAndKills()
    {
        await using ScriptedServer server = new ScriptedServer().Silent(LspMethods.Shutdown).Start();
        await using LspClient client = new(server.Connection, new LspClientOptions { ShutdownTimeout = TimeSpan.FromMilliseconds(300) });
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        await client.ShutdownAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        Assert.Equal(LspClientState.Stopped, client.State);
    }

    [Fact]
    public async Task Dispose_WhileRunning_ShutsDown()
    {
        FakeServerHost host = FakeServerHost.StartInProcess();
        await using FakeServerHost h = host;
        LspClient client = new(host.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        await client.DisposeAsync();

        Assert.Equal(LspClientState.Stopped, client.State);
        Assert.Contains("shutdown", host.Server!.ReceivedMethods);
    }

    [Fact]
    public async Task Dispose_Twice_Ok()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        LspClient client = new(host.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        await client.DisposeAsync();
        await client.DisposeAsync();

        Assert.Equal(LspClientState.Stopped, client.State);
    }

    [Fact]
    public async Task Dispose_AfterFault_DoesNotThrow()
    {
        await using ScriptedServer server = new ScriptedServer().On(LspMethods.Initialize, _ => throw new LoomEditor.Lsp.JsonRpc.JsonRpcException(-32603, "boom")).Start();
        LspClient client = new(server.Connection, new LspClientOptions());
        await Assert.ThrowsAsync<LoomEditor.Lsp.JsonRpc.JsonRpcException>(() => client.InitializeAsync(TestContext.Current.CancellationToken));

        await client.DisposeAsync();

        Assert.Equal(LspClientState.Faulted, client.State);
    }
}
