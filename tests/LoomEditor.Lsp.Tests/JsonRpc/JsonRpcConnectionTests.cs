using System.Text.Json;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Tests.TestHelpers;

namespace LoomEditor.Lsp.Tests.JsonRpc;

public class JsonRpcConnectionTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static long IdOf(JsonRpcMessage message) => message.Id!.Value.GetInt64();

    private static async Task Pong(Rig rig)
    {
        // A request that proves the loops are alive after something odd was ignored.
        Task<JsonElement?> task = rig.Connection.SendRequestAsync("ping", null, Ct);
        JsonRpcMessage request = await rig.Peer.ReadAsync();
        await rig.Peer.WriteAsync(TestPeer.Response(IdOf(request), "1"));
        Assert.Equal(1, (await task.WaitAsync(Wait, Ct))!.Value.GetInt32());
    }

    [Fact]
    public async Task Request_GetsTypedResult()
    {
        await using Rig rig = new();
        Task<TestReply?> task = rig.Connection.SendRequestAsync("m", new TestParams(3), TestJson.Default.TestParams, TestJson.Default.TestReply, Ct);

        JsonRpcMessage request = await rig.Peer.ReadAsync();
        Assert.Equal("m", request.Method);
        Assert.Equal("2.0", request.JsonRpc);
        Assert.Equal(3, request.Params!.Value.GetProperty("n").GetInt32());
        await rig.Peer.WriteAsync(TestPeer.Response(IdOf(request), """{"value":"x"}"""));

        Assert.Equal(new TestReply("x"), await task.WaitAsync(Wait, Ct));
    }

    [Fact]
    public async Task Request_NullResult_ReturnsNull()
    {
        await using Rig rig = new();
        Task<TestReply?> task = rig.Connection.SendRequestAsync("m", new TestParams(1), TestJson.Default.TestParams, TestJson.Default.TestReply, Ct);

        JsonRpcMessage request = await rig.Peer.ReadAsync();
        await rig.Peer.WriteRawAsync($$"""{"jsonrpc":"2.0","id":{{IdOf(request)}},"result":null}""");

        Assert.Null(await task.WaitAsync(Wait, Ct));
    }

    [Fact]
    public async Task Request_ErrorResponse_FaultsWithJsonRpcException()
    {
        await using Rig rig = new();
        Task<JsonElement?> task = rig.Connection.SendRequestAsync("m", null, Ct);

        JsonRpcMessage request = await rig.Peer.ReadAsync();
        await rig.Peer.WriteAsync(TestPeer.Error(request.Id!.Value, JsonRpcErrorCodes.ContentModified, "changed", """{"why":1}"""));

        JsonRpcException e = await Assert.ThrowsAsync<JsonRpcException>(() => task.WaitAsync(Wait, Ct));
        Assert.Equal(JsonRpcErrorCodes.ContentModified, e.Code);
        Assert.Equal("changed", e.Message);
        Assert.Equal(1, e.ErrorData!.Value.GetProperty("why").GetInt32());
    }

    [Fact]
    public async Task Request_IdsStartAtOneAndIncrease()
    {
        await using Rig rig = new();
        _ = rig.Connection.SendRequestAsync("a", null, Ct);
        _ = rig.Connection.SendRequestAsync("b", null, Ct);

        Assert.Equal(1, IdOf(await rig.Peer.ReadAsync()));
        Assert.Equal(2, IdOf(await rig.Peer.ReadAsync()));
    }

    [Fact]
    public async Task Requests_ConcurrentAndAnsweredOutOfOrder()
    {
        await using Rig rig = new();
        Task<JsonElement?>[] tasks = Enumerable.Range(0, 50)
            .Select(i => rig.Connection.SendRequestAsync("m", JsonElement.Parse($$"""{"i":{{i}}}"""), Ct))
            .ToArray();

        List<JsonRpcMessage> requests = [];
        for (int i = 0; i < 50; i++)
        {
            requests.Add(await rig.Peer.ReadAsync());
        }

        for (int i = requests.Count - 1; i >= 0; i--)
        {
            await rig.Peer.WriteAsync(TestPeer.Response(IdOf(requests[i]), requests[i].Params!.Value.GetProperty("i").GetRawText()));
        }

        for (int i = 0; i < 50; i++)
        {
            JsonElement? result = await tasks[i].WaitAsync(Wait, Ct);
            Assert.Equal(requests.First(r => IdOf(r) == IdOf(requests[i])).Params!.Value.GetProperty("i").GetInt32(), result!.Value.GetInt32());
        }
    }

    [Fact]
    public async Task Request_Cancelled_SendsCancelRequestAndCancelsTask()
    {
        await using Rig rig = new();
        using CancellationTokenSource cts = new();
        Task<JsonElement?> task = rig.Connection.SendRequestAsync("m", null, cts.Token);
        JsonRpcMessage request = await rig.Peer.ReadAsync();

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Wait, Ct));
        JsonRpcMessage cancel = await rig.Peer.ReadAsync();
        Assert.Equal("$/cancelRequest", cancel.Method);
        Assert.Null(cancel.Id);
        Assert.Equal(IdOf(request), cancel.Params!.Value.GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Request_TokenAlreadyCancelled_SendsNothing()
    {
        await using Rig rig = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rig.Connection.SendRequestAsync("m", null, cts.Token));
        rig.Connection.SendNotification("next");

        Assert.Equal("next", (await rig.Peer.ReadAsync()).Method);
    }

    [Fact]
    public async Task Request_AnswerAfterCancel_Ignored()
    {
        await using Rig rig = new();
        using CancellationTokenSource cts = new();
        Task<JsonElement?> task = rig.Connection.SendRequestAsync("m", null, cts.Token);
        JsonRpcMessage request = await rig.Peer.ReadAsync();
        await cts.CancelAsync();
        await rig.Peer.ReadAsync();

        await rig.Peer.WriteAsync(TestPeer.Response(IdOf(request), "5"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Wait, Ct));
        await Pong(rig);
    }

    [Fact]
    public async Task Response_UnknownId_Ignored()
    {
        await using Rig rig = new();
        await rig.Peer.WriteAsync(TestPeer.Response(99, "1"));

        await Pong(rig);
    }

    [Fact]
    public async Task Response_StringId_Ignored()
    {
        await using Rig rig = new();
        Task<JsonElement?> task = rig.Connection.SendRequestAsync("m", null, Ct);
        JsonRpcMessage request = await rig.Peer.ReadAsync();

        await rig.Peer.WriteRawAsync("""{"jsonrpc":"2.0","id":"1","result":7}""");
        await rig.Peer.WriteAsync(TestPeer.Response(IdOf(request), "8"));

        Assert.Equal(8, (await task.WaitAsync(Wait, Ct))!.Value.GetInt32());
    }

    [Fact]
    public async Task Notification_IsWrittenWithoutId()
    {
        await using Rig rig = new();
        rig.Connection.SendNotification("n", JsonElement.Parse("""{"a":1}"""));

        JsonRpcMessage message = await rig.Peer.ReadAsync();
        Assert.Equal("n", message.Method);
        Assert.Null(message.Id);
        Assert.Equal(1, message.Params!.Value.GetProperty("a").GetInt32());
    }

    [Fact]
    public async Task Notifications_KeepCallOrder()
    {
        await using Rig rig = new();
        for (int i = 0; i < 1000; i++)
        {
            rig.Connection.SendNotification("n", JsonElement.Parse($$"""{"i":{{i}}}"""));
        }

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(i, (await rig.Peer.ReadAsync()).Params!.Value.GetProperty("i").GetInt32());
        }
    }

    [Fact]
    public async Task Notifications_FromManyThreads_AllArrive_NoInterleavedBytes()
    {
        await using Rig rig = new();
        Task[] senders = Enumerable.Range(0, 8).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                rig.Connection.SendNotification("n", JsonElement.Parse($$"""{"t":{{t}},"i":{{i}}}"""));
            }
        }, Ct)).ToArray();

        int[] next = new int[8];
        for (int k = 0; k < 8 * 200; k++)
        {
            JsonElement p = (await rig.Peer.ReadAsync()).Params!.Value;
            int t = p.GetProperty("t").GetInt32();
            Assert.Equal(next[t]++, p.GetProperty("i").GetInt32());
        }

        await Task.WhenAll(senders);
    }

    [Fact]
    public async Task Notification_SentBeforeRequest_ArrivesBeforeIt()
    {
        await using Rig rig = new();
        rig.Connection.SendNotification("first");
        _ = rig.Connection.SendRequestAsync("second", null, Ct);

        Assert.Equal("first", (await rig.Peer.ReadAsync()).Method);
        Assert.Equal("second", (await rig.Peer.ReadAsync()).Method);
    }

    [Fact]
    public async Task SendNotification_DoesNotBlock_WhenPeerNeverReads()
    {
        await using Rig rig = new();
        JsonElement big = JsonElement.Parse("\"" + new string('x', 100_000) + "\"");

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            rig.Connection.SendNotification("n", big);
        }

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"took {watch.Elapsed}");
    }

    [Fact]
    public async Task NotificationReceived_InOrder_OnTheReadLoop()
    {
        await using Rig rig = new();
        List<int> seen = [];
        TaskCompletionSource done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.NotificationReceived += (method, p) =>
        {
            lock (seen)
            {
                seen.Add(p!.Value.GetProperty("i").GetInt32());
                if (seen.Count == 100)
                {
                    done.SetResult();
                }
            }
        };

        for (int i = 0; i < 100; i++)
        {
            await rig.Peer.WriteAsync(TestPeer.Notification("n", $$"""{"i":{{i}}}"""));
        }

        await done.Task.WaitAsync(Wait, Ct);
        Assert.Equal(Enumerable.Range(0, 100), seen);
    }

    [Fact]
    public async Task NotificationHandlerThrows_LoopContinues()
    {
        await using Rig rig = new();
        TaskCompletionSource<string> second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.NotificationReceived += (method, _) =>
        {
            if (method == "boom")
            {
                throw new InvalidOperationException("handler failure");
            }

            second.TrySetResult(method);
        };

        await rig.Peer.WriteAsync(TestPeer.Notification("boom"));
        await rig.Peer.WriteAsync(TestPeer.Notification("fine"));

        Assert.Equal("fine", await second.Task.WaitAsync(Wait, Ct));
    }

    [Fact]
    public async Task MalformedJson_RaisesProtocolError_LoopContinues()
    {
        await using Rig rig = new();
        TaskCompletionSource<Exception> error = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.ProtocolError += e => error.TrySetResult(e);

        await rig.Peer.WriteRawAsync("{not json");

        await error.Task.WaitAsync(Wait, Ct);
        await Pong(rig);
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("null")]
    public async Task NotAnObject_RaisesProtocolError(string body)
    {
        await using Rig rig = new();
        TaskCompletionSource<Exception> error = new(TaskCreationOptions.RunContinuationsAsynchronously);
        rig.Connection.ProtocolError += e => error.TrySetResult(e);

        await rig.Peer.WriteRawAsync(body);

        await error.Task.WaitAsync(Wait, Ct);
    }

    [Fact]
    public async Task UnknownMessageShape_Ignored()
    {
        await using Rig rig = new();
        int errors = 0;
        rig.Connection.ProtocolError += _ => Interlocked.Increment(ref errors);
        rig.Connection.NotificationReceived += (_, _) => Interlocked.Increment(ref errors);

        await rig.Peer.WriteRawAsync("""{"jsonrpc":"2.0"}""");

        await Pong(rig);
        Assert.Equal(0, errors);
    }
}
