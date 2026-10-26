using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>The other end of a <see cref="MemoryPipe"/>: reads what the connection sends and writes what a server would.</summary>
/// <remarks>Threading: one reader and one writer at a time, like the streams it wraps.</remarks>
internal sealed class TestPeer(Stream serverInput, Stream serverOutput)
{
    private readonly LspMessageReader _reader = new(serverInput);
    private readonly LspMessageWriter _writer = new(serverOutput);

    public Stream Output => serverOutput;

    public static JsonRpcMessage Response(long id, string resultJson)
        => new() { Id = JsonElement.Parse(id.ToString(System.Globalization.CultureInfo.InvariantCulture)), Result = JsonElement.Parse(resultJson) };

    public static JsonRpcMessage Error(JsonElement id, int code, string message, string? dataJson = null)
        => new() { Id = id, Error = new JsonRpcError { Code = code, Message = message, Data = dataJson is null ? null : JsonElement.Parse(dataJson) } };

    public static JsonRpcMessage Request(string idJson, string method, string? paramsJson = null)
        => new() { Id = JsonElement.Parse(idJson), Method = method, Params = paramsJson is null ? null : JsonElement.Parse(paramsJson) };

    public static JsonRpcMessage Notification(string method, string? paramsJson = null)
        => new() { Method = method, Params = paramsJson is null ? null : JsonElement.Parse(paramsJson) };

    /// <summary>Reads the next message of the connection; throws <see cref="TimeoutException"/> after 5 seconds.</summary>
    public async Task<JsonRpcMessage> ReadAsync()
    {
        byte[]? body = await _reader.ReadMessageAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return JsonSerializer.Deserialize(body, LspJsonContext.Default.JsonRpcMessage)!;
    }

    /// <summary>Reads the next message body as text.</summary>
    public async Task<string> ReadRawAsync()
    {
        byte[]? body = await _reader.ReadMessageAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        return Encoding.UTF8.GetString(body!);
    }

    public Task WriteAsync(JsonRpcMessage message)
        => _writer.WriteMessageAsync(JsonSerializer.SerializeToUtf8Bytes(message, LspJsonContext.Default.JsonRpcMessage), CancellationToken.None).AsTask();

    public Task WriteRawAsync(string json) => _writer.WriteMessageAsync(Encoding.UTF8.GetBytes(json), CancellationToken.None).AsTask();

    public async Task WriteBytesAsync(string text)
    {
        await serverOutput.WriteAsync(Encoding.ASCII.GetBytes(text), CancellationToken.None);
        await serverOutput.FlushAsync(CancellationToken.None);
    }
}

/// <summary>A started connection on a pipe, with its peer.</summary>
/// <remarks>Threading: test use only.</remarks>
internal sealed class Rig : IAsyncDisposable
{
    public Rig(bool start = true)
    {
        Pipe = new MemoryPipe();
        Connection = new JsonRpcConnection(Pipe.ClientInput, Pipe.ClientOutput);
        Peer = new TestPeer(Pipe.ServerInput, Pipe.ServerOutput);
        if (start)
        {
            Connection.Start();
        }
    }

    public MemoryPipe Pipe { get; }

    public JsonRpcConnection Connection { get; }

    public TestPeer Peer { get; }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        Pipe.Dispose();
    }
}

internal sealed record TestParams(int N);

internal sealed record TestReply(string Value);

[JsonSerializable(typeof(TestParams))]
[JsonSerializable(typeof(TestReply))]
[JsonSerializable(typeof(string))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class TestJson : JsonSerializerContext;
