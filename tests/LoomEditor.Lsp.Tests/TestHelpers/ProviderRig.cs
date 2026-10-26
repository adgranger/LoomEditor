using LoomEditor.Core.Model;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>An initialized client on the fake or a scripted server, with an opened document when the fake is used.</summary>
internal sealed class ProviderRig : IAsyncDisposable
{
    public const string Uri = "file:///t.txt";

    private readonly FakeServerHost? _host;
    private readonly ScriptedServer? _scripted;
    private readonly LspDocumentSync? _sync;

    private ProviderRig(FakeServerHost? host, ScriptedServer? scripted, LspClient client, TextModel model, LspDocumentSync? sync)
    {
        _host = host;
        _scripted = scripted;
        Client = client;
        Model = model;
        _sync = sync;
    }

    public LspClient Client { get; }

    public TextModel Model { get; }

    public FakeServerHost Host => _host!;

    public static async Task<ProviderRig> StartFakeAsync(string text, FakeServerOptions? options = null)
    {
        FakeServerHost host = FakeServerHost.StartInProcess(options);
        LspClient client = new(host.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);
        TextModel model = new(text, DefaultEndOfLine.LF);
        LspDocumentSync sync = new(client, model, Uri, "fake");
        sync.Open();
        await host.BarrierAsync();
        return new ProviderRig(host, null, client, model, sync);
    }

    public static async Task<ProviderRig> StartScriptedAsync(ScriptedServer server)
    {
        server.Start();
        LspClient client = new(server.Connection, new LspClientOptions());
        await client.InitializeAsync(TestContext.Current.CancellationToken);
        return new ProviderRig(null, server, client, new TextModel("foo", DefaultEndOfLine.LF), null);
    }

    public async ValueTask DisposeAsync()
    {
        _sync?.Dispose();
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
