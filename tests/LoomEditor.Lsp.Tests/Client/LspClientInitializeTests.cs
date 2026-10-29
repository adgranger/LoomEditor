using System.Text.Json;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Client;

public class LspClientInitializeTests
{
    private static readonly LspClientOptions Options = new() { RootUri = "file:///root" };

    [Fact]
    public async Task Initialize_HandshakeSucceeds()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        InitializeResult result = await client.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(LspClientState.Running, client.State);
        Assert.Same(result.Capabilities, client.Capabilities);
    }

    [Fact]
    public async Task Initialize_DeclaresSnippetSupport()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        await client.InitializeAsync(TestContext.Current.CancellationToken);

        JsonElement p = host.Server!.LastInitializeParams!.Value;
        Assert.Equal(JsonValueKind.Number, p.GetProperty("processId").ValueKind);
        Assert.Equal("LoomEditor", p.GetProperty("clientInfo").GetProperty("name").GetString());
        Assert.Equal("file:///root", p.GetProperty("rootUri").GetString());
        JsonElement capabilities = p.GetProperty("capabilities");
        Assert.True(capabilities.GetProperty("textDocument").GetProperty("completion").GetProperty("completionItem").GetProperty("snippetSupport").GetBoolean());
        Assert.Equal("utf-16", Assert.Single(capabilities.GetProperty("general").GetProperty("positionEncodings").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task Initialize_ThenInitializedNotificationReceived()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        await client.InitializeAsync(TestContext.Current.CancellationToken);
        await host.BarrierAsync();

        Assert.Equal(["initialize", "initialized"], host.Server!.ReceivedMethods.Take(2));
    }

    [Fact]
    public async Task State_Transitions_Raised()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);
        List<LspClientState> states = [];
        client.StateChanged += states.Add;

        await client.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal([LspClientState.Initializing, LspClientState.Running], states);
    }

    [Fact]
    public async Task CapabilityHelpers_FromFakeServer()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        await client.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.True(client.SupportsCompletion);
        Assert.Equal(["."], client.CompletionTriggerCharacters);
        Assert.True(client.SupportsCompletionResolve);
        Assert.True(client.SupportsHover);
        Assert.True(client.SupportsDefinition);
        Assert.Equal((true, 2), client.TextDocumentSync);
    }

    [Fact]
    public async Task CapabilityHelpers_FullSync_ChangeKindOne()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { Sync = "full" });
        await using LspClient client = new(host.Connection, Options);

        await client.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal((true, 1), client.TextDocumentSync);
    }

    [Fact]
    public async Task CapabilityHelpers_BeforeRunning_Empty()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        Assert.False(client.SupportsCompletion);
        Assert.Empty(client.CompletionTriggerCharacters);
        Assert.False(client.SupportsCompletionResolve);
        Assert.False(client.SupportsHover);
        Assert.False(client.SupportsDefinition);
        Assert.Equal((false, 0), client.TextDocumentSync);
    }

    [Fact]
    public async Task Initialize_Twice_Throws()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.InitializeAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Request_BeforeInitialize_Throws()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        await Assert.ThrowsAsync<LspClientException>(() => client.RequestRawAsync(LspMethods.Hover, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Notify_BeforeInitialize_Throws()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);

        Assert.Throws<LspClientException>(() => client.Notify(LspMethods.DidClose, Close(), LspJsonContext.Default.DidCloseTextDocumentParams));
    }

    [Fact]
    public async Task Request_AfterInitialize_Works()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);
        await client.InitializeAsync(TestContext.Current.CancellationToken);
        client.Notify(
            LspMethods.DidOpen,
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem { Uri = "file:///t.txt", LanguageId = "fake", Version = 1, Text = "hello" },
            },
            LspJsonContext.Default.DidOpenTextDocumentParams);

        LspHover? hover = await client.RequestAsync(
            LspMethods.Hover,
            new TextDocumentPositionParams { TextDocument = new TextDocumentIdentifier { Uri = "file:///t.txt" }, Position = new LspPosition { Line = 0, Character = 2 } },
            LspJsonContext.Default.TextDocumentPositionParams,
            LspJsonContext.Default.LspHover,
            TestContext.Current.CancellationToken);

        Assert.NotNull(hover);
    }

    [Fact]
    public async Task Notify_DoesNotBlock()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, Options);
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        Task burst = Task.Run(
            () =>
            {
                for (int i = 0; i < 500; i++)
                {
                    client.Notify(LspMethods.DidClose, Close(), LspJsonContext.Default.DidCloseTextDocumentParams);
                }
            },
            TestContext.Current.CancellationToken);

        await burst.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private static DidCloseTextDocumentParams Close() => new() { TextDocument = new TextDocumentIdentifier { Uri = "file:///t.txt" } };
}
