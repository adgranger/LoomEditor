using System.Text.Json;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.FakeServer;

public class FakeServerLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Initialize_ReturnsCapabilities()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        InitializeResult result = await host.InitializeAsync();

        Assert.Equal("utf-16", result.Capabilities.PositionEncoding);
        Assert.Equal((true, 2), LspUnions.ParseTextDocumentSync(result.Capabilities.TextDocumentSync));
        Assert.Equal(["."], result.Capabilities.CompletionProvider!.TriggerCharacters!);
        Assert.True(result.Capabilities.CompletionProvider?.ResolveProvider);
        Assert.True(LspUnions.IsEnabled(result.Capabilities.HoverProvider));
        Assert.True(LspUnions.IsEnabled(result.Capabilities.DefinitionProvider));
        Assert.Equal("LoomEditor.FakeServer", result.ServerInfo?.Name);
        Assert.Equal("1.0", result.ServerInfo?.Version);
    }

    [Theory]
    [InlineData("none", 0)]
    [InlineData("full", 1)]
    [InlineData("incremental", 2)]
    public async Task Initialize_SyncOption(string sync, int expected)
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { Sync = sync });
        InitializeResult result = await host.InitializeAsync();

        Assert.Equal((true, expected), LspUnions.ParseTextDocumentSync(result.Capabilities.TextDocumentSync));
    }

    [Fact]
    public async Task RequestBeforeInitialize_ServerNotInitialized()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 0)));
        Assert.Equal(JsonRpcErrorCodes.ServerNotInitialized, e.Code);
        Assert.Equal("Server not initialized", e.Message);
    }

    [Fact]
    public async Task NotificationBeforeInitialize_Ignored()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();

        host.Open("text");
        await host.BarrierAsync();

        Assert.Equal("", host.Server!.DocumentText);
        Assert.Null(host.Server.DocumentUri);
    }

    [Fact]
    public async Task SecondInitialize_InvalidRequest()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(
            () => host.RequestAsync(LspMethods.Initialize, """{"processId":null,"rootUri":null,"capabilities":{}}"""));
        Assert.Equal(JsonRpcErrorCodes.InvalidRequest, e.Code);
    }

    [Fact]
    public async Task Shutdown_ThenRequest_InvalidRequest()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();

        JsonElement? result = await host.RequestAsync(LspMethods.Shutdown, "null");
        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 0)));

        Assert.Null(result);
        Assert.Equal(JsonRpcErrorCodes.InvalidRequest, e.Code);
        Assert.Equal("Server is shutting down", e.Message);
    }

    [Fact]
    public async Task ShutdownThenExit_ExitCodeZero()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();
        await host.RequestAsync(LspMethods.Shutdown, "null");

        host.Notify(LspMethods.Exit, "null");

        Assert.Equal(0, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExitWithoutShutdown_ExitCodeOne()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();

        host.Notify(LspMethods.Exit, "null");

        Assert.Equal(1, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InputEndsAfterShutdown_ExitCodeZero()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();
        await host.RequestAsync(LspMethods.Shutdown, "null");

        await host.Connection.DisposeAsync();

        Assert.Equal(0, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InputEndsWithoutShutdown_ExitCodeOne()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();

        await host.Connection.DisposeAsync();

        Assert.Equal(1, await host.ServerExit.WaitAsync(Timeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnknownRequest_MethodNotFound()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => host.RequestAsync("nope/nothing", "{}"));
        Assert.Equal(JsonRpcErrorCodes.MethodNotFound, e.Code);
    }

    [Fact]
    public async Task UnknownNotification_Ignored()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await host.InitializeAsync();

        host.Notify("nope/nothing", "{}");
        JsonElement? hover = await host.RequestAsync(LspMethods.Hover, FakeServerHost.At(0, 0));

        Assert.Null(hover);
        Assert.Contains("nope/nothing", host.Server!.ReceivedMethods);
    }
}
