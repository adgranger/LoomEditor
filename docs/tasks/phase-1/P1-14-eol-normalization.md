# P1-14 — EOL normalization (`NormalizeEol`, `SetEol`)

- **Status:** todo
- **Depends on:** P1-09
- **Size:** S

## Goal

Change the end-of-line sequence of a whole document (`\n` ↔ `\r\n`), rewriting all line breaks, as when the user
clicks "LF/CRLF" in the status bar.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts`:

| Lines | What to port |
|---|---|
| 320–353 | `normalizeEOL` |
| 359–363 | `setEOL` |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Eol.cs` — `NormalizeEol(string eol)`, `SetEol(string eol)`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeEolTests.cs` — create

## Design decisions (already made — do not change)

- Same `string` EOL convention as the constructor (`"\n"` or `"\r\n"`, else `ArgumentException`).
- `normalizeEOL` rebuilds the tree from chunks of ~`AverageBufferSize`: keep that logic exactly (it calls `Create`).
- Monaco's regex `/\r\n|\r|\n/g` → a `[GeneratedRegex]`.

Monaco has no direct test for these two methods (they are tested through `TextModel.setEOL`), so write new tests.

## Tests

- [ ] LF text → `SetEol("\r\n")` → raw content uses `\r\n` everywhere; line count and line contents unchanged
- [ ] Mixed `\r`, `\n`, `\r\n` → `SetEol("\n")` → only `\n`
- [ ] Large text (> 3 × 65,535 chars, many lines) → content correct, `TreeInvariants.AssertValid` passes
- [ ] After `SetEol`, inserts and deletes still work (`PieceTreeAssert.LinesContent`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`TextModel.SetEol` and its events (P1-22).

## Suggested commit message

`feat(core): add EOL normalization to PieceTreeBase`
