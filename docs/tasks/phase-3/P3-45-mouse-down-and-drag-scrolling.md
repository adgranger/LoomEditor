# P3-45 — Mouse down, drag selection and drag scrolling (pure logic)

- **Status:** done
- **Depends on:** P3-44
- **Size:** M

## Goal

Port the state of one mouse-down operation: counting clicks (double/triple click within 400 ms at the same position),
remembering the modifiers and buttons, dispatching on mouse down and on every move while the button is held (drag
selection), finding the position when the mouse leaves the editor, and scrolling automatically while dragging outside
(faster the further away). Everything is pure: time and frames are injected, so it is testable without WinUI.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/controller/mouseHandler.ts` | 658–749 | `class MouseDownState` (`CLEAR_MOUSE_DOWN_COUNT_TIME = 400`, `trySetCount`) |
| same | 360–656 | `class MouseDownOperation`: `start` (443–516, **without** selection drag-and-drop), `_onMouseDownThenMove` (410–441), `_getPositionOutsideEditor` (532–593), `_findMousePosition`, `_dispatchMouse` (633–656), `isActive`, `onPointerUp`, `onHeightChanged`, `onScrollChanged` |
| `src/vs/editor/browser/controller/dragScrolling.ts` | 18–218 | `DragScrolling`, `DragScrollingOperation`, `TopBottomDragScrolling(Operation)`, `LeftRightDragScrolling(Operation)` (speed formulas 112–124, 173–184; ticks 126–159, 186–217) |

## Files to create or change

- `src/LoomEditor.View/Controller/MouseDownState.cs` — create
- `src/LoomEditor.View/Controller/MouseDownOperation.cs` — create
- `src/LoomEditor.View/Controller/DragScrolling.cs` — create (top/bottom and left/right)
- `src/LoomEditor.View/Controller/EditorMouseEvent.cs` — create: what the control gives for each pointer event
- `tests/LoomEditor.View.Tests/Controller/MouseDownStateTests.cs`, `MouseDownOperationTests.cs`, `DragScrollingTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `EditorMouseEvent` = `internal sealed record EditorMouseEvent(double X, double Y, bool LeftButton, bool MiddleButton, bool AltKey, bool CtrlKey, bool ShiftKey, bool MetaKey, int Detail, long TimestampMs)`
  (`X`, `Y` in DIPs relative to the control; `Detail` is the click count the platform reports — WinUI does not report
  one, so the control passes 4 (not 1: see P3-46) and `MouseDownState.TrySetCount` does the counting, as Monaco does for IE).
- Time: `MouseDownState` takes a `TimeProvider` (Monaco calls `new Date().getTime()`). Frames: `DragScrollingOperation`
  takes `Func<Action, IDisposable> scheduleAtNextAnimationFrame` (the same delegate as the view model, ADR 0005
  decision 7) and the `TimeProvider`.
- `MouseDownOperation(ViewModel viewModel, ViewController viewController, Func<double, double, MouseTarget> createMouseTarget, …)`:
  the hit test of P3-43 is passed as a delegate so the class stays free of Rendering types.
- **Selection drag-and-drop is not ported** (ADR 0006, decision 12): the condition of lines 461–497 is replaced by
  `// Not ported in phase 3: drag-and-drop of the selection (mouseHandler.ts 461–497)` and `isDragAndDrop` is always `false`.
- `_getPositionOutsideEditor`: returns a `MouseTarget` of type `OutsideEditor` (P3-43) with the position Monaco computes
  (above: line at `scrollTop - distance`, column 1; below: the line's max column; left of `contentLeft`: column 1;
  right of the content: max column) and the outside distance used by drag scrolling.
- `selectOnLineNumbers` (default `true`, `editorOptions.ts` search `selectOnLineNumbers`): add it to `EditorCursorOptions`.

## Tests

No Monaco test file covers these classes. New tests:

- [x] New: `TrySetCount` — two clicks 300 ms apart at the same position → count 2; 500 ms apart → 1; second click at
      another position → 1; a reported count of 3 after a count of 1 → 2 (no jump)
- [x] New: `MouseDownOperation` — down at `(1,3)`, move to `(1,6)` with the button held → selection `(1,3) → (1,6)`
      (dispatched with `InSelectionMode = true`); up → inactive
- [x] New: double-click then drag → the selection grows by words (`_wordSelectDrag`)
- [x] New: moving above the editor while dragging starts top drag scrolling. Speed in lines per second, with
      `v = viewport height / lineHeight` and `d = outside distance / lineHeight` (lines 112–124):
      `d ≤ 1.5` → `max(30, v × (1 + d))`; `d ≤ 3` → `max(60, v × (2 + d))`; else `max(200, v × (7 + d))`.
      With line height 19, viewport 380 (v = 20), d = 1 and a fake clock advancing 100 ms between frames, one frame
      scrolls `max(30, 40) × 0.1 × 19 = 76` DIPs up
- [x] New: moving back inside the editor stops drag scrolling

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `mouseHandler.ts` (🔶 `MouseDownState`, `MouseDownOperation` without drag-and-drop, P3-45),
      `dragScrolling.ts` (✅, P3-45); behavior difference "no drag-and-drop of the selection"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

WinUI pointer events and pointer capture (P3-46), touch (`pointerHandler.ts`), the context menu.

## Suggested commit message

`feat(view): port mouse-down, drag selection and drag scrolling logic`
