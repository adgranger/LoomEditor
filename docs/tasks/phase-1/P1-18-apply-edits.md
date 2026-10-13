# P1-18 — `PieceTreeTextBuffer.ApplyEdits`

- **Status:** done
- **Depends on:** P1-17
- **Size:** M

## Goal

Apply a batch of edits to the buffer at once (edits are expressed against the text *before* the batch), and return
the content changes plus the reverse edits needed for undo.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts`:

| Lines | What to port |
|---|---|
| 243–418 | `applyEdits` (validation, sorting, overlap check, reverse edits, auto-whitespace trimming) |
| 420–432 | `_reduceOperations` |
| 479–518 | `_doApplyEdits` |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer.ApplyEdits.cs` — create
- `src/LoomEditor.Core/Model/ITextBuffer.cs` — add `ApplyEditsResult ApplyEdits(IReadOnlyList<ValidAnnotatedEditOperation> operations, bool recordTrimAutoWhitespace, bool computeUndoEdits)`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferApplyEditsTests.cs` — create

## Design decisions (already made — do not change)

- Overlapping edits throw `ArgumentException` with Monaco's message ("Overlapping ranges are not allowed!").
- Ranges in the operations are assumed **already validated** (as in Monaco: `TextModel` validates them in P1-22).
- Monaco's `strings.*` helpers: use `StringClassification` (P1-09, P1-16).
- Keep the auto-whitespace trimming logic (`recordTrimAutoWhitespace`, `isAutoWhitespaceEdit`) even though nothing
  uses it yet; the cursor code (phase 3) will.

Monaco tests `applyEdits` through `TextModel` (`editableTextModel.test.ts`, ported in P1-23/P1-24), so write new tests here:

## Tests

- [x] Single insert, delete, replace → text, `Changes` (range, offset, length, text) and `ReverseEdits` correct
- [x] Several non-overlapping edits given in random order → same result as applying them back-to-front on a `string`
- [x] Applying `ReverseEdits` restores the original text exactly (seeded random batches, 200 iterations, several seeds)
- [x] Overlapping edits → `ArgumentException`
- [x] `computeUndoEdits: false` → `ReverseEdits` is `null`
- [x] Inserting RTL / non-ASCII text flips `MightContainRtl` / `MightContainNonBasicAscii`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `pieceTreeTextBuffer.ts` ported (except `findMatchesLineByLine` → phase 5)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`TextModel` (P1-20+).

## Suggested commit message

`feat(core): add ApplyEdits to PieceTreeTextBuffer`
