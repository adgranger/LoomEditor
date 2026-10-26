using System.Text;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.JsonRpc;

public class LspMessageReaderTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static LspMessageReader ReaderOf(string raw, int chunk = int.MaxValue, int max = 64 * 1024 * 1024) =>
        new(new ChunkedStream(Encoding.UTF8.GetBytes(raw), chunk), max);

    private static async Task<string?> ReadString(LspMessageReader reader)
    {
        byte[]? body = await reader.ReadMessageAsync(Token);
        return body is null ? null : Encoding.UTF8.GetString(body);
    }

    private static string Frame(string body) => $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";

    private static async Task AssertThrows(string raw, int max = 64 * 1024 * 1024)
    {
        await Assert.ThrowsAsync<LspProtocolException>(async () => await ReaderOf(raw, max: max).ReadMessageAsync(Token));
    }

    [Fact]
    public async Task ReadsOneMessage() => Assert.Equal("{}", await ReadString(ReaderOf(Frame("{}"))));

    [Fact]
    public async Task ReadsTwoMessagesBackToBack()
    {
        LspMessageReader reader = ReaderOf(Frame("{\"a\":1}") + Frame("{\"b\":2}"));
        Assert.Equal("{\"a\":1}", await ReadString(reader));
        Assert.Equal("{\"b\":2}", await ReadString(reader));
        Assert.Null(await ReadString(reader));
    }

    [Fact]
    public async Task EmptyStream_ReturnsNull() => Assert.Null(await ReadString(ReaderOf("")));

    [Fact]
    public async Task EofBetweenMessages_ReturnsNull()
    {
        LspMessageReader reader = ReaderOf(Frame("{}"));
        await ReadString(reader);
        Assert.Null(await ReadString(reader));
    }

    [Fact]
    public async Task OneByteChunks_StillParses() => Assert.Equal("{\"x\":true}", await ReadString(ReaderOf(Frame("{\"x\":true}"), 1)));

    [Fact]
    public async Task HeaderSplitAcrossReads() => Assert.Equal("{}", await ReadString(ReaderOf(Frame("{}"), 5)));

    [Fact]
    public async Task BodySplitAcrossReads() => Assert.Equal("{\"hello\":\"world\"}", await ReadString(ReaderOf(Frame("{\"hello\":\"world\"}"), 3)));

    [Fact]
    public async Task MultiByteBody_LengthIsInBytes()
    {
        string body = "{\"s\":\"é€\U0001F600\"}";
        Assert.Equal(body, await ReadString(ReaderOf(Frame(body), 4)));
    }

    [Fact]
    public async Task ContentType_Ignored() =>
        Assert.Equal("{}", await ReadString(ReaderOf("Content-Length: 2\r\nContent-Type: application/vscode-jsonrpc; charset=utf-8\r\n\r\n{}")));

    [Fact]
    public async Task UnknownHeader_Ignored() =>
        Assert.Equal("{}", await ReadString(ReaderOf("X-Foo: bar\r\nContent-Length: 2\r\n\r\n{}")));

    [Fact]
    public async Task HeaderName_CaseInsensitive() => Assert.Equal("{}", await ReadString(ReaderOf("content-length: 2\r\n\r\n{}")));

    [Fact]
    public async Task ValueWhitespace_Trimmed() => Assert.Equal("{}", await ReadString(ReaderOf("Content-Length: \t 2 \t\r\n\r\n{}")));

    [Fact]
    public async Task BareLf_Accepted() => Assert.Equal("{}", await ReadString(ReaderOf("Content-Length: 2\n\n{}")));

    [Fact]
    public async Task ZeroLength_ReturnsEmptyArray()
    {
        byte[]? body = await ReaderOf("Content-Length: 0\r\n\r\n").ReadMessageAsync(Token);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public Task MissingContentLength_Throws() => AssertThrows("Content-Type: x\r\n\r\n{}");

    [Fact]
    public Task DuplicateContentLength_Throws() => AssertThrows("Content-Length: 2\r\nContent-Length: 2\r\n\r\n{}");

    [Fact]
    public Task NonNumericLength_Throws() => AssertThrows("Content-Length: abc\r\n\r\n");

    [Fact]
    public Task NegativeLength_Throws() => AssertThrows("Content-Length: -1\r\n\r\n");

    [Fact]
    public Task LengthWithPlusSign_Throws() => AssertThrows("Content-Length: +2\r\n\r\n{}");

    [Fact]
    public Task LengthTooLarge_Throws() => AssertThrows("Content-Length: 11\r\n\r\n01234567890", max: 10);

    [Fact]
    public Task LineWithoutColon_Throws() => AssertThrows("Content-Length 2\r\n\r\n{}");

    [Fact]
    public Task HeaderLineTooLong_Throws() => AssertThrows("X: " + new string('a', 8190) + "\r\nContent-Length: 2\r\n\r\n{}");

    [Fact]
    public async Task HeaderLineAtLimit_Accepted() =>
        Assert.Equal("{}", await ReadString(ReaderOf("X: " + new string('a', 8189) + "\r\nContent-Length: 2\r\n\r\n{}")));

    [Fact]
    public Task TooManyHeaders_Throws() =>
        AssertThrows(string.Concat(Enumerable.Repeat("X: y\r\n", 32)) + "Content-Length: 2\r\n\r\n{}");

    [Fact]
    public Task EofInHeader_Throws() => AssertThrows("Content-Length: 2\r\n");

    [Fact]
    public Task EofInBody_Throws() => AssertThrows("Content-Length: 5\r\n\r\n{}");

    [Fact]
    public async Task Cancelled_ThrowsOperationCanceled()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await ReaderOf(Frame("{}")).ReadMessageAsync(cts.Token));
    }

    [Fact]
    public async Task LargeBody_1MiB()
    {
        string body = new('x', 1024 * 1024);
        Assert.Equal(body, await ReadString(ReaderOf(Frame(body), 4096)));
    }

    // After an LspProtocolException the reader is faulted: every later call throws again.
    [Fact]
    public async Task AfterThrow_ReaderIsNotReusable()
    {
        LspMessageReader reader = ReaderOf("Content-Length: abc\r\n\r\n" + Frame("{}"));
        await Assert.ThrowsAsync<LspProtocolException>(async () => await reader.ReadMessageAsync(Token));
        await Assert.ThrowsAsync<LspProtocolException>(async () => await reader.ReadMessageAsync(Token));
    }
}
