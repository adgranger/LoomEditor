using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.JsonRpc;

public class JsonRpcConnectionServerRequestTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ServerRequest_HandlerResult_SentAsResponse()
    {
        await using Rig rig = new();
        rig.Connection.RequestHandler = (method, p, _) => Task.FromResult<JsonElement?>(JsonElement.Parse($"\"{method}:{p!.Value.GetProperty("a").GetInt32()}\""));

        await rig.Peer.WriteAsync(TestPeer.Request("7", "ask", """{"a":4}"""));

        JsonRpcMessage response = await rig.Peer.ReadAsync();
        Assert.Equal(7, response.Id!.Value.GetInt64());
        Assert.Equal("ask:4", response.Result!.Value.GetString());
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ServerRequest_StringId_EchoedAsString()
    {
        await using Rig rig = new();
        rig.Connection.RequestHandler = (_, _, _) => Task.FromResult<JsonElement?>(JsonElement.Parse("1"));

        await rig.Peer.WriteAsync(TestPeer.Request("\"abc\"", "ask"));

        JsonRpcMessage response = await rig.Peer.ReadAsync();
        Assert.Equal(JsonValueKind.String, response.Id!.Value.ValueKind);
        Assert.Equal("abc", response.Id.Value.GetString());
    }

    [Fact]
    public async Task ServerRequest_NullResult()
    {
        await using Rig rig = new();
        rig.Connection.RequestHandler = (_, _, _) => Task.FromResult<JsonElement?>(null);

        await rig.Peer.WriteAsync(TestPeer.Request("1", "ask"));

        Assert.Equal("""{"jsonrpc":"2.0","id":1,"result":null}""", await rig.Peer.ReadRawAsync());
    }

    [Fact]
    public async Task ServerRequest_HandlerThrowsJsonRpcException_SentAsError()
    {
        await using Rig rig = new();
        rig.Connection.RequestHandler = (_, _, _) => throw new JsonRpcException(JsonRpcErrorCodes.InvalidParams, "bad", JsonElement.Parse("""{"x":1}"""));

        await rig.Peer.WriteAsync(TestPeer.Request("1", "ask"));

        JsonRpcMessage response = await rig.Peer.ReadAsync();
        Assert.Equal(JsonRpcErrorCodes.InvalidParams, response.Error!.Code);
        Assert.Equal("bad", response.Error.Message);
        Assert.Equal(1, response.Error.Data!.Value.GetProperty("x").GetInt32());
    }

    [Fact]
    public async Task ServerRequest_HandlerThrows_InternalError()
    {
        await using Rig rig = new();
        rig.Connection.RequestHandler = (_, _, _) => throw new InvalidOperationException("kaput");

        await rig.Peer.WriteAsync(TestPeer.Request("1", "ask"));

        JsonRpcMessage response = await rig.Peer.ReadAsync();
        Assert.Equal(JsonRpcErrorCodes.InternalError, response.Error!.Code);
        Assert.Equal("kaput", response.Error.Message);
    }

    [Fact]
    public async Task ServerRequest_NoHandler_MethodNotFound()
    {
        await using Rig rig = new();

        await rig.Peer.WriteAsync(TestPeer.Request("1", "workspace/unknown"));

        JsonRpcMessage response = await rig.Peer.ReadAsync();
        Assert.Equal(JsonRpcErrorCodes.MethodNotFound, response.Error!.Code);
        Assert.Equal("Method not found: workspace/unknown", response.Error.Message);
    }

    [Fact]
    public async Task ServerRequest_HandlerIsAsync_ReadLoopKeepsReading()
    {
        await using Rig rig = new();
        TaskCompletionSource<JsonElement?> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> notified = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.RequestHandler = (_, _, _) => gate.Task;
        rig.Connection.NotificationReceived += (method, _) => notified.TrySetResult(method);

        await rig.Peer.WriteAsync(TestPeer.Request("1", "slow"));
        await rig.Peer.WriteAsync(TestPeer.Notification("meanwhile"));

        Assert.Equal("meanwhile", await notified.Task.WaitAsync(Wait, Ct));
        gate.SetResult(JsonElement.Parse("true"));
        Assert.True((await rig.Peer.ReadAsync()).Result!.Value.GetBoolean());
    }

    [Fact]
    public async Task ServerRequest_CancelRequest_CancelsTheHandlerToken()
    {
        await using Rig rig = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.RequestHandler = async (_, _, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return null;
        };

        await rig.Peer.WriteAsync(TestPeer.Request("4", "slow"));
        await started.Task.WaitAsync(Wait, Ct);
        await rig.Peer.WriteAsync(TestPeer.Notification("$/cancelRequest", """{"id":4}"""));

        JsonRpcMessage response = await rig.Peer.ReadAsync();
        Assert.Equal(4, response.Id!.Value.GetInt64());
        Assert.NotNull(response.Error);
    }

    [Fact]
    public async Task ServerRequest_ConnectionClosed_CancelsTheHandlerToken()
    {
        await using Rig rig = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.RequestHandler = (_, _, token) =>
        {
            token.Register(() => cancelled.TrySetResult());
            started.SetResult();
            return new TaskCompletionSource<JsonElement?>().Task;
        };

        await rig.Peer.WriteAsync(TestPeer.Request("4", "slow"));
        await started.Task.WaitAsync(Wait, Ct);
        await rig.Peer.Output.DisposeAsync();

        await cancelled.Task.WaitAsync(Wait, Ct);
    }
}
