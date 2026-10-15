# P3-15 — Replace commands

- **Status:** done
- **Depends on:** P3-14
- **Size:** S

## Goal

Port the family of small commands that replace a range with a text and put the cursor somewhere specific. Typing,
paste, delete, cut and IME composition all use them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/commands/replaceCommand.ts` (215 lines). Port the whole file.

| Lines | Class |
|---|---|
| 12–33 | `ReplaceCommand` (cursor at the end of the inserted text) |
| 35–65 | `ReplaceOvertypeCommand` |
| 67–86 | `ReplaceCommandThatSelectsText` |
| 88–109 | `ReplaceCommandWithoutChangingPosition` |
| 111–136 | `ReplaceCommandWithOffsetCursorState` |
| 138–163 | `ReplaceOvertypeCommandOnCompositionEnd` |
| 165–189 | `ReplaceCommandThatPreservesSelection` (uses `trackSelection`) |
| 191–215 | `addPositiveOffsetToModelPosition` (helper used by the overtype commands) |

## Files to create or change

- `src/LoomEditor.View/Cursors/Commands/ReplaceCommand.cs` — create (one file with the 7 classes and the helper; split
  into `ReplaceCommand.cs` and `ReplaceOvertypeCommand.cs` if it passes ~400 lines)
- `tests/LoomEditor.View.Tests/Cursors/Commands/ReplaceCommandTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Each class is `internal sealed class … : IEditCommand` with the constructor of Monaco (`insertsAutoWhitespace`
  defaults to `false`) and the private fields `_range`, `_text`…; `InsertsAutoWhitespace` is a get-only property.
- Port `getEditOperations` and `computeCursorState` line by line. Monaco reads `helper.getInverseEditOperations()[0].range`
  to find where the text ended up: keep that.
- The overtype commands are ported now (they are small and `PasteOperation` references `ReplaceOvertypeCommand`), even
  though overtype mode is not reachable in phase 3 (`InputMode` is always `Insert`, P3-13).
- `addPositiveOffsetToModelPosition(model, position, offset)` is a `private static` helper in the file (used by the
  overtype commands only).

## Tests

No Monaco test file targets these classes. New tests with `CommandTester.TestCommand` (P3-14), one per class:

- [x] `ReplaceCommand`: replace `(1,2)-(1,4)` of `"abcde"` with `"XY"` → `"aXYde"`, cursor `(1,4)`
- [x] `ReplaceCommandThatSelectsText`: same → selection `(1,2) → (1,4)`
- [x] `ReplaceCommandWithoutChangingPosition`: insert `"XY"` at `(1,2)` → cursor stays `(1,2)`
- [x] `ReplaceCommandWithOffsetCursorState`: insert `"()"` at `(1,2)` with offsets `(0, -1)` → cursor `(1,3)`
- [x] `ReplaceCommandThatPreservesSelection`: insert `"\n"` at `(1,1)` with a selection `(1,2) → (1,4)` → selection `(2,2) → (2,4)`
- [x] `ReplaceOvertypeCommand`: range `(1,2)-(1,2)` (empty), text `"XY"`, model `"abcde"` → `"aXYde"`, cursor `(1,4)`;
      the same at `(1,6)` (end of the line) appends: `"abcdeXY"`, cursor `(1,8)`
- [x] `ReplaceCommand` with a multi-line text puts the cursor at the end of the last inserted line

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `editor/common/commands/replaceCommand.ts` → `LoomEditor.View/Cursors/Commands/ReplaceCommand.cs` (✅, P3-15)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`ShiftCommand` (P3-31), `ColumnSelectionPasteCommand` (P3-35).

## Suggested commit message

`feat(view): port the replace commands`
