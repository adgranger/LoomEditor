using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspDocumentSyncTests
{
    private const string Uri = "file:///t.txt";

    internal sealed class Rig(FakeServerHost host, LspClient client, TextModel model, LspDocumentSync sync) : IAsyncDisposable
    {
        public FakeServerHost Host { get; } = host;

        public LspClient Client { get; } = client;

        public TextModel Model { get; } = model;

        public LspDocumentSync Sync { get; } = sync;

        public string ServerText => Host.Server!.DocumentText;

        public static async Task<Rig> StartAsync(string text, FakeServerOptions? options = null, bool open = true, DefaultEndOfLine eol = DefaultEndOfLine.LF, bool process = false)
        {
            FakeServerHost host = process ? FakeServerHost.StartProcess(options) : FakeServerHost.StartInProcess(options);
            LspClient client = new(host.Connection, new LspClientOptions());
            await client.InitializeAsync(TestContext.Current.CancellationToken);
            TextModel model = new(text, eol);
            LspDocumentSync sync = new(client, model, Uri, "fake");
            if (open)
            {
                sync.Open();
            }

            return new Rig(host, client, model, sync);
        }

        public async ValueTask DisposeAsync()
        {
            Sync.Dispose();
            await Client.DisposeAsync();
            await Host.DisposeAsync();
        }
    }

    private static TextEdit Replace(int l1, int c1, int l2, int c2, string text) => new(new Range(l1, c1, l2, c2), text);

    private static async Task AssertSyncedAsync(Rig rig)
    {
        await rig.Host.BarrierAsync();
        Assert.Equal(rig.Model.GetValue(), rig.ServerText);
    }

    [Fact]
    public async Task Open_SendsFullTextAndVersion()
    {
        await using Rig rig = await Rig.StartAsync("hello\nworld");
        await rig.Host.BarrierAsync();

        Assert.Equal("hello\nworld", rig.ServerText);
        Assert.Equal(Uri, rig.Host.Server!.DocumentUri);
        Assert.Equal(rig.Model.VersionId, rig.Host.Server.DocumentVersion);
        Assert.True(rig.Sync.IsOpen);
    }

    [Fact]
    public async Task Open_WithCrLfModel_ServerTextHasCrLf()
    {
        await using Rig rig = await Rig.StartAsync("a\r\nb\r\nc");
        await rig.Host.BarrierAsync();

        Assert.Equal("a\r\nb\r\nc", rig.ServerText);
    }

    [Fact]
    public async Task Open_TwiceIsNoOp()
    {
        await using Rig rig = await Rig.StartAsync("x");
        rig.Sync.Open();
        await rig.Host.BarrierAsync();

        Assert.Single(rig.Host.Server!.ReceivedMethods, "textDocument/didOpen");
    }

    [Fact]
    public async Task Open_BeforeRunning_Throws()
    {
        await using FakeServerHost host = FakeServerHost.StartInProcess();
        await using LspClient client = new(host.Connection, new LspClientOptions());
        using LspDocumentSync sync = new(client, new TextModel("x"), Uri, "fake");

        Assert.Throws<InvalidOperationException>(sync.Open);
    }

    [Fact]
    public async Task Open_ServerWithoutSync_SendsNothing()
    {
        await using ScriptedServer server = new ScriptedServer().WithCapabilities("{}").Start();
        await using LspClient client = new(server.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);
        using LspDocumentSync sync = new(client, new TextModel("x"), Uri, "fake");

        sync.Open();

        Assert.False(sync.IsOpen);
        Assert.DoesNotContain(server.Notifications, n => n.Method == "textDocument/didOpen");
    }

    [Fact]
    public async Task Edit_Insert_Incremental()
    {
        await using Rig rig = await Rig.StartAsync("hello world");
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 6), ",")]);

        await AssertSyncedAsync(rig);
        Assert.Equal("hello, world", rig.ServerText);
    }

    [Fact]
    public async Task Edit_Delete_Incremental()
    {
        await using Rig rig = await Rig.StartAsync("hello world");
        rig.Model.ApplyEdits([Replace(1, 6, 1, 7, "")]);

        await AssertSyncedAsync(rig);
        Assert.Equal("helloworld", rig.ServerText);
    }

    [Fact]
    public async Task Edit_Replace_Incremental()
    {
        await using Rig rig = await Rig.StartAsync("hello world");
        rig.Model.ApplyEdits([Replace(1, 7, 1, 12, "there")]);

        await AssertSyncedAsync(rig);
        Assert.Equal("hello there", rig.ServerText);
    }

    [Fact]
    public async Task Edit_MultiLine_Incremental()
    {
        await using Rig rig = await Rig.StartAsync("one\ntwo\nthree");
        rig.Model.ApplyEdits([Replace(1, 2, 3, 3, "X\nY\nZ")]);

        await AssertSyncedAsync(rig);
        Assert.Equal("oX\nY\nZree", rig.ServerText);
    }

    [Fact]
    public async Task Edit_MultipleRangesInOneEdit_ChangesInOrder()
    {
        await using Rig rig = await Rig.StartAsync("aaa\nbbb\nccc");
        rig.Model.ApplyEdits([Replace(1, 1, 1, 2, "1\n2"), Replace(2, 2, 2, 3, ""), TextEdit.Insert(new Position(3, 4), "!")]);

        await AssertSyncedAsync(rig);
    }

    [Fact]
    public async Task Edit_Undo_Redo_Synced()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        rig.Model.PushEditOperations(null, [TextEdit.Insert(new Position(1, 2), "XY")], null);
        await AssertSyncedAsync(rig);

        rig.Model.Undo();
        await AssertSyncedAsync(rig);
        Assert.Equal("abc", rig.ServerText);

        rig.Model.Redo();
        await AssertSyncedAsync(rig);
        Assert.Equal("aXYbc", rig.ServerText);
    }

    [Fact]
    public async Task Edit_AfterSetValue_FullText()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        rig.Model.SetValue("completely\nnew");

        await AssertSyncedAsync(rig);
        Assert.Equal("completely\nnew", rig.ServerText);
    }

    [Fact]
    public async Task Edit_AfterSetEol_FullText()
    {
        await using Rig rig = await Rig.StartAsync("a\nb\nc");
        rig.Model.SetEol(EndOfLineSequence.CRLF);

        await AssertSyncedAsync(rig);
        Assert.Equal("a\r\nb\r\nc", rig.ServerText);
    }

    [Fact]
    public async Task Edit_ServerWantsFull_SendsFullText()
    {
        await using Rig rig = await Rig.StartAsync("abc", new FakeServerOptions { Sync = "full" });
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 2), "Q")]);

        await AssertSyncedAsync(rig);
        Assert.Equal("aQbc", rig.ServerText);
    }

    [Fact]
    public async Task Edit_ServerWantsNone_NothingSent_ButOpenClose()
    {
        await using Rig rig = await Rig.StartAsync("abc", new FakeServerOptions { Sync = "none" });
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 2), "Q")]);
        await rig.Host.BarrierAsync();

        Assert.Equal("abc", rig.ServerText);
        Assert.Equal(rig.Model.VersionId, rig.Sync.SentVersion);
    }

    [Fact]
    public async Task Edit_MultiByteCharacters_Utf16Positions()
    {
        await using Rig rig = await Rig.StartAsync("é€😀x");
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 5), "[")]);
        rig.Model.ApplyEdits([Replace(1, 1, 1, 2, "E")]);

        await AssertSyncedAsync(rig);
        Assert.Equal("E€😀[x", rig.ServerText);
    }

    [Fact]
    public async Task Edit_VersionsIncrease_AndMatchTheModel()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        int last = rig.Model.VersionId;
        for (int i = 0; i < 3; i++)
        {
            rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);
            Assert.True(rig.Model.VersionId > last);
            last = rig.Model.VersionId;
            await rig.Host.BarrierAsync();
            Assert.Equal(last, rig.Host.Server!.DocumentVersion);
            Assert.Equal(last, rig.Sync.SentVersion);
        }
    }

    [Fact]
    public async Task Edit_BeforeOpen_Ignored()
    {
        await using Rig rig = await Rig.StartAsync("abc", open: false);
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);
        await rig.Host.BarrierAsync();

        Assert.Null(rig.Host.Server!.DocumentUri);
    }

    [Fact]
    public async Task Edit_AfterClose_Ignored()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        rig.Sync.Close();
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);
        await rig.Host.BarrierAsync();

        Assert.Null(rig.Host.Server!.DocumentUri);
        Assert.DoesNotContain("didChange(ignored)", rig.Host.Server.ReceivedMethods);
    }

    [Fact]
    public async Task Close_SendsDidClose()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        rig.Sync.Close();
        await rig.Host.BarrierAsync();

        Assert.Null(rig.Host.Server!.DocumentUri);
        Assert.False(rig.Sync.IsOpen);
    }

    [Fact]
    public async Task Dispose_Closes()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        rig.Sync.Dispose();
        await rig.Host.BarrierAsync();

        Assert.Null(rig.Host.Server!.DocumentUri);
    }

    [Fact]
    public async Task Dispose_Twice_Ok()
    {
        await using Rig rig = await Rig.StartAsync("abc");
        rig.Sync.Dispose();
        rig.Sync.Dispose();
        await rig.Host.BarrierAsync();

        Assert.Single(rig.Host.Server!.ReceivedMethods, "textDocument/didClose");
    }

    [Fact]
    public async Task ClientFaulted_StopsSending_IsFaulted()
    {
        await using Rig rig = await Rig.StartAsync("abc", new FakeServerOptions { ExitOn = "textDocument/didChange" }, process: true);
        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "x")]);
        DateTime end = DateTime.UtcNow.AddSeconds(10);
        while (rig.Client.State == LspClientState.Running && DateTime.UtcNow < end)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        rig.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "y")]);

        Assert.True(rig.Sync.IsFaulted);
    }

    [Fact]
    public async Task Edit_OrderBeforeRequest()
    {
        await using Rig rig = await Rig.StartAsync("foo bar");
        rig.Model.ApplyEdits([Replace(1, 1, 1, 4, "baz")]);

        System.Text.Json.JsonElement? hover = await rig.Client.RequestRawAsync(
            "textDocument/hover", System.Text.Json.JsonElement.Parse(FakeServerHost.At(0, 1)), TestContext.Current.CancellationToken);

        Assert.Contains("baz", hover!.Value.GetRawText());
    }
}
