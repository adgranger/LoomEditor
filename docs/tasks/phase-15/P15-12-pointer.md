# P15-12 — Pointer: click, drag selection, multi-cursor, drag and drop, gutter clicks

- **Status:** done
- **Depends on:** P15-11
- **Size:** M

**Recommended model:** Sonnet

## Goal

Mouse and touchpad behave as in the WinUI control: click, double and triple click, drag selection with auto-scroll, column selection, the multi-cursor modifier, drag and
drop of the selection, Ctrl+wheel zoom, gutter clicks.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs`, `Input/PointerInput.cs`, `Input/ScreenCoordinates.cs`. Also read
`src/LoomEditor.View/Controller/ViewController.cs` (`MultiCursorModifier`, around line 278) and `src/LoomEditor.Rendering/TextViewRenderer.HitTest.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/LoomEditor.Pointer.cs`, `Input/PointerInput.cs`, `Input/ScreenCoordinates.cs` — create (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/LoomEditorPointerTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-12"

## Design decisions (already made — do not change)

- Events: `PointerPressed` / `PointerMoved` / `PointerReleased` / `PointerCaptureLost`; capture with `e.Pointer.Capture(this)`. Click count: `e.ClickCount` (Avalonia gives it). Do not
  count clicks by hand.
- Modifiers go through `KeyMapping.ToModifiers` (P15-11), so on macOS Cmd+click is the `CtrlCmd` multi-cursor gesture. That matches Monaco on macOS.
- Cursor shape: `Cursor = new Cursor(StandardCursorType.Ibeam)` over text, `Arrow` over the gutter and the minimap (same areas as WinUI).
- `ScreenCoordinates` uses `this.PointToScreen` / `TopLevel.RenderScaling`.

## Tests

- [x] New: `Click_PlacesCaret` (click at the x of column 3 of line 2 → caret (2,3)); `DoubleClick_SelectsWord`; `Drag_SelectsRange`; `ShiftClick_ExtendsSelection`;
  `AltClick_AddsCursor` (default modifier); `CtrlWheel_Zooms`; `GutterClick_RaisesGutterClicked` (after P15-16: mark `// completed in P15-16` and assert the internal
  hook here)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Overlay widgets under the pointer (P15-15), hover widget (P15-18).

## Suggested commit message

`feat(avalonia): pointer selection, multi-cursor and drag and drop`

## Result

- Done as written; `GutterArea`, `GutterClickedEventArgs`, `GutterClicked` and `DragAndDrop` are the new public members (the pending list lost 13 keys).
- Not in this card (later cards hook into the same handlers): the view zone press and move (`HandleZonePress`, `_capturedZoneId`), the comment and diff gutters, the find widget and overlay filters (`IsFromFindWidget`, `IsFromOverlay`) and the `ProtectedCursor` of the widgets.
- The press detail is `e.ClickCount`; the other events pass 1. `Escape` while dragging releases the pointer capture through `_capturedPointer` (Avalonia has no "release all captures").
- `LoomEditor.ViewModel` is an internal member added for the tests (to aim a click at a line and column).
- Manual checks "P15-12" written in `docs/testing/phase-15-manual-tests.md`, not run.
