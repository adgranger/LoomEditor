# P3-46 — The control: mouse selection

- **Status:** todo
- **Depends on:** P3-45
- **Size:** M

## Goal

Wire WinUI pointer events to the mouse logic of P3-43 … P3-45: click to place the cursor, drag to select, double and
triple click, Shift+click, Alt+click for more cursors, Alt+Shift+drag and middle-button drag for column selection,
clicks and drags on the line numbers, automatic scrolling while dragging outside, and the I-beam pointer over text.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What |
|---|---|---|
| `src/vs/editor/browser/controller/mouseHandler.ts` | 310–353 | `_onMouseDown`: which buttons and targets start an operation (left and middle; content; line numbers only with `selectOnLineNumbers`; nothing on the glyph margin and line decorations) |
| same | 260–308 (search the names) | `_onMouseMove`, `_onMouseLeave`, `_onMouseUp` (only the parts that drive `MouseDownOperation`) |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — create (partial): `OnPointerPressed` (replaces the P2/P3-41
  version), `OnPointerMoved`, `OnPointerReleased`, `OnPointerCaptureLost`, `OnPointerExited`
- `src/LoomEditor.WinUI/Input/PointerInput.cs` — create: `PointerRoutedEventArgs` → `EditorMouseEvent` (P3-45)
- `src/LoomEditor.WinUI/Input/README.md` — update

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `EditorMouseEvent.Detail`: pass **4** on every press (found in P3-45: `MouseDownState.TrySetCount` takes the minimum of the reported count and the number of clicks at the same position, so a constant 1 would never give a double-click; 4 is Monaco's highest count and the state clamps it to one more than the previous click).
- Pressed: focus the control (as today), `CapturePointer`, build the `EditorMouseEvent` (position relative to the
  control in DIPs, `PointerPointProperties.IsLeftButtonPressed` / `IsMiddleButtonPressed`, modifiers from
  `e.KeyModifiers`: `Menu` → Alt, `Control` → Ctrl, `Shift` → Shift, `Windows` → Meta; `TimestampMs = point.Timestamp / 1000`),
  run Monaco's `_onMouseDown` rules, start the `MouseDownOperation`, mark the event handled.
- Moved: when an operation is active, forward the move (it dispatches with `InSelectionMode = true`); otherwise only
  update the pointer shape.
- Released / capture lost: end the operation, `ReleasePointerCapture`.
- Pointer shape: `ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam)` over the text area,
  `Arrow` over the gutter and the scrollbars (the XAML scrollbars keep their own pointer).
- The hit test passed to `MouseDownOperation` is `MouseTargetFactory.CreateMouseTarget(viewModel, renderer, x, y)`
  (P3-43); when the renderer does not exist yet (no canvas), ignore the pointer event.
- Drag scrolling frames use the existing `AnimationFrameScheduler` (`CompositionTarget.Rendering`, P2-18).
- Touch and pen: treat them like a left mouse button (no gestures in phase 3); write it in the README.

## Tests

The logic is tested in P3-43 … P3-45. This card is wiring; manual checks in the Gallery (Debug and the AOT build):

- [ ] Click places the caret at the nearest character boundary, also in a proportional font (`Segoe UI`) and on CJK / emoji
- [ ] Drag selects; dragging above/below the editor scrolls, faster when further away
- [ ] Double-click selects a word, triple-click a line, four clicks everything; Shift+click extends
- [ ] Alt+click adds a cursor, Alt+click on it again removes it; typing goes to all cursors
- [ ] Alt+Shift+drag and middle-button drag make a column selection across tabs
- [ ] Click and drag on the line numbers select whole lines
- [ ] The pointer is an I-beam over the text and an arrow over the gutter

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the manual checks pass in the AOT app
- [ ] `docs/PORTING.md`: map row `mouseHandler.ts` (+ WinUI wiring, P3-46)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Context menu, hover, drag-and-drop of text, touch gestures.

## Suggested commit message

`feat(winui): select with the mouse`
