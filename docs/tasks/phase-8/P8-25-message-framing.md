# P8-25 — LSP message framing: `Content-Length` reader and writer

- **Status:** done
- **Depends on:** P8-24
- **Size:** S

## Goal

`LspMessageReader` turns a byte stream (a language server's stdout) into JSON message bodies, and `LspMessageWriter` writes a body with its header to a stream (the server's stdin), robustly: split reads, multi-byte characters, extra headers, EOF and malformed headers are all defined.

## Monaco sources

Sources: none (specified in this card). The wire format is the "Base Protocol" of the LSP 3.17 specification (<https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#baseProtocol>): a header part (`Content-Length: <n>\r\n`, optional `Content-Type: …\r\n`) ended by an empty line `\r\n`, then exactly `n` bytes of UTF-8 JSON.

## Files to create or change

- `src/LoomEditor.Lsp/JsonRpc/LspMessageReader.cs`, `LspMessageWriter.cs`, `LspProtocolException.cs` — create (`internal`)
- `tests/LoomEditor.Lsp.Tests/JsonRpc/LspMessageReaderTests.cs`, `LspMessageWriterTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Lsp.JsonRpc`. `internal sealed class LspProtocolException(string message) : Exception(message)` — "the peer broke the protocol" (malformed header, EOF inside a message, message too large).
- **`LspMessageReader(Stream input, int maxMessageSize = 64 * 1024 * 1024)`**: `ValueTask<byte[]?> ReadMessageAsync(CancellationToken token)`: returns the body bytes of the next message, or `null` when the stream ends **cleanly between messages** (zero bytes read where a header would start).
  Not thread-safe (one reader loop), `<remarks>`. It owns a private 8 KiB `byte[]` buffer and reads with `input.ReadAsync(buffer, token)` (handles any chunking; a header can arrive one byte at a time, a body in pieces).
  Header rules (decided here because the spec leaves them open):
  1. A header line ends at `\n`; one optional `\r` before it is stripped (**lenient**: a bare `\n` is accepted; `\r\n` is what servers send). An **empty line** ends the header part.
  2. `name: value` split at the first `:`; the name is compared **ignoring case** (`Content-Length`, `content-length`); the value is trimmed of spaces and tabs. `Content-Type` and any other header are accepted and ignored. A line without `:` → `LspProtocolException("Malformed header line")`.
  3. `Content-Length` must be present exactly once (a second one overrides nothing: **duplicate → exception**), parse as a non-negative base-10 `int` with `int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, …)`; non-numeric, negative or `> maxMessageSize` → `LspProtocolException`. Missing at the empty line → `LspProtocolException("Missing Content-Length")`. `Content-Length: 0` is valid: the result is an empty array (the caller decides it is not JSON).
  4. A single header line longer than **8192 bytes** → `LspProtocolException("Header line too long")`; more than **32** header lines → `LspProtocolException("Too many headers")`.
  5. The body is exactly `Content-Length` **bytes** (not characters: multi-byte UTF-8 characters count by their bytes). EOF before the body is complete, or EOF inside a header (some bytes read, no empty line yet) → `LspProtocolException("Unexpected end of stream")`.
  6. Bytes after the body belong to the next message (kept in the buffer). The reader never decodes the body (the JSON layer does).
  Cancellation: the token is passed to every `ReadAsync`; an `OperationCanceledException` propagates unchanged.
- **`LspMessageWriter(Stream output)`**: `ValueTask WriteMessageAsync(ReadOnlyMemory<byte> body, CancellationToken token)`: writes the ASCII header `Content-Length: {body.Length}\r\n\r\n` (length formatted with `CultureInfo.InvariantCulture`; **no** `Content-Type`), then the body, then `FlushAsync`. The header and the body are written with **one** `WriteAsync` call on a buffer when `body.Length <= 64 KiB` (copy into a pooled `byte[]` from `ArrayPool<byte>.Shared`, return it in `finally`) and with two calls above that.
  Not thread-safe (the connection serializes writes, P8-26), `<remarks>`.
- Headers: no Monaco header; the file header of `LspMessageReader` is absent (nothing ported).

## Tests

- [x] New (`LspMessageReaderTests`; the input is a `MemoryStream`, or a `ChunkedStream` test helper in the same folder that returns at most `chunkSize` bytes per read, to test split reads): `ReadsOneMessage`, `ReadsTwoMessagesBackToBack`, `EmptyStream_ReturnsNull`, `EofBetweenMessages_ReturnsNull`, `OneByteChunks_StillParses`, `HeaderSplitAcrossReads`, `BodySplitAcrossReads` (chunk 3),
      `MultiByteBody_LengthIsInBytes` (a body containing `é`, `€`, `😀`: `Content-Length` counts bytes; the result decodes back), `ContentType_Ignored`, `UnknownHeader_Ignored`, `HeaderName_CaseInsensitive`, `ValueWhitespace_Trimmed`, `BareLf_Accepted`, `ZeroLength_ReturnsEmptyArray`, `MissingContentLength_Throws`, `DuplicateContentLength_Throws`,
      `NonNumericLength_Throws`, `NegativeLength_Throws`, `LengthWithPlusSign_Throws` (`NumberStyles.None`), `LengthTooLarge_Throws` (`maxMessageSize` 10, length 11), `LineWithoutColon_Throws`, `HeaderLineTooLong_Throws` (8193 bytes), `TooManyHeaders_Throws` (33 lines), `EofInHeader_Throws`, `EofInBody_Throws`, `Cancelled_ThrowsOperationCanceled`,
      `LargeBody_1MiB` (1 MiB body read through 4 KiB chunks), `AfterThrow_ReaderIsNotReusable` (document: after an `LspProtocolException` the reader must be discarded; assert the next call also throws or returns null without hanging — pick the behavior your implementation has and test it)
- [x] New (`LspMessageWriterTests`): `WritesHeaderAndBody` (exact bytes `Content-Length: 5\r\n\r\nhello`), `MultiByteBody_LengthInBytes`, `EmptyBody`, `LargeBody_TwoWrites_SameBytes` (> 64 KiB: bytes identical to the small-path format), `Flushes` (a stream that records `Flush` calls), `WriterThenReader_RoundTrip` (through `MemoryPipe` of P8-24, 100 messages of varying size), `Cancelled_Throws`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

JSON parsing (P8-26, P8-27), `Content-Type` charset handling (a charset other than UTF-8 is not supported: the reader ignores the header and the JSON layer assumes UTF-8), the connection (P8-26).

## Suggested commit message

`feat(lsp): add the Content-Length message reader and writer`
