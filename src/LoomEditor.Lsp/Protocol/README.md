# Protocol

LSP 3.17 types (internal, PascalCase members; the source-generated `LspJsonContext` writes camelCase and omits nulls).

- Optional members are nullable; required members are `required`. `InitializeParams.ProcessId` and `RootUri` are always written (`null` when unset).
- Union-typed values are kept as `JsonElement?` and decoded by `LspUnions` (never throws on malformed input).
- `LspClientCapabilities.Create()` builds the capabilities the client sends.
- `LspCompletionItem.Data` is opaque and round-trips unchanged (`completionItem/resolve`).
- Only the members the client reads or writes exist; unknown JSON members are ignored.

## Files

- `MessageTypes.cs`, `CommonTypes.cs`, `LspConstants.cs`, `LspMethods.cs` — the JSON-RPC envelope, positions / ranges / locations, error and method names.
- `LifecycleTypes.cs`, `ClientCapabilitiesTypes.cs`, `ServerCapabilitiesTypes.cs`, `SyncTypes.cs` — `initialize`, the capabilities both ways, document sync.
- `CompletionTypes.cs`, `HoverTypes.cs`, `DiagnosticTypes.cs` — the three features and `publishDiagnostics`.
- `LspUnions.cs` — hand-written decoders of the LSP unions; `LspJsonContext.cs` — the source-generated JSON context.
