# P10-11 — Overtype mode: the input mode and the overtype edits

- **Status:** done
- **Depends on:** P9-33
- **Size:** M

## Goal

An editor has an input mode, `Insert` (default) or `Overtype`. In overtype mode a typed character replaces the character after the caret (not past the end of the line), a composition that ends overtypes what follows it, and a paste overtypes when `OvertypeOnPaste` is true. A command `editor.action.toggleOvertype` flips the mode. No key and no caret change yet (P10-12).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/inputMode.ts` | 5–34 | `InputMode` (`'insert' \| 'overtype'`, change event) → `EditorInputMode` enum and the state in `EditorContext` |
| `src/vs/editor/common/commands/replaceCommand.ts` | 35–65, 138–164, 191–215 | `ReplaceOvertypeCommand`, `ReplaceOvertypeCommandOnCompositionEnd`, `addPositiveOffsetToModelPosition` |
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 545–560 | `SimpleCharacterTypeOperation`: the `ChosenReplaceCommand` choice (`overtype && !isDoingComposition`) |
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 406–420 | `CompositionEndOvertypeOperation` |
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 715–790 | `PasteOperation`: `shouldOvertypeOnPaste = config.overtypeOnPaste && config.inputMode === 'overtype'` and `ChosenReplaceCommand` at 759–760 and 785–786 (the `ColumnSelectionPasteCommand` of 720–721 is **not** ported: column paste inserts) |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 85–95, 160–165 | where `CompositionEndOvertypeOperation.getEdits` is called in `compositionEndWithInterceptors` and `typeWithoutInterceptors` |
| `src/vs/editor/common/cursorCommon.ts` | 82, 146, 183 | `overtypeOnPaste` and the `inputMode` getter of `CursorConfiguration` |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 6818–6996 | the suite `Overtype Mode` (all its tests) |

## Files to create or change

- `src/LoomEditor.View/Cursors/EditorInputMode.cs` — create (`enum EditorInputMode { Insert, Overtype }`)
- `src/LoomEditor.View/Cursors/ReplaceOvertypeCommand.cs` — create (both commands + the helper; find where `ReplaceCommand` is: `grep -rn "class ReplaceCommand" src`, put the new file next to it and follow its style)
- `src/LoomEditor.View/Cursors/EditorCursorOptions.cs` — change: `InputMode` (default `Insert`) and `OvertypeOnPaste` (default `true`) next to the other options
- `src/LoomEditor.View/Cursors/CursorConfiguration.cs` — change: expose both
- `src/LoomEditor.View/Cursors/TypeEditOperations.cs`, `TypeEditOperations.CompositionEnd.cs`, `TypeEditOperations.Paste.cs` — change: the three places above
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `EditorInputMode InputMode { get; set; }` with an `InputModeChanged` event; setting it updates the cursor options (find how `ReadOnly` reaches `EditorCursorOptions`: grep `ReadOnly` in `EditorContext.cs` and copy that path)
- `src/LoomEditor.View/Features/Overtype/OvertypeFeature.cs`, `OvertypeCommands.cs`, `README.md` — create; add to `EditorFeatures.Default`. Command id `editor.action.toggleOvertype` (no Monaco id: the workbench owns it)
- `tests/LoomEditor.View.Tests/Cursors/OvertypeModeTests.cs` — create

## Design decisions (already made — do not change)

- The mode is **per editor** (ADR 0013 decision 9): `EditorContext.InputMode`, not a static. Monaco's tests use the global `InputMode.setInputMode('overtype')`; the port sets `context.InputMode` (or builds the cursor options with `InputMode = Overtype`) in each test's setup.
- `ReplaceOvertypeCommand.getEditOperations` is ported literally: `offsetDelta = text.length + (range.isEmpty() ? 0 : -1)`, the end position is clamped to the end of the initial end line, the replaced range is `[start, end]`. `addPositiveOffsetToModelPosition` (lines 191–215) walks forward line by line, subtracting each line's `getLineMaxColumn` from the remaining offset (a line break counts as one unit of that arithmetic): port it **as written**, including the `throw` on a negative offset (an `ArgumentOutOfRangeException`); do not replace it by `GetOffsetAt` / `GetPositionAt` arithmetic, whose line break handling (`\r\n` counts as 2) differs.
- The undo behavior is unchanged: typing in overtype mode still merges into one undo step through `EditOperationType.TypingOther` / `getTypingOperation` as in Monaco.
- Auto-closing pairs, surround-selection, electric characters and auto-indent still take priority over `SimpleCharacterTypeOperation` in the same order as Monaco (the code path is the existing one: you only change which command class `SimpleCharacterTypeOperation` builds).
- `toggleOvertype` is disabled in a read-only editor (follow how another editing command checks it) and announces `Overtype mode` / `Insert mode` through `IEditorAnnouncer` with two new strings in `AccessibilityMessages` (English only).

## Tests

- [x] Port: every test of the `Overtype Mode` suite (`cursor.test.ts` 6818–6996): `simple type`, and the others (the test titles are in the file; keep them in `// Monaco:` comments). Replace `viewModel.type(...)` / `setSelections` by the helpers used by `tests/LoomEditor.View.Tests/Cursors` (see an existing typing test)
- [x] New: `Type_OvertypeAtEndOfLine_Appends`, `Type_OvertypeWithSelection_ReplacesTheSelection`, `Type_InsertMode_Inserts`, `Composition_Overtype_ReplacesFollowingCharacters` (use the composition helpers of the existing `CompositionTests`), `Paste_OvertypeOnPasteTrue_Overtypes`, `Paste_OvertypeOnPasteFalse_Inserts`, `ToggleCommand_FlipsModeAndFiresEvent`, `ToggleCommand_ReadOnly_DoesNothing`, `Type_OvertypeMultipleCursors`, `Undo_AfterOvertypeRun_RestoresInOneStep`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `inputMode.ts`, `replaceCommand.ts` (overtype commands), `cursorTypeEditOperations.ts` (overtype), and "Behavior differences": per-editor mode, column paste does not overtype
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The `Insert` key and the caret shape (P10-12), `ColumnSelectionPasteCommand` overtype, a status bar indicator.

## Suggested commit message

`feat(view): add overtype mode (input mode, overtype edits)`
