# P4-26 — Core tokens: `ContiguousMultilineTokens` and its builder

- **Status:** todo
- **Depends on:** P4-25
- **Size:** M

## Goal

A block of tokens for consecutive lines exists (the unit the tokenizers produce and the store accepts), with
`applyEdit` so that a block computed on an older version can be moved to the current one (background tokenizer,
P4-38).

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/tokens/contiguousMultilineTokens.ts` | 17–80, 111–229 | `ContiguousMultilineTokens` (`startLineNumber`, `endLineNumber`, constructor, `getLineTokens`, `appendLineTokens`, `applyEdit`, `_acceptDeleteRange`, `_acceptInsertText`, `_insertLines`) |
| vscode: same file | 18–30, 82–109 | **not ported**: `deserialize`, `serializeSize`, `serialize` (ADR 0008 decision 5: objects, not bytes) |
| vscode: `src/vs/editor/common/tokens/contiguousMultilineTokensBuilder.ts` | 9–41 | `ContiguousMultilineTokensBuilder` (`add`, `finalize`) — not `deserialize` / `serialize` (11–19, 43–65) |

## Files to create or change

- `src/LoomEditor.Core/Tokens/ContiguousMultilineTokens.cs`, `ContiguousMultilineTokensBuilder.cs` — create
- `tests/LoomEditor.Core.Tests/Tokens/ContiguousMultilineTokensTests.cs` — create

## Design decisions (already made — do not change)

- `getLineRange` (which returns Monaco's `LineRange`) is not ported; callers use `StartLineNumber` / `EndLineNumber`.
- `applyEdit(IModelContentChange change, string text)` takes a `ModelContentChange` (the existing Core record) and
  uses `EolCounter`, as upstream uses `countEOL`.
- A block is mutated only by its owner thread; once posted to the UI thread (P4-37), the worker never touches it again
  (remarks).

## Tests

No upstream unit tests. New:

- [ ] `Builder_ConsecutiveLines_GoInOneBlock`, `…_Gap_StartsANewBlock`
- [ ] `ApplyEdit_InsertLineBeforeBlock_ShiftsBlock`
- [ ] `ApplyEdit_DeleteInsideBlock_RemovesLines`
- [ ] `ApplyEdit_EditInsideALine_EditsItsTokens`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated; "Behavior differences": no serialization
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

State stores (P4-27).

## Suggested commit message

`feat(core): port contiguous multi-line tokens`
