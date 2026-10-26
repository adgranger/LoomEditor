using System.Text;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.JsonRpc;

public class LspMessageWriterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class RecordingStream : MemoryStream
    {
        public int Flushes;
        public int Writes;

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Writes++;
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            Flushes++;
            return Task.CompletedTask;
        }
    }

    private static async Task<byte[]> WriteAsync(byte[] body)
    {
        using MemoryStream stream = new();
        await new LspMessageWriter(stream).WriteMessageAsync(body, Token);
        return stream.ToArray();
    }

    [Fact]
    public async Task WritesHeaderAndBody() =>
        Assert.Equal("Content-Length: 5\r\n\r\nhello", Encoding.UTF8.GetString(await WriteAsync("hello"u8.ToArray())));

    [Fact]
    public async Task MultiByteBody_LengthInBytes() =>
        Assert.Equal("Content-Length: 9\r\n\r\né€\U0001F600", Encoding.UTF8.GetString(await WriteAsync(Encoding.UTF8.GetBytes("é€\U0001F600"))));

    [Fact]
    public async Task EmptyBody() =>
        Assert.Equal("Content-Length: 0\r\n\r\n", Encoding.UTF8.GetString(await WriteAsync([])));

    [Fact]
    public async Task LargeBody_TwoWrites_SameBytes()
    {
        byte[] body = new byte[64 * 1024 + 1];
        Array.Fill(body, (byte)'a');
        RecordingStream stream = new();
        await new LspMessageWriter(stream).WriteMessageAsync(body, Token);

        Assert.Equal(2, stream.Writes);
        byte[] expected = [.. Encoding.ASCII.GetBytes("Content-Length: 65537\r\n\r\n"), .. body];
        Assert.Equal(expected, stream.ToArray());
    }

    [Fact]
    public async Task Flushes()
    {
        RecordingStream stream = new();
        await new LspMessageWriter(stream).WriteMessageAsync("{}"u8.ToArray(), Token);
        Assert.Equal(1, stream.Writes);
        Assert.Equal(1, stream.Flushes);
    }

    [Fact]
    public async Task WriterThenReader_RoundTrip()
    {
        using MemoryPipe pipe = new();
        LspMessageWriter writer = new(pipe.ClientOutput);
        LspMessageReader reader = new(pipe.ServerInput);
        Random random = new(42);
        for (int i = 0; i < 100; i++)
        {
            byte[] body = new byte[random.Next(0, 5000)];
            random.NextBytes(body);
            Task write = Task.Run(async () => await writer.WriteMessageAsync(body, Token), Token);
            Assert.Equal(body, await reader.ReadMessageAsync(Token));
            await write;
        }
    }

    [Fact]
    public async Task Cancelled_Throws()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        using MemoryStream stream = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await new LspMessageWriter(stream).WriteMessageAsync("{}"u8.ToArray(), cts.Token));
    }
}
