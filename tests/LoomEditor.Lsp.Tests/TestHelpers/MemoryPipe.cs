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

    // Anonymous pipes have no real asynchronous I/O on Windows: every pending read holds a thread pool thread. With many connection tests
    // at once the pool would add threads one at a time and the tests would time out, so the pool gets its threads at once.
    static MemoryPipe() => ThreadPool.SetMinThreads(256, 256);

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
