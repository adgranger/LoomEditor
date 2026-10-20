# P5-58 — LinesOperations: delete lines, indent / outdent lines, insert line above / below

- **Status:** todo
- **Depends on:** P5-57
- **Size:** S

## Goal

Add five line commands with their VS Code keys: delete lines (`Ctrl+Shift+K`), indent lines (`Ctrl+]`), outdent
lines (`Ctrl+[`), insert line above (`Ctrl+Shift+Enter`) and below (`Ctrl+Enter`). Port their tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/linesOperations/browser/linesOperations.ts` | 498–609 | `IDeleteLinesOperation`, `DeleteLinesAction` (`run`, `_getLinesToRemove`) |
| same | 611–655 | `IndentLinesAction`, `OutdentLinesAction` |
| same | 657–707 | `InsertLineBeforeAction`, `InsertLineAfterAction` |
| `contrib/linesOperations/test/browser/linesOperations.test.ts` | 1543–1623 | tests `InsertLineBeforeAction`, `InsertLineAfterAction` |
| same | 1625–1712 | `Bug 18276:[editor] Indentation broken when selection is empty`, `issue #80736: …`, `Indenting on empty line should move cursor`, `issue #62112: …` |
| same | 1714–1958 | helper `testDeleteLinesCommand` and its 10 tests |

## Files to create or change

- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsActions.Lines.cs` — create
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsFeature.cs`, `README.md` — change
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: five rules (below)
- `tests/LoomEditor.View.Tests/Features/LinesOperations/LinesOperationsTests.Lines.cs` — create

## Design decisions (already made — do not change)

- `DeleteLines`: `executeEdits(id, edits, cursorState)` then `viewModel.RevealAllCursors(id, revealHorizontal: true)`,
  between two `PushStackElement` calls (Monaco calls `revealAllCursors` before the second undo stop: keep the order).
  The operations sort is stable; the merge mutates the previous operation, so `IDeleteLinesOperation` is a small
  mutable private class.
- `IndentLines`: `TypeOperations.Indent(viewModel.CursorConfig, new TextModelCursorSimpleModel(viewModel.Model), viewModel.GetSelections())`
  (Monaco passes the text model, not the view model). `OutdentLines`: `CoreEditingCommands.Outdent.RunCoreEditingCommand(viewModel)`.
- `InsertLineBefore` / `InsertLineAfter`: `EnterOperation.LineInsertBefore` / `LineInsertAfter` (exist, `Cursors/TypeEditOperations.Enter.cs`); one
  `PushStackElement` **before** only, as Monaco.
- `precondition: writable` → `canExecute: () => !viewModel.CursorConfig.ReadOnly` for all five.
- Keybindings (`KeybindingWeight.EditorContrib`):

  | Keys (Windows) | Command id | when |
  |---|---|---|
  | `Ctrl+Shift+K` | `editor.action.deleteLines` | `WhenClauses.WritableTextInput` (`writable AND textInputFocus`) |
  | `Ctrl+]` | `editor.action.indentLines` | `WhenClauses.WritableEditorTextFocus` |
  | `Ctrl+[` | `editor.action.outdentLines` | `WhenClauses.WritableEditorTextFocus` |
  | `Ctrl+Shift+Enter` | `editor.action.insertLineBefore` | `WhenClauses.WritableEditorTextFocus` |
  | `Ctrl+Enter` | `editor.action.insertLineAfter` | `WhenClauses.WritableEditorTextFocus` |

- Test port: `Bug 18276` runs `CoreEditingCommands.Tab.RunCoreEditingCommand(editor.ViewModel)`; `Indenting on empty
  line should move cursor` uses `CursorOptions = EditorCursorOptions.Default with { UseTabStops = false }`; models with
  `insertSpaces: false` use `new TextModelOptions(insertSpaces: false)`.

## Tests

- [ ] Port `InsertLineBeforeAction`, `InsertLineAfterAction`
- [ ] Port `Bug 18276:[editor] Indentation broken when selection is empty`, `issue #80736: Indenting while the cursor is at the start of a line of text causes the added spaces or tab to be selected`, `Indenting on empty line should move cursor`
- [ ] Port `issue #62112: Delete line does not work properly when multiple cursors are on line`
- [ ] Port `empty selection in middle of lines`, `empty selection at top of lines`, `empty selection at end of lines`, `with selection in middle of lines`, `with selection at top of lines`, `with selection at end of lines`, `with full line selection in middle of lines`, `with full line selection at top of lines`, `with full line selection at end of lines`, `multicursor 1`
- [ ] New: keybindings `Ctrl+Shift+K`, `Ctrl+]`, `Ctrl+Enter` resolve to their ids; not writable → no command

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `linesOperations.ts` updated (P5-58); tests table `linesOperations.test.ts` 38 / 55; keybindings row
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Delete all left / right, join (P5-59), transpose and case (P5-60).

## Suggested commit message

`feat(view): add the delete, indent and insert line commands`
