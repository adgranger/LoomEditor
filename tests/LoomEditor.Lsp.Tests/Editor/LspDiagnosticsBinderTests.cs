using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.Protocol;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspDiagnosticsBinderTests
{
    private const string Uri = "file:///t.txt";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private sealed class Rig : IAsyncDisposable
    {
        private readonly SemaphoreSlim _received = new(0);
        private readonly FakeServerHost? _host;
        private readonly ScriptedServer? _scripted;

        private Rig(LspClient client, TextModel model, FakeServerHost? host, ScriptedServer? scripted, string owner)
        {
            Client = client;
            Model = model;
            _host = host;
            _scripted = scripted;
            Binder = new LspDiagnosticsBinder(client, model, Markers, Uri, Post.Post, owner);
            Client.DiagnosticsReceived += _ => _received.Release();
        }

        public LspClient Client { get; }

        public TextModel Model { get; }

        public MarkerService Markers { get; } = new();

        public ManualPost Post { get; } = new();

        public LspDiagnosticsBinder Binder { get; }

        public LspDocumentSync? Sync { get; private set; }

        public static async Task<Rig> StartFakeAsync(string text, string owner = "lsp")
        {
            FakeServerHost host = FakeServerHost.StartInProcess();
            LspClient client = new(host.Connection, new LspClientOptions());
            await client.InitializeAsync(TestContext.Current.CancellationToken);
            Rig rig = new(client, new TextModel(text, DefaultEndOfLine.LF), host, null, owner);
            rig.Sync = new LspDocumentSync(client, rig.Model, Uri, "fake");
            rig.Sync.Open();
            return rig;
        }

        public static async Task<Rig> StartScriptedAsync(string text = "some text")
        {
            ScriptedServer server = new ScriptedServer().Start();
            LspClient client = new(server.Connection, new LspClientOptions());
            await client.InitializeAsync(TestContext.Current.CancellationToken);
            return new Rig(client, new TextModel(text, DefaultEndOfLine.LF), null, server, "lsp");
        }

        public async Task WaitForPublishAsync() => await _received.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        public async Task PublishAsync(string uri, int? version, string diagnosticsJson)
        {
            string v = version is { } n ? $"\"version\":{n}," : "";
            await _scripted!.SendNotificationAsync(LspMethods.PublishDiagnostics, $"{{\"uri\":\"{uri}\",{v}\"diagnostics\":{diagnosticsJson}}}");
            await WaitForPublishAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Sync?.Dispose();
            Binder.Dispose();
            await Client.DisposeAsync();
            if (_host is not null)
            {
                await _host.DisposeAsync();
            }

            if (_scripted is not null)
            {
                await _scripted.DisposeAsync();
            }
        }
    }

    private static string Diag(int line, int c1, int c2, int? severity = 1)
        => $"{{\"range\":{{\"start\":{{\"line\":{line},\"character\":{c1}}},\"end\":{{\"line\":{line},\"character\":{c2}}}}},\"severity\":{severity},\"message\":\"m\"}}";

    [Fact]
    public async Task Diagnostics_BecomeMarkers()
    {
        await using Rig rig = await Rig.StartFakeAsync("ERROR and WARN");
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();

        IReadOnlyList<Marker> markers = rig.Markers.Read("lsp");
        Assert.Equal(2, markers.Count);
        Assert.Equal(MarkerSeverity.Error, markers[0].Severity);
        Assert.Equal("FAKE001", markers[0].Code);
        Assert.Equal("fake", markers[0].Source);
        Assert.Equal((1, 1, 1, 6), (markers[0].StartLineNumber, markers[0].StartColumn, markers[0].EndLineNumber, markers[0].EndColumn));
        Assert.Equal(MarkerSeverity.Warning, markers[1].Severity);
        Assert.Equal("2", markers[1].Code);
        Assert.Equal(11, markers[1].StartColumn);
    }

    [Fact]
    public async Task Diagnostics_NotAppliedBeforeRunAll()
    {
        await using Rig rig = await Rig.StartFakeAsync("ERROR");
        await rig.WaitForPublishAsync();

        Assert.Empty(rig.Markers.Read("lsp"));
        Assert.Equal(1, rig.Post.PendingCount);
        rig.Post.RunAll();
        Assert.Single(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Diagnostics_OtherUri_Ignored()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        await rig.PublishAsync("file:///other.txt", null, $"[{Diag(0, 0, 4)}]");

        Assert.Equal(0, rig.Post.PendingCount);
        Assert.Empty(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Diagnostics_StaleVersion_Dropped()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        await rig.PublishAsync(Uri, rig.Model.VersionId - 1, $"[{Diag(0, 0, 4)}]");
        rig.Post.RunAll();

        Assert.Empty(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Diagnostics_NoVersion_Applied()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");
        rig.Post.RunAll();

        Assert.Single(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Diagnostics_Replaced_OnNextPublish()
    {
        await using Rig rig = await Rig.StartFakeAsync("ERROR and WARN");
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();
        Assert.Equal(2, rig.Markers.Read("lsp").Count);

        rig.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 7), "")]);
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();

        Assert.Single(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Diagnostics_EmptyList_ClearsMarkers()
    {
        await using Rig rig = await Rig.StartFakeAsync("ERROR");
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();
        Assert.Single(rig.Markers.Read("lsp"));

        rig.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 6), "fine")]);
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();

        Assert.Empty(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Diagnostics_Tags_Mapped()
    {
        await using Rig rig = await Rig.StartFakeAsync("UNUSED OLD");
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();

        IReadOnlyList<Marker> markers = rig.Markers.Read("lsp");
        Assert.Equal([MarkerTag.Unnecessary], markers[0].Tags);
        Assert.Equal([MarkerTag.Deprecated], markers[1].Tags);
    }

    [Fact]
    public async Task Diagnostics_RangeBeyondText_Clamped()
    {
        await using Rig rig = await Rig.StartScriptedAsync("abc");
        await rig.PublishAsync(Uri, null, $"[{Diag(50, 0, 99)}]");
        rig.Post.RunAll();

        Marker marker = Assert.Single(rig.Markers.Read("lsp"));
        Assert.Equal(1, marker.EndLineNumber);
        Assert.InRange(marker.EndColumn, 1, 4);
    }

    [Fact]
    public async Task PostRefused_Dropped()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        rig.Post.Accepting = false;
        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");

        Assert.Equal(0, rig.Post.PendingCount);
        Assert.Empty(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Handler_DoesNotTouchTheModel()
    {
        int handlerThread = 0;
        int applyThread = 0;
        await using Rig rig = await Rig.StartScriptedAsync();
        rig.Client.DiagnosticsReceived += _ => handlerThread = Environment.CurrentManagedThreadId;
        rig.Markers.MarkersChanged += (_, _) => applyThread = Environment.CurrentManagedThreadId;
        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");

        Assert.Equal(0, applyThread);
        rig.Post.RunAll();

        Assert.Equal(Environment.CurrentManagedThreadId, applyThread);
        Assert.NotEqual(Environment.CurrentManagedThreadId, handlerThread);
    }

    [Fact]
    public async Task Dispose_ClearsMarkers_AndUnsubscribes()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");
        rig.Post.RunAll();
        Assert.Single(rig.Markers.Read("lsp"));

        rig.Binder.Dispose();
        Assert.Empty(rig.Markers.Read("lsp"));

        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");
        Assert.Equal(0, rig.Post.PendingCount);
    }

    [Fact]
    public async Task Dispose_PendingPostedAction_DoesNothing()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");
        rig.Binder.Dispose();
        rig.Post.RunAll();

        Assert.Empty(rig.Markers.Read("lsp"));
    }

    [Fact]
    public async Task Dispose_Twice_Ok()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        rig.Binder.Dispose();
        rig.Binder.Dispose();
    }

    [Fact]
    public async Task Clear_RemovesTheOwnersMarkers_OnlyTheOwners()
    {
        await using Rig rig = await Rig.StartScriptedAsync();
        rig.Markers.Change("other", [new MarkerData(MarkerSeverity.Info, "x", 1, 1, 1, 2)]);
        await rig.PublishAsync(Uri, null, $"[{Diag(0, 0, 4)}]");
        rig.Post.RunAll();

        rig.Binder.Clear();

        Assert.Empty(rig.Markers.Read("lsp"));
        Assert.Single(rig.Markers.Read("other"));
    }

    [Fact]
    public async Task CustomOwner_Used()
    {
        await using Rig rig = await Rig.StartFakeAsync("ERROR", owner: "mine");
        await rig.WaitForPublishAsync();
        rig.Post.RunAll();

        Assert.Single(rig.Markers.Read("mine"));
        Assert.Empty(rig.Markers.Read("lsp"));
    }
}
