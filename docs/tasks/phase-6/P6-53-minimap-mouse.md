# P6-53 — Minimap pointer input: click to reveal, slider drag, hover

- **Status:** done
- **Depends on:** P6-52
- **Size:** S

## Goal

Clicking the minimap reveals the clicked line (or, for `fill` / `fit`, starts a drag from the slider's center), dragging the
slider scrolls the editor (and snaps back when the pointer wanders 140 DIPs away, on Windows), touch drags scroll, and the
hover state feeds the slider / auto-hide presentation.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/minimap/minimap.ts`.

| Lines | What to port |
|---|---|
| 46 | `POINTER_DRAG_RESET_DISTANCE = 140` |
| 1230–1235 | mouse over / leave (`_isMouseOverMinimap`) |
| 1237–1270 | pointer down on the minimap (proportional: reveal the line under the pointer; other sizes: drag from the slider center) |
| 1274–1280 | pointer down on the slider (start dragging) |
| 1283–1306, 1363–1367 | touch start / move / end, `scrollDueToTouchEvent` |
| 1322–1361 | `_startSliderDragging` (the orthogonal-distance reset on Windows, `getDesiredScrollTopFromDelta`) |

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapPointerController.cs` — create (header `minimap.ts (InnerMinimap pointer handlers)`): pure logic —
  `PointerDown(double x, double y, bool isLeftButton, bool isMouse)`, `PointerMove(double x, double y)`, `PointerUp()`,
  `PointerEntered()` / `PointerExited()`, hit test "on the slider", using the last rendered `MinimapLayout`
- `src/LoomEditor.Rendering/TextViewRenderer.Minimap.cs` — change: expose the last rendered layout and the minimap rectangle
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: route pointer events that fall inside the minimap rectangle to the
  controller **before** the text mouse handling (capture the pointer while dragging)
- `tests/LoomEditor.View.Tests/Minimap/MinimapPointerControllerTests.cs` — create

## Design decisions (already made — do not change)

- Coordinates are DIPs relative to the minimap's top-left; Monaco's `e.offsetY` with
  `internalOffsetY = (canvasInnerHeight / canvasOuterHeight) * offsetY` is kept as is.
- `platform.isWindows` is always true for LoomEditor: the 140-DIP reset always applies (comment it).
- Touch: WinUI touch pointers (`PointerDeviceType.Touch`) drive the same `scrollDueToTouchEvent` path: pointer down on the minimap
  with touch starts it, moves continue it, up ends it (Monaco's gesture `Start` / `Change` / `End`).
- The controller calls `IMinimapModel.RevealLineNumber` / `SetScrollTop` (P6-49) and sets the slider `IsActive` / `IsHovered` flags of
  `MinimapPresentationState` (P6-52).

## Tests

- [x] New: `Click_Proportional_RevealsTheLineUnderThePointer`
- [x] New: `Click_FitSize_StartsDraggingFromTheSliderCenter`
- [x] New: `DragSlider_ScrollsByTheSliderRatio`
- [x] New: `DragSlider_PointerFarAway_ResetsTheScroll`
- [x] New: `RightClick_Mouse_DoesNothing`; `Touch_ScrollsToTheTouchLocation`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `minimap.ts` row ✅ except section headers
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Mouse wheel over the minimap (Monaco forwards it to the editor's scrollable: the control already scrolls on wheel anywhere).

## Suggested commit message

`feat(winui): click and drag in the minimap`
