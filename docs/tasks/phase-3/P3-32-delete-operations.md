# P3-32 — Backspace and Delete (`DeleteOperations`, `deleteLeft`, `deleteRight`)

- **Status:** done
- **Depends on:** P3-31
- **Size:** S

## Goal

Backspace deletes the selection, or one grapheme / emoji to the left, or one indentation step when the cursor is in
the leading whitespace; Delete does the same to the right. Port `DeleteOperations` (without auto-closing pairs and
without cut) and the core commands `deleteLeft` / `deleteRight`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorDeleteOperations.ts` | 18–73 | `deleteRight`, `getDeleteRightRange` (uses `MoveOperations.right` and `trimWhitespaceOnDelete`) |
| same | 166–191 | `deleteLeft` — **cut** lines 167–169 (auto-closing pair delete) |
| same | 193–233 | `getDeleteLeftRange` (tab-stop unindent with `indentSize`), `getPositionAfterDeleteLeft` (`strings.getLeftDeleteOffset`) |
| `src/vs/editor/browser/coreCommands.ts` | 1966–1979 | `CoreEditingCommand` (base) |
| same | 2047–2094 | `DeleteLeft` (`deleteLeft`), `DeleteRight` (`deleteRight`) |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 7 tests of the suite `Editor Controller` |

## Files to create or change

- `src/LoomEditor.View/Cursors/DeleteOperations.cs` — create
- `src/LoomEditor.View/Commands/CoreEditingCommands.cs` — create (`internal static class`, this card: `DeleteLeft`, `DeleteRight`)
- `src/LoomEditor.View/Commands/CoreCommandRegistration.cs` — change: register `deleteLeft`, `deleteRight`
- `tests/LoomEditor.View.Tests/Cursors/DeleteOperationsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `DeleteOperations.DeleteLeft(...)` / `DeleteRight(...)` return `(bool ShouldPushStackElementBefore, IReadOnlyList<IEditCommand?> Commands)`
  (Monaco returns a tuple `[boolean, Array<ICommand | null>]`).
- **Cut points (phase 4–5):** `isAutoClosingPairDelete` (75–149) and `_runAutoClosingPairDelete` (151–164) are not
  ported; the guard of lines 167–169 is replaced by the comment. Without a language, Monaco's `autoClosingPairsOpenByEnd`
  is empty and this guard never fires: same behavior.
- `getDeleteLeftRange` uses `CursorColumns.PrevIndentTabStop` (P3-03) with **`config.IndentSize`** (not `TabSize`),
  only when the cursor column is ≤ the last indentation column, and only when `config.UseTabStops`. Port exactly.
- `getPositionAfterDeleteLeft` uses `Strings.GetLeftDeleteOffset` (P3-01): Backspace after `👍🏽` deletes the whole
  emoji with its modifier; after a ZWJ family it deletes the last member.
- The C# command class: `internal abstract class CoreEditingCommand` with `string Id` and
  `abstract void RunCoreEditingCommand(ViewModel viewModel)` (Monaco also passes `editor`; `editor.pushUndoStop()` is
  `viewModel.Model.PushStackElement()` and `editor.executeCommands(id, commands)` is `viewModel.ExecuteCommands(commands, source: id)`).
  After executing, `viewModel.SetPrevEditOperationType(EditOperationType.DeletingLeft)` (or `DeletingRight`), as in Monaco.
- `CursorTestHelpers` (P3-27): add `DeleteLeft(TestCodeEditor)` / `DeleteRight(TestCodeEditor)` that run the core
  commands (Monaco tests use `editor.runCommand(CoreEditingCommands.DeleteLeft, null)`).

## Tests

Port from `cursor.test.ts`, suite `Editor Controller` (with `TestCodeEditor`; options such as `useTabStops` go through
`TestCodeEditorOptions.CursorOptions`, model options such as `tabSize` through `ModelOptions`):

- [x] `issue #1140: Backspace stops prematurely` (2033)
- [x] `issue #84897: Left delete behavior in some languages is changed` (2975)
- [x] `issue #122914: Left delete behavior in some languages is changed (useTabStops: false)` (3007)
- [x] `issue #99629: Emoji modifiers in text treated separately when using backspace` (3039)
- [x] `issue #99629: Emoji modifiers in text treated separately when using backspace (ZWJ sequence)` (3057)
- [x] `UseTabStops is off` (3631)
- [x] `Backspace removes whitespaces with tab size` (3648)

If a test uses `viewModel.type(...)` to prepare the text, prepare it with `Model.SetValue` / `ExecuteEdits` instead
**only if** the typed text does not matter to the test; otherwise move that test to P3-33 and say so in the commit body.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `cursorDeleteOperations.ts` (🔶 without auto-closing and cut, P3-32); tests table `cursor.test.ts` (+7)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`cut` (P3-36), the "Undo stops" suite (P3-33, it needs typing), auto-closing pair delete (phase 4–5).

## Suggested commit message

`feat(view): port Backspace and Delete`
