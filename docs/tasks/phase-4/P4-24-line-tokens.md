# P4-24 — Core tokens: `LineTokens` and `IViewLineTokens`

- **Status:** done
- **Depends on:** P4-23
- **Size:** M

## Goal

The tokens of one model line exist as Monaco's `LineTokens` (end offset + metadata pairs), with the view's
`IViewLineTokens`, `inflate`, `sliceAndInflate` and `withInserted`, and `lineTokens.test.ts` passes.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/tokens/lineTokens.ts` | 14–303 | `IViewLineTokens`, `LineTokens` (statics `createEmpty`, `createFromTextAndMetadata`, `convertToEndOffset`, `findIndexInTokensArray`; every member except `getClassName`, `getInlineStyle`, `getTokensInRange`) |
| vscode: same file | 305–423 | `SliceLineTokens`, `getStandardTokenTypeAtPosition` |
| vscode: same file | 435–526 | **not ported**: `TokenArray`, `TokenInfo`, `TokenArrayBuilder` (used by features of later phases) |
| vscode: `src/vs/editor/test/common/core/testLineToken.ts` | 13–156 | `TestLineToken`, `TestLineTokens`, `TestLineTokenFactory` (test helper) |
| vscode: `src/vs/editor/test/common/core/lineTokens.test.ts` | 12–252 | the 7 tests and their helpers |

## Files to create or change

- `src/LoomEditor.Core/Tokens/IViewLineTokens.cs`, `LineTokens.cs` (+ `LineTokens.Slice.cs` for `SliceLineTokens`) — create
- `tests/LoomEditor.Core.Tests/Tokens/TestHelpers/TestLineToken.cs` — create
- `tests/LoomEditor.Core.Tests/Tokens/LineTokensTests.cs` — create

## Design decisions (already made — do not change)

- `LineTokens` holds `uint[] _tokens` (pairs `[endOffset, metadata]`), the line text and the `ILanguageIdCodec`.
- `getClassName` / `getInlineStyle` are not ported (CSS); `IViewLineTokens` gets `GetMetadata`, `GetForeground` and
  `GetPresentation` instead, used by the renderer (P4-44). Say so in the XML doc.
- `withInserted` takes `IReadOnlyList<(int Offset, string Text, uint TokenMetadata)>` (Monaco's object literals).

## Tests

- [x] Port: `'withInserted 1'`, `'withInserted (tokens at the same position)'`, `'withInserted (tokens at the end)'`,
      `'basics'`, `'findToken'`, `'inflate'`, `'sliceAndInflate'` (exact names from the file)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map and ported-tests list updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Token stores (P4-25).

## Suggested commit message

`feat(core): port LineTokens`
