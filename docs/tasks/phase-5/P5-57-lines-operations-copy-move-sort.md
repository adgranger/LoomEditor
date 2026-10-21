# P5-57 — LinesOperations: copy, duplicate, move, sort, remove duplicates, reverse, trim

- **Status:** done
- **Depends on:** P5-56
- **Size:** M

## Goal

The `LinesOperations` feature registers its first eleven commands: copy lines up / down (`Shift+Alt+↑/↓`), duplicate
selection, move lines up / down (`Alt+↑/↓`), sort ascending / descending, remove duplicate lines, reverse lines and
trim trailing whitespace (`Ctrl+K Ctrl+X`). Port the matching tests of `linesOperations.test.ts` and the
`Duplicate Selection` suite.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/linesOperations/browser/linesOperations.ts` | 33–122 | `AbstractCopyLinesAction.run`, `CopyLinesUpAction`, `CopyLinesDownAction` (ids, `kbOpts`) |
| same | 124–163 | `DuplicateSelectionAction` |
| same | 167–237 | `AbstractMoveLinesAction.run`, `MoveLinesUpAction`, `MoveLinesDownAction` |
| same | 239–296 | `AbstractSortLinesAction.run`, `SortLinesAscendingAction`, `SortLinesDescendingAction` |
| same | 298–372 | `DeleteDuplicateLinesAction` |
| same | 374–448 | `ReverseLinesAction` |
| same | 450–501 | `TrimTrailingWhitespaceArgs`, `TrimTrailingWhitespaceAction` |
| `contrib/linesOperations/test/browser/linesOperations.test.ts` | 19–28 | helpers `assertSelection`, `executeAction` |
| same | 34–129, 131–187 | suites `SortLinesAscendingAction` (4), `SortLinesDescendingAction` (2) |
| same | 189–289 | suite `DeleteDuplicateLinesAction` (4) |
| same | 601–824 | suite `ReverseLinesAction` (12) |
| `contrib/linesOperations/test/browser/copyLinesCommand.test.ts` | 205–263 | suite `Editor Contrib - Duplicate Selection` (2) |

## Files to create or change

- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsActions.CopyMove.cs`, `LinesOperationsActions.Sort.cs`
  (sort, duplicates, reverse, trim) — create: `internal static partial class LinesOperationsActions`, one method per action taking the `ViewModel`
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsFeature.cs` — change: register the eleven commands
- `src/LoomEditor.View/Features/LinesOperations/README.md` — change
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: five rules (below)
- `docs/how-to/add-a-feature.md` — change: the note below
- `tests/LoomEditor.View.Tests/Features/LinesOperations/LinesOperationsTests.cs` (+ `.Reverse.cs`) — create
- `tests/LoomEditor.View.Tests/Features/LinesOperations/CopyLinesCommandTests.cs` — change: add the `Duplicate Selection` suite

## Design decisions (already made — do not change)

- Every action: `viewModel.Model.PushStackElement()`, then `viewModel.ExecuteCommands(commands, id)` or
  `viewModel.ExecuteEdits(id, edits, _ => endCursorState)` (Monaco's `executeEdits` with an array; `undefined` →
  `_ => null`), then `PushStackElement()`. Selections are sorted with a **stable** sort.
- `precondition: writable` → `canExecute: () => !viewModel.CursorConfig.ReadOnly` for all eleven.
- Move uses `viewModel.CursorConfig.AutoIndent` and the model's language configuration registry (as in P5-51).
- `TrimTrailingWhitespaceAction`: no arguments, so `reason: 'auto-save'` never happens and `cursors` is empty.
  `files.trimTrailingWhitespaceInRegexAndStrings` is a workbench setting: use its default `true`
  (`// files.trimTrailingWhitespaceInRegexAndStrings: VS Code's default`).
- `DuplicateSelectionAction` uses `ReplaceCommandThatSelectsText` (exists) for non-empty selections.
- `new Set()` of lines in `DeleteDuplicateLinesAction` → `HashSet<string>(StringComparer.Ordinal)`.
- Keybindings (`KeybindingWeight.EditorContrib`, `when` = `WhenClauses.WritableEditorTextFocus`; Linux overrides ignored):

  | Keys (Windows) | Command id |
  |---|---|
  | `Shift+Alt+↑` / `Shift+Alt+↓` | `editor.action.copyLinesUpAction` / `editor.action.copyLinesDownAction` |
  | `Alt+↑` / `Alt+↓` | `editor.action.moveLinesUpAction` / `editor.action.moveLinesDownAction` |
  | `Ctrl+K Ctrl+X` | `editor.action.trimTrailingWhitespace` |

- **How-to.** `docs/how-to/add-a-feature.md` keeps "Duplicate line" as its teaching example. Add under its first
  paragraph: "This guide is an example. The real command ships in `src/LoomEditor.View/Features/LinesOperations/`
  (`editor.action.copyLinesDownAction`). If you type the example in, give it the id `loom.example.duplicateLine`:
  command ids are unique and `CommandRegistry.Register` throws on a duplicate." Change nothing else in the how-to.
- Test port: `executeAction(action, editor)` → `LinesOperationsActions.X(editor.ViewModel)`; `withTestCodeEditor(lines, {}, …)`
  → `TestCodeEditor.WithTestCodeEditor(lines, editor => …)`.

## Tests

- [x] Port `SortLinesAscendingAction` › `should sort selected lines in ascending order`, `should sort lines in ascending order`, `should sort multiple selections in ascending order`, `applies to whole document when selection is single line`
- [x] Port `SortLinesDescendingAction` › `should sort selected lines in descending order`, `should sort multiple selections in descending order`
- [x] Port `DeleteDuplicateLinesAction` › `should remove duplicate lines within selection`, `should remove duplicate lines`, `should remove duplicate lines in multiple selections`, `applies to whole document when selection is single line`
- [x] Port `ReverseLinesAction` › `reverses lines`, `excludes empty last line`, `updates cursor`, `preserves cursor on empty last line`, `preserves selected text when selections do not span lines`, `reverses lines within selection`, `reverses lines within partial selection`, `reverses lines with multiple selections`, `updates selection positions after reversal`, `applies to whole document when selection is single line` (769), `excludes end line when selection ends at column 1`, `applies to whole document when selection is single line` (805; same name twice upstream: suffix the C# method names `_1` / `_2`)
- [x] Port `Editor Contrib - Duplicate Selection` › `empty selection`, `with selection`
- [x] New: keybindings `Alt+↓` → `editor.action.moveLinesDownAction`, `Ctrl+K` then `Ctrl+X` → `editor.action.trimTrailingWhitespace`; read-only → no command
- [x] New: `MoveLinesDown_TwoCursors_MovesBothBlocksInOneUndoStep`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `contrib/linesOperations/browser/linesOperations.ts` (🔶 first eleven actions, P5-57);
      tests tables `linesOperations.test.ts` 22 / 55, `copyLinesCommand.test.ts` 10 / 10; keybindings row
- [ ] Manual check (maintainer): Gallery, `Alt+↑/↓` and `Shift+Alt+↑/↓` on a C# file with a selection
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Delete / indent / insert lines (P5-58), delete all left / right and join (P5-59), transpose and case (P5-60).

## Suggested commit message

`feat(view): add the copy, move, sort and trim line commands`
