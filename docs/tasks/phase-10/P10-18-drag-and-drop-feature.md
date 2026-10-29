# P10-18 — Drag and drop of the selection (2/2): the command, the drop marker and the mouse cursor

- **Status:** done
- **Depends on:** P10-17
- **Size:** M

## Goal

Dragging a selection with the mouse moves its text to where it is dropped (`Ctrl` held: copies it), with a dotted caret showing the drop position and the mouse cursor showing move or copy; `Escape` cancels; a click inside a selection collapses it to the click position.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/dnd/browser/dnd.ts` | 24–242 | `DragAndDropController` (all methods: the key handlers for the trigger modifier, `_onEditorMouseDown/Up/Drag/Drop/DropCanceled`, `showAt`, `_removeDecoration`, `_hitContent`, `_hitMargin`) with the Windows trigger key `Ctrl` (`hasTriggerModifier`) |
| `src/vs/editor/contrib/dnd/browser/dragAndDropCommand.ts` | 13–108 | `DragAndDropCommand` (`getEditOperations`, `computeCursorState`: all the branches for copy / move up / move down) |
| `src/vs/editor/contrib/dnd/browser/dnd.css` | 5–25 | `dnd-target`: a 2 px **dotted** right border, color `#AEAFAD` (dark), `black` (light and hc light), `#fff` (hc dark); `mouse-default` / `mouse-copy` cursors |

## Files to create or change

- `src/LoomEditor.View/Features/DragAndDrop/DragAndDropCommand.cs`, `DragAndDropFeature.cs`, `DragAndDropController.cs`, `DragAndDropCommands.cs` (no commands: leave the file out if empty), `README.md` — create (headers: ported from the two `.ts` files); add to `EditorFeatures.Default`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `MouseStyle` (`enum EditorMouseStyle { Text, Default, Copy }`, default `Text`) with a `MouseStyleChanged` event (Monaco: `updateOptions({ mouseStyle })`)
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: `dnd-target` → a 2 px right border, drawn **solid** (Monaco's dotted pattern is cosmetic and is not ported). Row: `DecorationStyle(BorderColorId: "editorCursor.foreground", BorderWidth: 0, RightBorderWidth: 2)`; add `RightBorderWidth` (default 0) to `DecorationStyle`, drawn exactly like `LeftBorderWidth` in `TextViewRenderer.Decorations.cs` (the dotted pattern is a "Behavior difference")
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: set `ProtectedCursor` from `EditorContext.MouseStyle` (`Text` → IBeam as today, `Default` → Arrow, `Copy` → the system copy cursor: `InputSystemCursorShape` has no copy shape, so use `InputSystemCursor.Create(InputSystemCursorShape.Arrow)` and say so in the XML doc and "Behavior differences": WinUI 3 has no built-in copy cursor)
- `tests/LoomEditor.View.Tests/Features/DragAndDrop/DragAndDropCommandTests.cs`, `DragAndDropControllerTests.cs` — create; `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs` — change: one test for `RightBorderWidth`

## Design decisions (already made — do not change)

- The controller subscribes to the view events of P10-17 (`MouseDown`, `MouseUp`, `MouseDrag`, `MouseDrop`, `MouseDropCanceled`) and to the key events (`KeyDown` / `KeyUp` of the control: the trigger modifier is `Ctrl` only; the macOS `Alt` variant is not ported) and to blur (the feature gets the focus-lost event of `EditorContext`; look at what `WordHighlighterFeature` uses). All state and branches are Monaco's, including `_modifierPressed`, the "paste beside the selection" exception, the `Shift` extension when no drag selection exists, and the "collapse the selection that contains the drop position" rule (`_dragSelection === null` → the click-in-selection collapse).
- `DragAndDrop` and `ColumnSelection` options gate the key handlers like Monaco (`if (!dragAndDrop || columnSelection) return;`): read them from `EditorContext` / cursor options (P10-17 added `DragAndDrop`).
- The drop target decoration is an **empty-range** model decoration (`description: 'dnd-target'`, `className: 'dnd-target'`) in an `EditorDecorationsCollection`; `ShowAt` also reveals the position immediately (`Reveal` with an immediate scroll: use the same call `GotoDefinition` uses for an immediate reveal).
- The command runs through the executor with an undo stop before and after (`PushStackElement`), like `MoveCaretCommand` (P10-15). `DragAndDropCommand` is ported literally, including all `computeCursorState` branches (read lines 60–108).
- `_hitContent` is `ContentText` or `ContentEmpty`; `_hitMargin` is `GutterGlyphMargin`, `GutterLineNumbers` or `GutterLineDecorations` (the C# names of `MouseTargetType`).
- The mouse style returns to `Text` on mouse up, drop, cancel and blur, exactly where Monaco calls `updateOptions({ mouseStyle: 'text' })`.
- Read-only editors never start a drag (P10-17 gate), so the command needs no read-only check.

## Tests

- [x] Port: no upstream test exists for `dnd`; the following are new and cover every branch of `computeCursorState`: `Move_Down_SelectionFollowsText`, `Move_Up_SelectionFollowsText`, `Move_WithinSameLine_Right`, `Move_WithinSameLine_Left`, `Move_MultiLineSelection_Down`, `Copy_SingleLine_SelectsCopy`, `Copy_MultiLine_SelectsCopy`, `Drop_InsideSelection_DoesNothing`, `Copy_DropAtSelectionEnd_Allowed`, `Copy_DropAtSelectionStart_Allowed`
- [x] New (controller, with a fake view events source): `Drag_ShowsDecorationAtTarget_RemovedWhenInsideSelection`, `Drop_OutsideSelection_MovesText_OneUndoStep`, `Drop_WithCtrl_CopiesText`, `Drop_InsideSelection_CollapsesToDropPosition`, `Drop_Shift_ExtendsPrimarySelection`, `Drop_OnGutter_Works`, `Drop_OnScrollBarOrOutside_DoesNothing`, `Escape_Cancels_NoEdit`, `Blur_ResetsState`, `Ctrl_WhileDragging_SetsCopyMouseStyle`, `Release_ResetsMouseStyleToText`, `DragAndDropOff_NoDecorationNoEdit`, `ColumnSelection_KeyHandlersIgnored`
- [ ] Manual (section "P10-18"): select a word, drag it elsewhere in the line, to another line, below the viewport end (it scrolls to reveal the target), with `Ctrl` (copy), with `Escape` (cancel: no change); a dotted/solid bar follows the pointer; one `Ctrl+Z` undoes the move; a plain click inside a selection collapses it.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `dnd.ts`, `dragAndDropCommand.ts`, `dnd.css` rows; "Behavior differences": solid drop marker, no copy cursor, no `Alt` trigger
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Dropping text or files from other applications (OS drag and drop), dragging out of the editor, `dropIntoEditor`, touch.

## Suggested commit message

`feat(view): add drag and drop of the selection (command, drop marker, mouse style)`
