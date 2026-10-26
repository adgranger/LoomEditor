# Protocol

LSP 3.17 types (internal, PascalCase members; the source-generated `LspJsonContext` writes camelCase and omits nulls).

- Optional members are nullable; required members are `required`. `InitializeParams.ProcessId` and `RootUri` are always written (`null` when unset).
- Union-typed values are kept as `JsonElement?` and decoded by `LspUnions` (never throws on malformed input).
- `LspClientCapabilities.Create()` builds the capabilities the client sends.
- `LspCompletionItem.Data` is opaque and round-trips unchanged (`completionItem/resolve`).
- Only the members the client reads or writes exist; unknown JSON members are ignored.
