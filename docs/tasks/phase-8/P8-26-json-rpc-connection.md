# P8-26 — The JSON-RPC connection: requests, notifications, cancellation, server requests

- **Status:** todo
- **Depends on:** P8-25
- **Size:** M
- **Recommended model:** Opus (concurrent read loop and ordered write queue, cancellation races, shutdown that must not hang on a pipe that cannot be cancelled, source-generated JSON under Native AOT; the failure modes are timing dependent and cannot be diagnosed from this card. On failure — a flaky or hanging test, an AOT warning you cannot remove — stop, do not commit, report the test name, the interleaving you suspect and what you tried)

## Goal

`JsonRpcConnection` speaks JSON-RPC 2.0 over two streams: it sends requests and gets their typed answers (with cancellation, error responses and connection loss), sends notifications in order without ever blocking the caller, receives notifications, and answers requests that the server sends.
It is the only class that touches the framing and the envelope; the LSP-specific layers (P8-27 …) sit on it.

## Monaco sources

Sources: none (specified in this card). JSON-RPC 2.0 (<https://www.jsonrpc.org/specification>) and the "Base Protocol" of LSP 3.17: requests carry `id`, `method`, `params`; notifications carry no `id`; responses carry `id` and `result` **or** `error`; `$/cancelRequest` cancels a request by `id`; error code values are in the spec (`ContentModified` -32801, `RequestCancelled` -32800, …).

## Files to create or change

- `src/LoomEditor.Lsp/JsonRpc/JsonRpcConnection.cs`, `JsonRpcConnection.Read.cs`, `JsonRpcConnection.Write.cs`, `JsonRpcMessage.cs` (the envelope types), `JsonRpcException.cs` (`JsonRpcException`, `JsonRpcConnectionClosedException`), `JsonRpcErrorCodes.cs` — create (`internal`)
- `src/LoomEditor.Lsp/Protocol/LspJsonContext.cs` — create: the source-generated context with **only the envelope types** for now (P8-27 adds the rest)
- `tests/LoomEditor.Lsp.Tests/JsonRpc/JsonRpcConnectionTests.cs`, `JsonRpcConnectionServerRequestTests.cs`, `JsonRpcConnectionShutdownTests.cs` — create; `tests/LoomEditor.Lsp.Tests/TestHelpers/TestPeer.cs` — create

## Design decisions (already made — do not change)

- **Envelope** (`JsonRpcMessage.cs`, `internal sealed class`, properties with `[JsonPropertyName]` in camelCase as in the spec): `string JsonRpc` (`"jsonrpc"`, always `"2.0"` when written), `JsonElement? Id`, `string? Method`, `JsonElement? Params`, `JsonElement? Result`, `JsonRpcError? Error`;
  `internal sealed class JsonRpcError { int Code; string Message; JsonElement? Data }`. `Id` is a `JsonElement?` because a server's request id can be an integer or a string and the response must echo it unchanged. A **response** is a message with an `Id`, no `Method`; `Result` is `null` both when absent and when JSON `null` (both mean "no value": fine).
  A **request** has `Id` and `Method`; a **notification** has `Method` and no `Id`.
- **`LspJsonContext`**: `[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, GenerationMode = JsonSourceGenerationMode.Default)] [JsonSerializable(typeof(JsonRpcMessage))] [JsonSerializable(typeof(JsonRpcError))] internal sealed partial class LspJsonContext : JsonSerializerContext;`.
  All (de)serialization in the project uses `LspJsonContext.Default.<Type>` type infos; the overloads with `JsonSerializerOptions` stay banned. Add to `src/BannedSymbols.txt` the other reflection-based generic overloads this project must never call:
  `M:System.Text.Json.JsonSerializer.Deserialize``1(System.ReadOnlySpan{System.Byte},System.Text.Json.JsonSerializerOptions)`, `…Deserialize``1(System.IO.Stream,System.Text.Json.JsonSerializerOptions)`, `…Serialize``1(System.IO.Stream,``0,System.Text.Json.JsonSerializerOptions)`, `…SerializeToUtf8Bytes``1(``0,System.Text.Json.JsonSerializerOptions)`, `…SerializeToElement``1(``0,System.Text.Json.JsonSerializerOptions)`,
  `…Deserialize``1(System.Text.Json.JsonElement,System.Text.Json.JsonSerializerOptions)` (same message text as the existing lines: "Use a source-generated JsonSerializerContext."). A banned-API entry whose signature does not resolve produces a warning (RS0030 / RS1xxx): if the build says a line is invalid, fix its signature, never delete the line silently.
- **`JsonRpcConnection(Stream input, Stream output)`** (`internal sealed class : IAsyncDisposable`); it **owns both streams** (disposes them in `DisposeAsync`). Members:
  - `void Start()`: starts the **read loop** (`Task.Factory.StartNew(…, TaskCreationOptions.LongRunning)` running an async method: `LongRunning` only gives it a dedicated thread for the synchronous part; the loop `await`s reads) and the **writer loop**. Calling it twice throws `InvalidOperationException`.
  - `Task<TResult?> SendRequestAsync<TParams, TResult>(string method, TParams @params, JsonTypeInfo<TParams> paramsInfo, JsonTypeInfo<TResult> resultInfo, CancellationToken token = default)`; `Task<JsonElement?> SendRequestAsync(string method, JsonElement? @params, CancellationToken token = default)` (raw; the typed one calls it). The typed overload serializes `@params` with `JsonSerializer.SerializeToElement(@params, paramsInfo)` and deserializes the result with `result.Value.Deserialize(resultInfo)`; a `null` result gives `default`.
  - `void SendNotification<TParams>(string method, TParams @params, JsonTypeInfo<TParams> paramsInfo)` and `void SendNotification(string method, JsonElement? @params = null)`: serialize **on the calling thread**, enqueue the bytes, return at once (never blocks, never throws for I/O problems: a lost connection is reported by `Closed`). Throws `JsonRpcConnectionClosedException` only if the connection is already closed or disposed.
  - `event Action<string, JsonElement?>? NotificationReceived;` raised **on the read loop thread**, in arrival order, **synchronously** (a handler must be quick and must not block: the LSP session only enqueues to its `post`, P8-35); an exception thrown by a handler is caught and ignored (it must not kill the read loop).
  - `Func<string, JsonElement?, CancellationToken, Task<JsonElement?>>? RequestHandler { get; set; }`: called for each request **from the server**, started **without awaiting it in the read loop** (`_ = HandleRequestAsync(message)`): its result element is sent as the response `result` (`null` → JSON `null`); a thrown `JsonRpcException` becomes an error response with its code / message / data; any other exception → code `InternalError` (-32603) with the exception's message; **no handler set → `MethodNotFound` (-32601)** with message `Method not found: <method>`. The `CancellationToken` passed is cancelled when the connection closes or when the server sends `$/cancelRequest` for that request id.
  - `event Action<Exception?>? Closed;` raised **once**, on the read loop thread (or the thread that disposed), when the read loop ends: `null` = clean EOF; an exception = the framing broke (`LspProtocolException`) or a stream failed. `Task Completion { get; }` completes at the same time (never faults: the exception goes to `Closed`).
  - `ValueTask DisposeAsync()`: marks the connection closed (further sends throw), completes the write queue **after draining what was already enqueued** (bounded wait: **2 seconds**, then abandon), disposes the streams (this unblocks a pending read), waits for the loops for at most **2 seconds**, faults every pending request with `JsonRpcConnectionClosedException`, raises `Closed(null)` if it has not been raised. Idempotent. **It must never hang** even when the peer never closes its end (test below).
  - `event Action<Exception>? ProtocolError;` raised for a body that is not valid JSON, not an object, or not a valid message shape: the message is **ignored** and the loop goes on (decision: a bad message does not kill the session). `LspProtocolException` from the **framing** is fatal: the loop ends, `Closed(exception)`.
- **Request ids** the client sends: `long` counter from 1 (`Interlocked.Increment`), written as JSON numbers. **Pending map**: `ConcurrentDictionary<long, TaskCompletionSource<JsonElement?>>` with `TaskCreationOptions.RunContinuationsAsynchronously`. A response whose `id` is not an integer or is unknown (cancelled, late) is ignored. An error response faults the task with `JsonRpcException(code, message, data)`.
  **Cancellation**: `token.Register(…)` when the token can be cancelled: it removes the pending entry, `TrySetCanceled(token)`, and sends the notification `$/cancelRequest` with `{ "id": <id> }` (through the normal queue) — only if the request was still pending. A token already cancelled when `SendRequestAsync` is called returns a canceled task and sends nothing.
- **Ordered writes**: one `Channel<byte[]>` (`Channel.CreateUnbounded(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false })`), one writer loop that reads items and calls `LspMessageWriter.WriteMessageAsync`; requests, notifications and responses all go through it, so the order of the calls is the order on the wire. A write failure ends the connection (`Closed(exception)`) and faults pending requests.
- **Read loop**: `LspMessageReader.ReadMessageAsync` → `JsonSerializer.Deserialize(body, LspJsonContext.Default.JsonRpcMessage)` → dispatch: response → complete the pending request; request (has `Id` and `Method`) → `$/cancelRequest` from the server is handled by the connection itself (cancel the matching running server request, no response); others → `RequestHandler`; notification → `NotificationReceived`.
  Server request ids are kept as `JsonElement` and echoed. The loop catches **everything** per message except framing / stream errors.
- `<remarks>Threading: all public members are thread-safe; events are raised on the read loop thread; DisposeAsync can be called from any thread.</remarks>`.
- **Error codes** (`JsonRpcErrorCodes`, `internal static class` of `const int`): `ParseError` -32700, `InvalidRequest` -32600, `MethodNotFound` -32601, `InvalidParams` -32602, `InternalError` -32603, `ServerNotInitialized` -32002, `UnknownErrorCode` -32001, `RequestFailed` -32803, `ServerCancelled` -32802, `ContentModified` -32801, `RequestCancelled` -32800.
- **Test peer** (`TestPeer`, tests only): wraps the **other end** of a `MemoryPipe` (P8-24) with an `LspMessageReader` and `LspMessageWriter`; `Task<JsonRpcMessage> ReadAsync()` (with a 5-second timeout that throws `TimeoutException`), `Task WriteAsync(JsonRpcMessage)`, `Task WriteRawAsync(string json)` (a raw body), helpers to build responses, errors, requests and notifications.

## Tests

- [ ] New (`JsonRpcConnectionTests`; every wait has a **5 s timeout**: no test may hang): `Request_GetsTypedResult` (use `JsonRpcMessage` itself as TResult type? use a raw `JsonElement` overload and a small `[JsonSerializable]` test type registered in a **test-local** `JsonSerializerContext` in the test project), `Request_NullResult_ReturnsNull`, `Request_ErrorResponse_FaultsWithJsonRpcException` (code, message, data preserved),
      `Request_IdsStartAtOneAndIncrease`, `Requests_ConcurrentAndAnsweredOutOfOrder` (50 requests, the peer answers in reverse order, every task gets its own answer), `Request_Cancelled_SendsCancelRequestAndCancelsTask`, `Request_TokenAlreadyCancelled_SendsNothing`, `Request_AnswerAfterCancel_Ignored`, `Response_UnknownId_Ignored`, `Response_StringId_Ignored`,
      `Notification_IsWrittenWithoutId`, `Notifications_KeepCallOrder` (1000 notifications from one thread arrive in order), `Notifications_FromManyThreads_AllArrive_NoInterleavedBytes` (8 threads × 200; every message parses: the writer loop serializes them), `Notification_SentBeforeRequest_ArrivesBeforeIt`, `SendNotification_DoesNotBlock_WhenPeerNeverReads` (fill the pipe: the call returns at once, a 10 MB total is accepted into the queue),
      `NotificationReceived_InOrder_OnTheReadLoop`, `NotificationHandlerThrows_LoopContinues`, `MalformedJson_RaisesProtocolError_LoopContinues`, `NotAnObject_RaisesProtocolError`, `UnknownMessageShape_Ignored` (no id and no method)
- [ ] New (`JsonRpcConnectionServerRequestTests`): `ServerRequest_HandlerResult_SentAsResponse` (integer id echoed), `ServerRequest_StringId_EchoedAsString`, `ServerRequest_NullResult`, `ServerRequest_HandlerThrowsJsonRpcException_SentAsError`, `ServerRequest_HandlerThrows_InternalError`, `ServerRequest_NoHandler_MethodNotFound`, `ServerRequest_HandlerIsAsync_ReadLoopKeepsReading` (a handler that waits on a TCS while a notification arrives and is delivered), `ServerRequest_CancelRequest_CancelsTheHandlerToken`, `ServerRequest_ConnectionClosed_CancelsTheHandlerToken`
- [ ] New (`JsonRpcConnectionShutdownTests`): `PeerClosesStream_ClosedRaisedOnceWithNull_PendingRequestsFault` (`JsonRpcConnectionClosedException`), `FramingError_ClosedWithProtocolException_PendingFault`, `Completion_CompletesOnClose`, `SendRequest_AfterClose_Throws`, `SendNotification_AfterClose_Throws`, `Dispose_DrainsQueuedNotifications` (10 notifications then dispose: the peer reads all 10),
      `Dispose_PeerNeverCloses_DoesNotHang` (the peer keeps its end open and silent: `DisposeAsync` completes within 5 s, `Closed(null)` raised), `Dispose_Twice_Ok`, `Dispose_FaultsPendingRequests`, `Start_Twice_Throws`, `WriteFailure_ClosesTheConnection` (a stream whose `WriteAsync` throws)
- [ ] Run the three test classes **ten times in a row** (`dotnet test --project tests/LoomEditor.Lsp.Tests -c Release` ten times) and report that none failed: these tests are timing sensitive on purpose

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish tests/LoomEditor.Lsp.FakeServer -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeds. To prove that the source-generated envelope works under Native AOT, change the fake server's `Program.cs` so that `--selftest` (the only argument) serializes one `JsonRpcMessage` with `LspJsonContext.Default.JsonRpcMessage`, deserializes it back and writes `ok` to standard output (exit code 0), anything else exits 0 silently as before; run the **published exe** with `--selftest` and report its output
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any LSP method, the LSP types (P8-27), process start (P8-29), `$/progress`, batch messages.

## Suggested commit message

`feat(lsp): add the JSON-RPC connection`
