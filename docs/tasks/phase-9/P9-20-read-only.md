# P9-20 — Read-only editor

- **Status:** done
- **Depends on:** P9-18
- **Size:** M

## Goal

A `LoomEditor` control can be read-only (`IsReadOnly`): typing, deleting, cut, paste, drag and drop, undo / redo, IME composition and every editing command do nothing, while selection, copy, find, scrolling and navigation keep working. The diff editor needs it for the original side (and for the modified side when the application asks).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/editorContextKeys.ts` | search `readOnly`, `writable` | `EditorContextKeys.readOnly` / `writable` (`!readOnly`): the precondition of editing actions |
| `src/vs/editor/common/cursor/cursor.ts` | search `readOnly` (`this._configuration.options.get(EditorOption.readOnly)` in `executeEdits`, `type`, `compositionType`, `paste`, `cut`) | where the cursor refuses to edit |
| `src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts` | search `readOnly` / `domReadOnly` | the input is disabled when read-only (our equivalent: the IME / `CharacterReceived` path) |

Read each file (`curl` them) and note in the card result which entry points Monaco guards; the list below is what this repository needs.

## Files to create or change

- `src/LoomEditor.View/Features/IEditorContext.cs`, `EditorContext.cs` — change: `bool IsReadOnly { get; set; }` + `event EventHandler? ReadOnlyChanged`
- `src/LoomEditor.View/Commands/KeybindingContext.cs`, `WhenClauses.cs` — change: `bool EditorReadOnly` and a `WhenClauses.Writable` helper; editing keybindings (typing is not a keybinding: see below) get `&& !c.EditorReadOnly` **only where Monaco's action has `precondition: EditorContextKeys.writable`** (Indent, comment, move / copy lines, delete lines, sort, transpose, case transform, join, paste, cut, undo, redo, suggest, …): grep the registrations (`grep -rn "DefaultKeybindings\|WhenClauses\." src/LoomEditor.View`) and list the commands you changed in the card result
- `src/LoomEditor.View/Cursors/CursorsController.cs` (and/or the `ViewModel` entry points `Type`, `ReplacePreviousChar`, `CompositionType`, `Paste`, `Cut`, `ExecuteEdits`, `ExecuteCommand`, `Undo`, `Redo` — the narrowest common point you find) — change: when read-only, user edits are ignored
- `src/LoomEditor.WinUI/Controls/LoomEditor.ReadOnly.cs` — create: `public static readonly DependencyProperty IsReadOnlyProperty`, `public bool IsReadOnly`, wired to the context; the IME / `CharacterReceived` / drop paths and the UIA value provider's `IsReadOnly` (phase 7, `IValueProvider.IsReadOnly`) read it
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — regenerate (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `tests/LoomEditor.View.Tests/Features/ReadOnlyTests.cs` — create

## Design decisions (already made — do not change)

- Programmatic changes are **not** blocked: `TextModel` methods (`ApplyEdits`, `SetValue`, the `Text` property) and the diff editor's own updates keep working. The cursor level entry points that user commands go through are blocked. (Monaco's `executeEdits` on the editor object also checks `readOnly`; read `cursor.ts` and note in the card result whether `EditorContext.ExecuteEdits`-style API methods exist here and, if they do, which way you decided; the model API is never blocked.)
- When a blocked edit is attempted the editor does nothing silently (no beep, no exception). Monaco shows a "Cannot edit in read-only editor" message via `readOnlyMessage` on typing: **not ported** (list in the card result).
- `IsReadOnly = true` clears pending IME composition and ends a drag-and-drop in progress.
- Undo / redo are blocked while read-only; the undo stack is kept.
- Accessibility: the UIA `IsReadOnly` of the value and text patterns reports the property (ADR 0010 already exposes both; follow the existing code).
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [x] New (`EditorContext` driven, no WinUI): `ReadOnly_Type_DoesNotChangeText`, `ReadOnly_Backspace_Delete_DoNothing`, `ReadOnly_Paste_Cut_DoNothing_CopyStillWorks`, `ReadOnly_UndoRedo_DoNothing`, `ReadOnly_MoveLinesDown_DoesNothing`, `ReadOnly_CommentLine_DoesNothing`, `ReadOnly_Indent_DoesNothing`, `ReadOnly_Composition_Ignored`, `ReadOnly_SelectAll_Navigation_Find_StillWork`, `ReadOnly_ModelApplyEdits_StillWorks`, `ReadOnly_SetFalse_EditingWorksAgain`, `ReadOnlyChanged_RaisedOnChangeOnly`, `KeybindingContext_EditorReadOnly_ReflectsProperty`, `Keybinding_Indent_NotResolvedWhenReadOnly`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` run, diff checked
- [x] `docs/PORTING.md`: where Monaco guards and where this repository guards; "Behavior differences": no `readOnlyMessage`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`readOnlyMessage`, `domReadOnly`, per-range read-only, any diff editor code.

## Suggested commit message

`feat: add a read-only mode to the editor`
