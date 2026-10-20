# P5-59 — LinesOperations: delete all left / right, join lines

- **Status:** todo
- **Depends on:** P5-58
- **Size:** M

## Goal

Add `deleteAllLeft`, `deleteAllRight` (delete from the cursor to the start / end of the line, joining lines at the
boundaries) and `editor.action.joinLines`, with their tests. On Windows none of them has a default key.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/linesOperations/browser/linesOperations.ts` | 709–750 | `AbstractDeleteAllToBoundaryAction.run` (merge of overlapping ranges) |
| same | 752–828 | `DeleteAllLeftAction`: `_getEndCursorState`, `_getRangesToDelete` |
| same | 830–895 | `DeleteAllRightAction`: `_getEndCursorState`, `_getRangesToDelete` |
| same | 897–1055 | `JoinLinesAction` |
| `contrib/linesOperations/test/browser/linesOperations.test.ts` | 291–469 | suite `DeleteAllLeftAction` (5) |
| same | 471–599 | suite `JoinLinesAction` (4) |
| same | 1366–1541 | suite `DeleteAllRightAction` (6) |

## Files to create or change

- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsActions.DeleteAll.cs`, `LinesOperationsActions.Join.cs` — create
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsFeature.cs`, `README.md` — change
- `tests/LoomEditor.View.Tests/Features/LinesOperations/LinesOperationsTests.DeleteAll.cs`, `LinesOperationsTests.Join.cs` — create

## Design decisions (already made — do not change)

- The abstract class with two overrides becomes one method `DeleteAllToBoundary(ViewModel viewModel, string id, bool right)`
  that calls private `GetRangesToDeleteLeft/Right` and `GetEndCursorStateLeft/Right` (same bodies as Monaco).
- `rangesToDelete.sort(Range.compareRangesUsingStarts)` sorts the selections array in place in Monaco: sort a copy,
  stable. `Range.intersectRanges` → `Range.Intersect` (returns `Range?`), `Range.plusRange` → `Range.Plus`.
- `JoinLinesAction`: `selections.reduce(…)` with no initial value → a loop starting at index 1 with
  `previousValue = sorted[0]`. `primaryCursor.equalsSelection(previousValue)` → `==`. `lineText.substr(i)` →
  `Substring(i)`. `trimmedLinesContent.replace(/[\s﻿\xA0]+$/g, ' ')` replaces the trailing run of JavaScript
  whitespace with one space: write a private helper that walks back while `Strings.IsJavaScriptWhitespaceOnly(c.ToString())`
  is true (never .NET `\s` or `char.IsWhiteSpace`).
- `precondition: writable` → `canExecute: () => !viewModel.CursorConfig.ReadOnly`.
- **No keybinding**: on Windows `primary` is `0` (the keys exist only on macOS). Write it in the README.
- Test port: `editor.trigger('keyboard', Handler.Type, { text })` → `viewModel.Type(text, "keyboard")`;
  `editor.runCommand(CoreEditingCommands.Undo, null)` → `CoreEditingCommands.Undo.RunCoreEditingCommand(viewModel)`.

## Tests

- [ ] Port `DeleteAllLeftAction` › `should delete to the left of the cursor`, `should jump to the previous line when on first column`, `should keep deleting lines in multi cursor mode`, `should work in multi cursor mode`, `issue #36234: should push undo stop`
- [ ] Port `JoinLinesAction` › `should join lines and insert space if necessary`, `#50471 Join lines at the end of document`, `should work in multi cursor mode`, `should push undo stop`
- [ ] Port `DeleteAllRightAction` › `should be noop on empty`, `should delete selected range`, `should delete to the right of the cursor`, `should join two lines, if at the end of the line`, `should work with multiple cursors`, `should work with undo/redo`
- [ ] New: `JoinLines_TrailingNoBreakSpace_IsReplacedByOneSpace` (`"a \t"` + `"b"` → `"a b"`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `linesOperations.ts` updated (P5-59); tests table `linesOperations.test.ts` 53 / 55
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Transpose and case transforms (P5-60), macOS keys.

## Suggested commit message

`feat(view): add delete all left/right and join lines`
