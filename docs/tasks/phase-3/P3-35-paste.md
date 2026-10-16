# P3-35 — Paste (single, distributed over cursors, block paste)

- **Status:** done
- **Depends on:** P3-34
- **Size:** M

## Goal

Paste text: at every cursor; spread line by line over several cursors when the number of lines matches
(`multiCursorPaste: "spread"`); as a block when the text was copied from a column selection; above the current line
when the text was copied from an empty selection (a whole line). Port `PasteOperation`, `ColumnSelectionPasteCommand`
and the paste path of the cursor and the view model, with their tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 702–795 | `PasteOperation`: `getEdits`, `_blockPaste`, `_distributePasteToCursors`, `_distributedPaste`, `_simplePaste` |
| `src/vs/editor/common/commands/columnSelectionPasteCommand.ts` | 13–74 | `ColumnSelectionPasteCommand` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 60–62 | `TypeOperations.paste` |
| `src/vs/editor/common/cursor/cursor.ts` | 609–615 | `CursorsController.paste` |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | search `public paste(` | `ViewModel.paste` |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 11 paste tests |
| `src/vs/editor/test/browser/controller/columnSelectionPaste.test.ts` | 26–543 | suite `Column selection paste` (47 tests) |

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.Paste.cs` — create (partial): `PasteOperation`
- `src/LoomEditor.View/Cursors/Commands/ColumnSelectionPasteCommand.cs` — create
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change: `Paste`
- `src/LoomEditor.View/Cursors/CursorsController.Type.cs` — change: `Paste`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `Paste(string text, bool pasteOnNewLine, IReadOnlyList<string>? multicursorText = null, string? source = null, bool isBlock = false)`
- `tests/LoomEditor.View.Tests/Cursors/PasteTests.cs`, `ColumnSelectionPasteTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `PasteOperation.getEdits` sorts the caller's selection array **in place** with `Range.compareRangesUsingStarts`
  (line ~711) and command `i` then belongs to sorted selection `i`. In C#: copy the selections, sort the copy with a
  **stable** sort (conventions §4.3) and use the copy everywhere Monaco uses the sorted array.
- `_simplePaste` reassigns `pasteOnNewLine` inside its loop (lines ~774–779): once it becomes `false` for one selection
  it stays `false` for the next ones. Port it as it is (a local variable modified in the loop).
- `_distributePasteToCursors` in `"spread"` mode strips **one** trailing `"\n"` and then one trailing `"\r"`, then splits
  with `Strings.SplitLines` (P3-01). Port the exact order.
- Overtype (`ReplaceOvertypeCommand` when `inputMode == "overtype"` and `overtypeOnPaste`): keep the branch; it is never
  taken in phase 3 (`InputMode` is always `Insert`).
- `ColumnSelectionPasteCommand` pads short lines using **visible** columns (`CursorConfiguration.VisibleColumnFromColumn`,
  graphemes and full-width characters count as in Monaco).
- `CursorsController.Paste` uses `ExecuteEdit(..., CursorChangeReason.Paste)` (P3-19), as in lines 609–615.
- The test helper `paste(editor, text, overrides)` of `columnSelectionPaste.test.ts` (Monaco: `editor.trigger('keyboard', Handler.Paste, payload)`)
  becomes a call to `ViewModel.Paste(text, pasteOnNewLine: false, multicursorText: Strings.SplitLines(text), source: "keyboard", isBlock: true)`
  with the overrides applied. The suite's `teardown` resets Monaco's clipboard metadata singleton: not needed in C#.

## Tests

- [x] Port from `cursor.test.ts`, suite `Editor Controller`: `issue #10212` (2054), `issue #74722` (2072),
      `issue #4996` (2091), `issue #16155` (2120), `issue #43722` (2163), `issue #46440 (1)` (2194), `issue #46440 (2)` (2218),
      `issue #256039` (2242), `issue #85712` (2953), `issue #15118` (3566), `issue #40695` (3603) — read each test name
      in the file and keep it in the `// Monaco:` comment
- [x] Port `columnSelectionPaste.test.ts` › suite `Column selection paste` (47 tests) **except**:
  `uses successive model lines when word wrap is enabled` (236, word wrap: phase 6),
  `reports pasted ranges for cursors and multiline destination selections` (491, needs the `onDidPaste` editor event),
  `respects overtype and pads short rows` (530, overtype mode) — 44 tests ported
- [x] **Not ported:** the nested suite `clipboard metadata` (10 tests, line 544): it tests browser clipboard events
      (`DataTransfer`); the clipboard card P3-47 has its own tests

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `cursorTypeEditOperations.ts` (+ paste), `columnSelectionPasteCommand.ts` (✅, P3-35);
      tests table `cursor.test.ts` (+11) and `columnSelectionPaste.test.ts` 44 / 57 with the reasons
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Reading the Windows clipboard (P3-47), copy and cut (P3-36).

## Suggested commit message

`feat(view): port paste and column selection paste`
