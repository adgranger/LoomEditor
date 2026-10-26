using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspCompletionProviderTests
{
    private const string Uri = "file:///t.txt";
    private static readonly CompletionContext Invoked = new(CompletionTriggerKind.Invoke);
    private static readonly CompletionContext Dot = new(CompletionTriggerKind.TriggerCharacter, ".");

    private sealed class Rig(FakeServerHost? host, ScriptedServer? scripted, LspClient client, TextModel model, LspDocumentSync? sync) : IAsyncDisposable
    {
        public FakeServerHost? Host => host;

        public LspClient Client { get; } = client;

        public TextModel Model { get; } = model;

        public LspCompletionProvider Provider { get; } = new(client, Uri);

        public static async Task<Rig> StartFakeAsync(string text, FakeServerOptions? options = null)
        {
            FakeServerHost host = FakeServerHost.StartInProcess(options);
            LspClient client = new(host.Connection, new LspClientOptions());
            await client.InitializeAsync(TestContext.Current.CancellationToken);
            TextModel model = new(text, DefaultEndOfLine.LF);
            LspDocumentSync sync = new(client, model, Uri, "fake");
            sync.Open();
            await host.BarrierAsync();
            return new Rig(host, null, client, model, sync);
        }

        public static async Task<Rig> StartScriptedAsync(ScriptedServer server)
        {
            server.Start();
            LspClient client = new(server.Connection, new LspClientOptions());
            await client.InitializeAsync(TestContext.Current.CancellationToken);
            return new Rig(null, server, client, new TextModel("foo", DefaultEndOfLine.LF), null);
        }

        public Task<CompletionList?> CompleteAsync(Position position, CompletionContext context, CancellationToken token = default)
            => Provider.ProvideCompletionItemsAsync(Model, position, context, token);

        public async ValueTask DisposeAsync()
        {
            sync?.Dispose();
            await Client.DisposeAsync();
            if (host is not null)
            {
                await host.DisposeAsync();
            }

            if (scripted is not null)
            {
                await scripted.DisposeAsync();
            }
        }
    }

    private static string[] Labels(CompletionList? list) => [.. list!.Suggestions.Select(i => i.Label)];

    [Fact]
    public async Task TriggerCharacters_FromServer()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo");

        Assert.Equal(["."], rig.Provider.TriggerCharacters);
    }

    [Fact]
    public async Task CanResolve_FromServer()
    {
        await using Rig fake = await Rig.StartFakeAsync("foo");
        await using Rig scripted = await Rig.StartScriptedAsync(new ScriptedServer().WithCapabilities("""{"completionProvider":{}}"""));

        Assert.True(fake.Provider.CanResolve);
        Assert.False(scripted.Provider.CanResolve);
    }

    [Fact]
    public async Task Complete_Words_ReturnsDistinctWordsAndKeywords()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo foobar bar\nbaz");

        CompletionList? list = await rig.CompleteAsync(new Position(2, 1), Invoked, TestContext.Current.CancellationToken);

        Assert.Equal(["bar", "baz", "foo", "foobar", "if", "else", "for", "class"], Labels(list));
        Assert.Equal(CompletionItemKind.Text, list!.Suggestions[0].Kind);
        Assert.Equal(CompletionItemKind.Keyword, list.Suggestions[4].Kind);
        Assert.False(list.Incomplete);
    }

    [Fact]
    public async Task Complete_PositionIsSentZeroBased()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo foobar bar\nbaz");

        // Column 4 is the end of "foo": the word ending at the cursor is not offered.
        CompletionList? list = await rig.CompleteAsync(new Position(1, 4), Invoked, TestContext.Current.CancellationToken);

        Assert.Equal(["bar", "baz", "foobar", "if", "else", "for", "class"], Labels(list));
    }

    [Fact]
    public async Task Complete_TriggerDot_MemberItemsWithInsertReplaceRanges()
    {
        await using Rig rig = await Rig.StartFakeAsync("obj.le");

        CompletionList? list = await rig.CompleteAsync(new Position(1, 5), Dot, TestContext.Current.CancellationToken);

        Assert.Equal(["Length", "Count", "ToString", "Equals"], Labels(list)[..4]);
        CompletionRange range = list!.Suggestions[2].Range!.Value;
        Assert.Equal(new Range(1, 5, 1, 5), range.Insert);
        Assert.Equal(new Range(1, 5, 1, 7), range.Replace);
    }

    [Fact]
    public async Task Complete_Preselect_Keywords()
    {
        await using Rig rig = await Rig.StartFakeAsync("");

        CompletionList? list = await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken);

        Assert.Equal([true, false, false, false], list!.Suggestions.Select(i => i.Preselect).ToArray());
    }

    [Fact]
    public async Task Complete_Incomplete_Flag()
    {
        await using Rig rig = await Rig.StartFakeAsync("abc", new FakeServerOptions { Incomplete = true });

        CompletionList? list = await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken);

        Assert.True(list!.Incomplete);
    }

    [Fact]
    public async Task Complete_ItemsKeepRawItemInProviderData()
    {
        await using Rig rig = await Rig.StartFakeAsync("abc");

        CompletionList? list = await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken);

        Assert.All(list!.Suggestions, i => Assert.Equal(i.Label, Assert.IsType<LspCompletionItem>(i.ProviderData).Label));
    }

    [Fact]
    public async Task Complete_ContextSent()
    {
        string? seen = null;
        ScriptedServer server = new ScriptedServer()
            .WithCapabilities("""{"completionProvider":{"triggerCharacters":["."]}}""")
            .On(LspMethods.Completion, p =>
            {
                seen = p!.Value.GetProperty("context").GetRawText() + p.Value.GetProperty("position").GetRawText();
                return """{"isIncomplete":false,"items":[]}""";
            });
        await using Rig rig = await Rig.StartScriptedAsync(server);

        await rig.CompleteAsync(new Position(2, 4), Dot, TestContext.Current.CancellationToken);

        Assert.Equal("""{"triggerKind":2,"triggerCharacter":"."}{"line":1,"character":3}""", seen);
    }

    [Fact]
    public async Task Complete_ClientNotRunning_Null()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo");
        await rig.Client.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Null(await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(JsonRpcErrorCodes.ContentModified)]
    [InlineData(JsonRpcErrorCodes.RequestCancelled)]
    [InlineData(JsonRpcErrorCodes.ServerCancelled)]
    public async Task Complete_ContentModified_Null(int code)
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Completion, _ => throw new JsonRpcException(code, "modified"));
        await using Rig rig = await Rig.StartScriptedAsync(server);

        Assert.Null(await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Complete_OtherError_Faults()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.Completion, _ => throw new JsonRpcException(JsonRpcErrorCodes.InternalError, "boom"));
        await using Rig rig = await Rig.StartScriptedAsync(server);

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken));

        Assert.Equal(JsonRpcErrorCodes.InternalError, e.Code);
    }

    [Fact]
    public async Task Complete_Cancelled_TaskCanceled_AndCancelRequestSent()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo", new FakeServerOptions { DelayMs = 500 });
        using CancellationTokenSource cts = new();

        Task<CompletionList?> task = rig.CompleteAsync(new Position(1, 1), Invoked, cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        DateTime end = DateTime.UtcNow.AddSeconds(5);
        while (!rig.Host!.Server!.ReceivedMethods.Contains("$/cancelRequest") && DateTime.UtcNow < end)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Contains("$/cancelRequest", rig.Host.Server.ReceivedMethods);
    }

    [Fact]
    public async Task Complete_DoesNotTouchTheModel_AfterFirstAwait()
    {
        // The provider never reads the model: editing it while the request is in flight must be harmless.
        await using Rig rig = await Rig.StartFakeAsync("foo", new FakeServerOptions { DelayMs = 100 });

        Task<CompletionList?> task = rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken);
        for (int i = 0; i < 20; i++)
        {
            rig.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "x")]);
        }

        Assert.NotNull(await task);
    }

    [Fact]
    public async Task Resolve_FillsDetailAndDocumentation()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo");
        CompletionItem item = (await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken))!.Suggestions.First(i => i.Label == "if");

        CompletionItem? resolved = await rig.Provider.ResolveCompletionItemAsync(item, TestContext.Current.CancellationToken);

        Assert.Equal("detail of if", resolved!.Detail);
        Assert.Equal("**if** documentation", resolved.Documentation!.Value);
    }

    [Fact]
    public async Task Resolve_Length_HasAdditionalTextEdit()
    {
        await using Rig rig = await Rig.StartFakeAsync("obj.");
        CompletionItem item = (await rig.CompleteAsync(new Position(1, 5), Dot, TestContext.Current.CancellationToken))!.Suggestions[0];
        Assert.Equal("Length", item.Label);

        CompletionItem? resolved = await rig.Provider.ResolveCompletionItemAsync(item, TestContext.Current.CancellationToken);

        TextEdit edit = Assert.Single(resolved!.AdditionalTextEdits!);
        Assert.Equal("// resolved\n", edit.Text);
        Assert.Equal(new Range(1, 1, 1, 1), edit.Range);
    }

    [Fact]
    public async Task Resolve_NoProviderData_Null()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo");

        Assert.Null(await rig.Provider.ResolveCompletionItemAsync(new CompletionItem("x", "x", CompletionItemKind.Text), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Resolve_ContentModified_Null()
    {
        ScriptedServer server = new ScriptedServer().On(LspMethods.CompletionResolve, _ => throw new JsonRpcException(JsonRpcErrorCodes.ContentModified, "modified"));
        await using Rig rig = await Rig.StartScriptedAsync(server);
        CompletionItem item = LspConvert.ToCompletionItem(new LspCompletionItem { Label = "a" });

        Assert.Null(await rig.Provider.ResolveCompletionItemAsync(item, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Resolve_RoundTripsData()
    {
        await using Rig rig = await Rig.StartFakeAsync("foo");
        CompletionItem item = (await rig.CompleteAsync(new Position(1, 1), Invoked, TestContext.Current.CancellationToken))!.Suggestions.First(i => i.Label == "if");

        CompletionItem? resolved = await rig.Provider.ResolveCompletionItemAsync(item, TestContext.Current.CancellationToken);

        LspCompletionItem raw = Assert.IsType<LspCompletionItem>(resolved!.ProviderData);
        Assert.Equal("""{"label":"if","n":1}""", raw.Data!.Value.GetRawText());
    }
}
