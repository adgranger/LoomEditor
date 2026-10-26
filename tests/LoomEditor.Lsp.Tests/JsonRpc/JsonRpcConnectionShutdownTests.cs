using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.JsonRpc;

public class JsonRpcConnectionShutdownTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class ThrowingWriteStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new IOException("write failed");

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("write failed");
    }

    private static List<Exception?> TrackClosed(JsonRpcConnection connection)
    {
        List<Exception?> raised = [];
        connection.Closed += e =>
        {
            lock (raised)
            {
                raised.Add(e);
            }
        };
        return raised;
    }

    [Fact]
    public async Task PeerClosesStream_ClosedRaisedOnceWithNull_PendingRequestsFault()
    {
        await using Rig rig = new();
        List<Exception?> closed = TrackClosed(rig.Connection);
        Task<System.Text.Json.JsonElement?> pending = rig.Connection.SendRequestAsync("m", null, Ct);
        await rig.Peer.ReadAsync();

        await rig.Peer.Output.DisposeAsync();

        await Assert.ThrowsAsync<JsonRpcConnectionClosedException>(() => pending.WaitAsync(Wait, Ct));
        await rig.Connection.Completion.WaitAsync(Wait, Ct);
        await rig.Connection.DisposeAsync();
        lock (closed)
        {
            Assert.Equal([null], closed);
        }
    }

    [Fact]
    public async Task FramingError_ClosedWithProtocolException_PendingFault()
    {
        await using Rig rig = new();
        List<Exception?> closed = TrackClosed(rig.Connection);
        Task<System.Text.Json.JsonElement?> pending = rig.Connection.SendRequestAsync("m", null, Ct);
        await rig.Peer.ReadAsync();

        await rig.Peer.WriteBytesAsync("garbage\r\n\r\n");

        JsonRpcConnectionClosedException e = await Assert.ThrowsAsync<JsonRpcConnectionClosedException>(() => pending.WaitAsync(Wait, Ct));
        Assert.IsType<LspProtocolException>(e.InnerException);
        await rig.Connection.Completion.WaitAsync(Wait, Ct);
        lock (closed)
        {
            Assert.IsType<LspProtocolException>(Assert.Single(closed));
        }
    }

    [Fact]
    public async Task Completion_CompletesOnClose()
    {
        await using Rig rig = new();
        Assert.False(rig.Connection.Completion.IsCompleted);

        await rig.Peer.Output.DisposeAsync();

        await rig.Connection.Completion.WaitAsync(Wait, Ct);
    }

    [Fact]
    public async Task SendRequest_AfterClose_Throws()
    {
        await using Rig rig = new();
        await rig.Peer.Output.DisposeAsync();
        await rig.Connection.Completion.WaitAsync(Wait, Ct);

        await Assert.ThrowsAsync<JsonRpcConnectionClosedException>(() => rig.Connection.SendRequestAsync("m", null, Ct));
    }

    [Fact]
    public async Task SendNotification_AfterClose_Throws()
    {
        await using Rig rig = new();
        await rig.Peer.Output.DisposeAsync();
        await rig.Connection.Completion.WaitAsync(Wait, Ct);

        Assert.Throws<JsonRpcConnectionClosedException>(() => rig.Connection.SendNotification("n"));
    }

    [Fact]
    public async Task Dispose_DrainsQueuedNotifications()
    {
        await using Rig rig = new();
        for (int i = 0; i < 10; i++)
        {
            rig.Connection.SendNotification("n");
        }

        Task dispose = rig.Connection.DisposeAsync().AsTask();
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal("n", (await rig.Peer.ReadAsync()).Method);
        }

        await dispose.WaitAsync(Wait, Ct);
    }

    [Fact]
    public async Task Dispose_PeerNeverCloses_DoesNotHang()
    {
        Rig rig = new();
        List<Exception?> closed = TrackClosed(rig.Connection);

        await rig.Connection.DisposeAsync().AsTask().WaitAsync(Wait, Ct);

        lock (closed)
        {
            Assert.Equal([null], closed);
        }

        rig.Pipe.Dispose();
    }

    [Fact]
    public async Task Dispose_Twice_Ok()
    {
        await using Rig rig = new();

        await rig.Connection.DisposeAsync();
        await rig.Connection.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_FaultsPendingRequests()
    {
        await using Rig rig = new();
        Task<System.Text.Json.JsonElement?> pending = rig.Connection.SendRequestAsync("m", null, Ct);

        await rig.Connection.DisposeAsync();

        await Assert.ThrowsAsync<JsonRpcConnectionClosedException>(() => pending.WaitAsync(Wait, Ct));
    }

    [Fact]
    public async Task Start_Twice_Throws()
    {
        await using Rig rig = new();

        Assert.Throws<InvalidOperationException>(() => rig.Connection.Start());
    }

    [Fact]
    public async Task WriteFailure_ClosesTheConnection()
    {
        using MemoryPipe pipe = new();
        await using JsonRpcConnection connection = new(pipe.ClientInput, new ThrowingWriteStream());
        List<Exception?> closed = TrackClosed(connection);
        connection.Start();

        connection.SendNotification("n");

        await connection.Completion.WaitAsync(Wait, Ct);
        lock (closed)
        {
            Assert.IsType<IOException>(Assert.Single(closed));
        }

        Assert.Throws<JsonRpcConnectionClosedException>(() => connection.SendNotification("n"));
    }
}
