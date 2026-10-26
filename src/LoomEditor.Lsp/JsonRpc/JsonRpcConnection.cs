using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;

namespace LoomEditor.Lsp.JsonRpc;

/// <summary>JSON-RPC 2.0 over two streams: typed requests, ordered notifications, cancellation and server requests.</summary>
/// <remarks>Threading: all public members are thread-safe; events are raised on the read loop thread; DisposeAsync can be called from any thread.</remarks>
internal sealed partial class JsonRpcConnection(Stream input, Stream output) : IAsyncDisposable
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);
    private static readonly JsonElement NullElement = JsonElement.Parse("null");

    private readonly Channel<byte[]> _queue = Channel.CreateUnbounded<byte[]>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement?>> _pending = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _serverRequests = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationTokenSource _readCancellation = new();
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task _readTask = Task.CompletedTask;
    private Task _writeTask = Task.CompletedTask;
    private long _nextId;
    private int _started;
    private int _closed;
    private int _closeRaised;
    private int _disposed;
    private volatile bool _disposing;

    /// <summary>Raised on the read loop thread, in arrival order, for every notification of the peer; must not block.</summary>
    public event Action<string, JsonElement?>? NotificationReceived;

    /// <summary>Raised for a body that is not a valid message; the message is ignored.</summary>
    public event Action<Exception>? ProtocolError;

    /// <summary>Raised once when the connection ends: <c>null</c> for a clean end, the failure otherwise.</summary>
    public event Action<Exception?>? Closed;

    /// <summary>Answers the requests of the peer; no handler means <c>MethodNotFound</c>.</summary>
    public Func<string, JsonElement?, CancellationToken, Task<JsonElement?>>? RequestHandler { get; set; }

    /// <summary>Completes (never faults) when the connection ends.</summary>
    public Task Completion => _completion.Task;

    /// <summary>Starts the read loop and the writer loop.</summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("The connection is already started");
        }

        _readTask = Task.Factory.StartNew(ReadLoopAsync, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        _writeTask = Task.Run(WriteLoopAsync);
    }

    /// <summary>Sends a typed request and returns its typed result (<c>default</c> for a null result).</summary>
    public async Task<TResult?> SendRequestAsync<TParams, TResult>(
        string method, TParams @params, JsonTypeInfo<TParams> paramsInfo, JsonTypeInfo<TResult> resultInfo, CancellationToken token = default)
    {
        JsonElement? result = await SendRequestAsync(method, JsonSerializer.SerializeToElement(@params, paramsInfo), token).ConfigureAwait(false);
        return result is { } value ? value.Deserialize(resultInfo) : default;
    }

    /// <summary>Sends a request and returns the raw result. A cancelled token sends <c>$/cancelRequest</c>.</summary>
    public async Task<JsonElement?> SendRequestAsync(string method, JsonElement? @params, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ThrowIfClosed();

        long id = Interlocked.Increment(ref _nextId);
        TaskCompletionSource<JsonElement?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;
        if (Volatile.Read(ref _closed) != 0)
        {
            _pending.TryRemove(id, out _);
            throw ClosedException();
        }

        using CancellationTokenRegistration registration = token.Register(() =>
        {
            if (_pending.TryRemove(id, out TaskCompletionSource<JsonElement?>? removed))
            {
                removed.TrySetCanceled(token);
                try
                {
                    SendNotification("$/cancelRequest", JsonElement.Parse(string.Create(CultureInfo.InvariantCulture, $"{{\"id\":{id}}}")));
                }
                catch (JsonRpcConnectionClosedException)
                {
                    // Closed meanwhile: nobody left to cancel for.
                }
            }
        });

        Enqueue(new JsonRpcMessage { Id = IdElement(id), Method = method, Params = @params }, throwIfClosed: true);
        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>Serializes on the calling thread and queues a typed notification; never blocks.</summary>
    public void SendNotification<TParams>(string method, TParams @params, JsonTypeInfo<TParams> paramsInfo)
        => SendNotification(method, JsonSerializer.SerializeToElement(@params, paramsInfo));

    /// <summary>Queues a notification; never blocks. Throws only when the connection is already closed.</summary>
    public void SendNotification(string method, JsonElement? @params = null)
    {
        ThrowIfClosed();
        Enqueue(new JsonRpcMessage { Method = method, Params = @params }, throwIfClosed: false);
    }

    /// <summary>Drains the queued messages (2 s at most), closes the streams, faults the pending requests. Idempotent, never hangs.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _closed, 1);
        _queue.Writer.TryComplete();
        await WaitAsync(_writeTask).ConfigureAwait(false);

        _disposing = true;
        await _readCancellation.CancelAsync().ConfigureAwait(false);
        DisposeStream(input);
        DisposeStream(output);
        await WaitAsync(Task.WhenAll(_readTask, _writeTask)).ConfigureAwait(false);

        Close(null);
    }

    private static async Task WaitAsync(Task task)
    {
        if (!task.IsCompleted)
        {
            try
            {
                await task.WaitAsync(ShutdownTimeout).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Timed out (abandon the loop) or the loop failed (already reported through Closed).
            }
        }
    }

    private static void DisposeStream(Stream stream)
    {
        try
        {
            stream.Dispose();
        }
        catch (Exception)
        {
            // Closing a broken stream: nothing more to do.
        }
    }

    private static JsonElement IdElement(long id) => JsonElement.Parse(id.ToString(CultureInfo.InvariantCulture));

    private static JsonRpcConnectionClosedException ClosedException(Exception? inner = null)
        => new("The JSON-RPC connection is closed", inner);

    private void ThrowIfClosed()
    {
        if (Volatile.Read(ref _closed) != 0)
        {
            throw ClosedException();
        }
    }
}
