# P1-26 — `TextChange` and change compression

- **Status:** todo
- **Depends on:** P1-22
- **Size:** M

## Goal

`TextChange` is the offset-based record of one edit (old text → new text) that the undo stack stores. Consecutive
batches of changes are *compressed* into one list, so that typing 100 characters stores one change, not 100.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/core/textChange.ts` | 9–15 | `escapeNewLine` (used by `ToString`) |
| same | 17–97 | `TextChange` — **without** `write`/`read`/`writeSize` (binary serialization, not needed) |
| same | 99–105 | `compressConsecutiveTextChanges` |
| same | 107–346 | `TextChangeCompressor` |

## Files to create or change

- `src/LoomEditor.Core/Undo/TextChange.cs` — `internal sealed class TextChange` (namespace `LoomEditor.Core.Undo`)
- `src/LoomEditor.Core/Undo/TextChangeCompressor.cs` — `internal` (+ static `CompressConsecutiveTextChanges`)
- `src/LoomEditor.Core/Undo/README.md` — update (what is ported, what is next)
- `tests/LoomEditor.Core.Tests/Undo/TextChangeTests.cs`, `TextChangeCompressorTests.cs`

## Design decisions (already made — do not change)

- Fields: `OldPosition`, `OldText`, `NewPosition`, `NewText` and the computed `OldLength`, `OldEnd`, `NewLength`, `NewEnd`
  (offsets are `int`, as everywhere in Core).
- Monaco has **no test file** for `textChange.ts`; it is tested through undo. Write a property test (below).

## Tests

- [ ] `TextChange` lengths/ends computed correctly; `ToString` escapes newlines like Monaco
- [ ] Compressing an empty previous list returns the current list
- [ ] Typing simulation: 100 single-character inserts at increasing offsets, compressed batch by batch → one change
- [ ] **Property test** (seeded, several seeds): start from a random string `s0`; generate batch A then batch B of
  random non-overlapping changes (B expressed on the text after A); applying `Compress(A, B)` to `s0` gives the same
  text as applying A then B

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `textChange.ts` ported (except binary serialization — listed under "Behavior differences")
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The undo stack itself (P1-27).

## Suggested commit message

`feat(core): port TextChange and change compression`
