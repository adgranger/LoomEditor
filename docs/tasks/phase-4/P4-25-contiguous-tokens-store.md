# P4-25 — Core tokens: `ContiguousTokensEditing` and `ContiguousTokensStore`

- **Status:** todo
- **Depends on:** P4-24
- **Size:** M

## Goal

The per-line token store of the model exists: it keeps the tokens of every line, follows edits (deleting, inserting
lines and text inside a line), and returns `LineTokens` with default tokens for lines without tokens.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/tokens/contiguousTokensEditing.ts` | 8–144 | `EMPTY_LINE_TOKENS`, `ContiguousTokensEditing` (`deleteBeginning`, `deleteEnding`, `delete`, `append`, `insert`), `toUint32Array` |
| vscode: `src/vs/editor/common/tokens/contiguousTokensStore.ts` | 19–257 | `ContiguousTokensStore` (`flush`, `hasTokens`, `getTokens`, `_massageTokens`, `_ensureLine`, `_deleteLines`, `_insertLines`, `setTokens`, `_equals`, `acceptEdit`, `_acceptDeleteRange`, `_acceptInsertText`), `getDefaultMetadata` |

## Files to create or change

- `src/LoomEditor.Core/Tokens/ContiguousTokensEditing.cs`, `ContiguousTokensStore.cs` — create
- `tests/LoomEditor.Core.Tests/Tokens/ContiguousTokensStoreTests.cs` — create

## Design decisions (already made — do not change)

- Monaco stores `(Uint32Array | ArrayBuffer | null)[]` per line; C# stores `uint[]?` per line (no `ArrayBuffer`
  views). `toUint32Array` becomes the identity and is not ported — say so in a comment.
- `_lineTokens` is a `List<uint[]?>` with Monaco's `_len` (the array may be longer than the number of lines with
  tokens).
- `acceptEdit` takes `(Range range, int eolCount, int firstLineLength, int lastLineLength, int firstCharCode)` as
  Monaco does; the caller (P4-29) computes them with the existing `EolCounter`.

## Tests

No dedicated upstream test file (Monaco covers the store through `model.line.test.ts`, ported in P4-31 and P4-32).
New:

- [ ] `GetTokens_LineWithoutTokens_ReturnsOneDefaultToken`
- [ ] `SetTokens_SameTokens_ReportsNoChange`
- [ ] `AcceptEdit_DeleteAcrossLines_JoinsTokensOfFirstAndLastLine`
- [ ] `AcceptEdit_InsertLines_ShiftsTokensDown`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Multi-line token blocks (P4-26).

## Suggested commit message

`feat(core): port the contiguous tokens store`
