using System.IO.Pipes;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>Two connected pairs of streams (client side and server side) built on anonymous pipes.</summary>
/// <remarks>Threading: each stream follows the usual rule: one reader and one writer at a time.</remarks>
internal sealed class MemoryPipe : IDisposable
{
    private readonly AnonymousPipeServerStream _clientToServerWriter;
    private readonly AnonymousPipeClientStream _clientToServerReader;
    private readonly AnonymousPipeServerStream _serverToClientWriter;
    private readonly AnonymousPipeClientStream _serverToClientReader;

    public MemoryPipe()
    {
        _clientToServerWriter = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
        _clientToServerReader = new AnonymousPipeClientStream(PipeDirection.In, _clientToServerWriter.ClientSafePipeHandle);
        _serverToClientWriter = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
        _serverToClientReader = new AnonymousPipeClientStream(PipeDirection.In, _serverToClientWriter.ClientSafePipeHandle);
    }

    /// <summary>What the client reads (what the server writes).</summary>
    public Stream ClientInput => _serverToClientReader;

    /// <summary>What the client writes.</summary>
    public Stream ClientOutput => _clientToServerWriter;

    /// <summary>What the server reads (what the client writes).</summary>
    public Stream ServerInput => _clientToServerReader;

    /// <summary>What the server writes.</summary>
    public Stream ServerOutput => _serverToClientWriter;

    public void Dispose()
    {
        _clientToServerWriter.Dispose();
        _clientToServerReader.Dispose();
        _serverToClientWriter.Dispose();
        _serverToClientReader.Dispose();
    }
}
