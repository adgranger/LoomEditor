namespace LoomEditor.Lsp.Tests.TestHelpers;

public class MemoryPipeTests
{
    private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        await stream.ReadExactlyAsync(buffer, TestContext.Current.CancellationToken);
        return buffer;
    }

    private static async Task<int> ReadOrZeroAsync(Stream stream)
    {
        try
        {
            return await stream.ReadAsync(new byte[8], TestContext.Current.CancellationToken);
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }

    [Fact]
    public async Task ClientWrite_ServerReads()
    {
        using MemoryPipe pipe = new();
        await pipe.ClientOutput.WriteAsync(new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);
        await pipe.ClientOutput.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 1, 2, 3 }, await ReadExactlyAsync(pipe.ServerInput, 3));
    }

    [Fact]
    public async Task ServerWrite_ClientReads()
    {
        using MemoryPipe pipe = new();
        await pipe.ServerOutput.WriteAsync(new byte[] { 4, 5 }, TestContext.Current.CancellationToken);
        await pipe.ServerOutput.FlushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 4, 5 }, await ReadExactlyAsync(pipe.ClientInput, 2));
    }

    [Fact]
    public async Task CloseClientOutput_ServerReadsZero()
    {
        using MemoryPipe pipe = new();
        await pipe.ClientOutput.DisposeAsync();

        Assert.Equal(0, await pipe.ServerInput.ReadAsync(new byte[8], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposeCompletesReaders()
    {
        MemoryPipe pipe = new();
        Stream serverInput = pipe.ServerInput;
        Stream clientInput = pipe.ClientInput;
        pipe.Dispose();

        Assert.Equal(0, await ReadOrZeroAsync(serverInput));
        Assert.Equal(0, await ReadOrZeroAsync(clientInput));
    }

    [Fact]
    public async Task LargeWrite_IsReadInChunks()
    {
        const int Size = 1024 * 1024;
        using MemoryPipe pipe = new();
        byte[] data = new byte[Size];
        new Random(42).NextBytes(data);

        Task writer = Task.Run(async () =>
        {
            await pipe.ClientOutput.WriteAsync(data, TestContext.Current.CancellationToken);
            await pipe.ClientOutput.FlushAsync(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        byte[] received = new byte[Size];
        int total = 0;
        while (total < Size)
        {
            int read = await pipe.ServerInput.ReadAsync(received.AsMemory(total, Math.Min(4096, Size - total)), TestContext.Current.CancellationToken);
            Assert.True(read > 0, "unexpected end of stream (seed 42)");
            total += read;
        }

        await writer;
        Assert.Equal(data, received);
    }
}
