# P10-17 — Drag and drop of the selection (1/2): the drag state of the mouse operation

- **Status:** done
- **Depends on:** P9-33
- **Size:** M

## Goal

`MouseDownOperation` recognizes a press inside a non-empty selection as the start of a drag (it no longer collapses the selection), reports the drag moves and the drop (or its cancellation) as three new view events, and does nothing else: no text is moved yet (P10-18). With the option `DragAndDrop` set to `false`, behavior is exactly today's.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/controller/mouseHandler.ts` | 408–430 | `_onMouseDownThenMove`: the `isDragAndDrop` branch (`emitMouseDrag`) |
| `src/vs/editor/browser/controller/mouseHandler.ts` | 438–500 | `start`: the condition of lines 461–472 and the monitoring callbacks that emit `emitMouseDropCanceled` (a **key press** ended the drag: Escape) or `emitMouseDrop` (the button was released) then stop |
| `src/vs/editor/browser/controller/mouseHandler.ts` | 680–705 | `MouseDownState.isDragAndDrop` |
| `src/vs/editor/browser/view/viewUserInputEvents.ts` | the `emitMouseDrag`, `emitMouseDrop`, `emitMouseDropCanceled` methods and their event types | the three events |
| `src/vs/editor/common/config/editorOptions.ts` | 6324–6330 | `dragAndDrop` option, default `true` |

## Files to create or change

- `src/LoomEditor.View/Controller/MouseDownOperation.cs` — change: the drag branch in `Start` (replace the comment "Not ported in phase 3: drag-and-drop of the selection (mouseHandler.ts 461–497)"), in `OnMouseDownThenMove`, and the drop / cancel paths; new members `OnKeyDownWhileDragging()` (cancel) and `IsDragAndDrop`
- `src/LoomEditor.View/Controller/MouseDownState.cs` — change: `IsDragAndDrop`
- `src/LoomEditor.View/Controller/ViewController.cs`, `ViewUserInputEvents.cs` — change: `EmitMouseDrag`, `EmitMouseDrop`, `EmitMouseDropCanceled` and the events `MouseDrag`, `MouseDrop` (`EditorMouseEventArgs` of the existing events), `MouseDropCanceled` (`EventArgs`)
- `src/LoomEditor.View/Cursors/EditorCursorOptions.cs` — change: `DragAndDrop` (default `true`); `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: the `DragAndDrop` property (`bool`, default `true`, public), Escape while a drag is active calls `OnKeyDownWhileDragging` (find where `KeyDown` is handled: `LoomEditor.Keyboard.cs`)
- `tests/LoomEditor.View.Tests/Controller/MouseDownOperationDragTests.cs` — create (look at the existing `MouseDownOperation` tests and use the same harness)

## Design decisions (already made — do not change)

- The condition to start a drag is Monaco's, literally: not read-only, `DragAndDrop` option, not `ColumnSelection`, no Alt (Monaco: "we don't support multiple mouse"; here `_mouseState.AltKey`), click count `< 2`, not already active, the **current selection** (`_currentSelection`: the primary selection, as Monaco's field) is not empty, the target type is `ContentText`, and the target position is inside that selection (`Selection.ContainsPosition`). When it holds, `IsDragAndDrop = true`, `IsActive = true` and `Start` returns **without** dispatching the mouse (the selection stays).
- While active in drag mode `OnMouseDownThenMove` emits `MouseDrag` (event args: the mouse event and the hit-test result, i.e. the same `EditorMouseEventArgs` type as `MouseMove`) and does **not** scroll or select. Edge scrolling during a drag is **not** ported (Monaco does not either: the drag branch is before the scrolling code).
- On release (`OnPointerUp`) in drag mode: emit `MouseDrop` with the hit test of the **last** mouse event (`null` target when the position is unknown: the event args carry a nullable target, as Monaco's `IPartialEditorMouseEvent`), then `Stop`. A click without movement in a selection therefore **is** a drop onto the same position: P10-18 makes that collapse the caret (`_dragSelection === null` branch of Monaco's `_onEditorMouseDrop`).
- `OnKeyDownWhileDragging()` emits `MouseDropCanceled` and stops. The control calls it for **any** key press while `IsDragAndDrop` is active (Monaco: `dom.isKeyboardEvent(browserEvent)` in the monitor callback). Ctrl pressed alone must not cancel (it switches to copy: Monaco's keyboard handling is in P10-18, so the control passes `Escape` only; document that this is stricter than Monaco and why: Ctrl is the copy modifier).
- `IsDragAndDrop` is reset in `Stop`.
- All of this is inside `LoomEditor.View`; WinUI only forwards keys and exposes the property.

## Tests

- [x] New: `Press_InsideNonEmptySelection_StartsDrag_AndKeepsSelection`, `Press_OutsideSelection_SelectsAsBefore`, `Press_InsideEmptySelection_SelectsAsBefore`, `Press_DragAndDropOff_SelectsAsBefore`, `Press_ReadOnly_SelectsAsBefore`, `Press_ColumnSelection_SelectsAsBefore`, `Press_DoubleClickInSelection_SelectsWord`, `Press_AltInSelection_AddsCursorAsBefore`, `Move_WhileDragging_EmitsMouseDragNotSelection`, `Release_WhileDragging_EmitsMouseDropWithLastTarget`, `Release_WithoutMove_EmitsMouseDropOnSamePosition`, `Escape_WhileDragging_EmitsDropCanceledAndStops`, `Stop_ResetsIsDragAndDrop`
- [x] Manual (section "P10-17"): with the option on and nothing else changed, pressing inside a selection and dragging does **not** change the selection (nothing moves yet: P10-18); releasing without moving leaves the selection as it was (P10-18 will collapse it); everything else about selecting with the mouse is unchanged.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `mouseHandler.ts` row (drag branch ported), remove the "not ported" note for it
- [x] Public API file updated (`DragAndDrop`); card status and `docs/tasks/README.md` updated

## Out of scope

Moving the text, the drop caret decoration, the mouse cursor (P10-18), dragging text out of the editor or from outside (OS drag and drop), touch.

## Suggested commit message

`feat(view): recognize a drag that starts inside the selection (drag and drop, part 1)`
