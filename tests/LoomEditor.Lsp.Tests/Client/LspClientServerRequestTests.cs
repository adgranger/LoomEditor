using System.Text.Json;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Client;

public class LspClientServerRequestTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static async Task<JsonElement?> ResponseAsync(FakeLanguageServer server, string id)
    {
        DateTime end = DateTime.UtcNow + Timeout;
        while (!server.ReceivedMethods.Contains($"response({id})"))
        {
            Assert.True(DateTime.UtcNow < end, $"No response({id}) within {Timeout}");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        return server.LastResponseResult(id);
    }

    private static async Task<JsonElement?> ChattyResponseAsync(string id)
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { Chatty = true });
        await using LspClient client = new(host.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);
        return await ResponseAsync(host.Server!, id);
    }

    private static async Task<LspLogMessageEventArgs> ChattyMessageAsync(bool show)
    {
        TaskCompletionSource<LspLogMessageEventArgs> got = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using FakeServerHost host = FakeServerHost.StartInProcess(new FakeServerOptions { Chatty = true });
        await using LspClient client = new(host.Connection, new LspClientOptions());
        client.LogMessageReceived += (_, e) =>
        {
            if (e.IsShowMessage == show)
            {
                got.TrySetResult(e);
            }
        };
        await client.InitializeAsync(TestContext.Current.CancellationToken);
        return await got.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConfigurationRequest_AnsweredWithNulls()
    {
        JsonElement? result = await ChattyResponseAsync("cfg-1");

        Assert.Equal(JsonValueKind.Null, Assert.Single(result!.Value.EnumerateArray()).ValueKind);
    }

    [Fact]
    public async Task RegisterCapability_AnsweredNull()
        => Assert.Null(await ChattyResponseAsync("reg-1"));

    [Fact]
    public async Task WorkDoneProgressCreate_AnsweredNull()
        => Assert.Null(await ChattyResponseAsync("wdp-1"));

    [Fact]
    public async Task ApplyEdit_AnsweredNotApplied()
        => Assert.False((await ChattyResponseAsync("edit-1"))!.Value.GetProperty("applied").GetBoolean());

    [Fact]
    public async Task LogMessage_RaisesEvent()
    {
        LspLogMessageEventArgs args = await ChattyMessageAsync(show: false);

        Assert.Equal(3, args.Type);
        Assert.Equal("fake server ready", args.Message);
    }

    [Fact]
    public async Task ShowMessage_RaisesEvent()
    {
        LspLogMessageEventArgs args = await ChattyMessageAsync(show: true);

        Assert.Equal(2, args.Type);
        Assert.Equal("fake warning", args.Message);
    }

    [Fact]
    public async Task Diagnostics_RaisesEvent()
    {
        TaskCompletionSource<PublishDiagnosticsParams> got = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, new LspClientOptions());
        client.DiagnosticsReceived += p => got.TrySetResult(p);
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        client.Notify(
            LspMethods.DidOpen,
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem { Uri = "file:///t.txt", LanguageId = "fake", Version = 1, Text = "an ERROR here" },
            },
            LspJsonContext.Default.DidOpenTextDocumentParams);

        PublishDiagnosticsParams diagnostics = await got.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        Assert.Equal("file:///t.txt", diagnostics.Uri);
        Assert.Single(diagnostics.Diagnostics);
    }

    [Fact]
    public async Task UnknownServerRequest_MethodNotFound()
    {
        await using ScriptedServer server = new ScriptedServer().Start();
        await using LspClient client = new(server.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);

        JsonRpcMessage response = await server.SendRequestAsync("x-1", "custom/unknown", "{}");

        Assert.Equal(JsonRpcErrorCodes.MethodNotFound, response.Error!.Code);
    }
}
