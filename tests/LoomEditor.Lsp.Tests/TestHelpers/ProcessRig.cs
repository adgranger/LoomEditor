using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.FakeServer;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>An <see cref="LspClient"/> on a fake server child process; disposing it asserts that the process is gone.</summary>
/// <remarks>Threading: test use only.</remarks>
internal sealed class ProcessRig : IAsyncDisposable
{
    public ProcessRig(FakeServerOptions? server = null, LspClientOptions? options = null, string[]? extraArgs = null)
    {
        Client = LspClient.StartProcess(FakeServerLocator.GetCommand(server, extraArgs), options ?? new LspClientOptions());
    }

    public LspClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        LspServerProcess process = Client.Process!;
        await Client.DisposeAsync();
        Assert.True(process.HasExited, "The fake server process is still running");
    }
}
