# P16-07 — Share the pointer and scrolling decisions of `LoomEditor`

- **Status:** done
- **Depends on:** P16-05
- **Size:** M

**Recommended model:** Opus — the only card that reshapes input handlers, and the WinUI ones have no automated test: keeping the exact order of focus, capture and shared calls in two different event models needs judgement.

## Goal

The decisions taken on a pointer press or move (target, gutter click, zone, sticky scroll line, minimap point) and the scroll properties and scroll bar update
exist once. The framework event handlers stay per host and only read the event and call the shared code.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs` and their Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Pointer.cs` | 89+32, 157+23, 286+22, 320+25 | bodies of `OnPointerPressed` and `OnPointerMoved`; `HandleStickyPress`, `SetHoveredStickyLine`, `TryGetMinimapPoint`, `MinimapLeft`, `CreateMouseTarget` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Scrolling.cs` | 18+33, 73+23, 175+25 | `ScrollTop`, `ScrollLeft`, body of `ScrollFromWheel`, `UpdateScrollBar` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/LoomEditor.Pointer.cs` — new: the members listed above, and the duplicated bodies extracted as methods that take plain values, e.g.
  `PointerPressResult HandlePointerPress(EditorMouseEvent mouseEvent)` returning what the host must do (`None`, `Handled`, `HandledFocus`, `StartDrag`).
  Each host's `LoomEditor.Pointer.cs` → `git mv` to `LoomEditor.Pointer.Host.cs`: framework handlers that build the `EditorMouseEvent`, call the shared method,
  then focus (`FocusState.Pointer` / `NavigationMethod.Pointer`), capture (`CapturePointer` / `Capture`) and set `e.Handled`.
- `src/LoomEditor.HostShared/Controls/LoomEditor.Scrolling.cs` — new: `ScrollTop`, `ScrollLeft`, the wheel arithmetic (`ScrollByWheelDelta(double deltaX, double deltaY, …)`), `UpdateScrollBar`
  over host members for the scroll bar values. Host files → `LoomEditor.Scrolling.Host.cs`.

## Design decisions (already made — do not change)

- The order of operations of each handler stays exactly as in WinUI (focus before capture before `_mouseDown.Start`). Where the two hosts already differ
  (wheel delta units, `Capture` semantics), the difference stays in the host part.
- If a duplicated block cannot be extracted without changing the order of a framework call and a shared call, leave that block and say so in the Result section.

## Tests

- [x] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [x] `LoomEditorPointerTests`, `LoomEditorScrollingTests` and `HostViewZoneTests` pass unchanged.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard and IME handlers. Any new pointer behavior.

## Suggested commit message

`refactor(hosts): share the pointer and scrolling decisions of LoomEditor`

## Result

- `LoomEditor.Pointer.cs` (shared): `GutterClicked`, `_mouseDown`, `InitializePointer`, `HandlePointerPress(EditorMouseEvent, bool isMouse)` and
  `HandlePointerMove(EditorMouseEvent, bool overScrollBar)` (both return whether the event is handled), `HandlePointerExit`, `HandleStickyPress`,
  `UpdateStickyHover`, `SetHoveredStickyLine`, `TryGetMinimapPoint`, `MinimapLeft`, `CreateMouseTarget`, `UpdatePointerShape`, `SetMarginHovered`.
- The press does not return an action for the host to run (the card's `PointerPressResult` example): the sticky press focuses *before* the sticky click
  and the text press focuses and captures *before* `_mouseDown.Start` and the mouse-down event, so focus and capture are host members called from the
  shared code at their WinUI places: `FocusFromPointer()`, `CapturePressedPointer()` (the host stores the pressed pointer in `_pressedPointer` before the
  call; it replaces P16-05's `_zonePressPointer` / `CapturePointerForZone`, whose `LoomEditor.ViewZonePointer.Host.cs` files are deleted),
  `SetTextAreaCursor(bool arrow)`, `InvalidateCanvas()` (added to Avalonia's `HostHooks.Host.cs`, it calls the render-pass-safe `Invalidate`).
- `LoomEditor.Scrolling.cs` (shared): the part names, the pending scroll fields, `ScrollTop`, `ScrollLeft`, `ScrollByWheelDelta(MouseWheelInput)` (the wheel
  arithmetic and the `alwaysConsumeMouseWheel` result), `ApplyPendingScrollPosition`, `ScrollFromScrollBar`, `OnViewLayoutContentSizeChanged`,
  `UpdateScrollBarValues`, `UpdateScrollBar`. Host members: `IsAnyScrollBarVisible()`, `SetScrollBarValues(...)`, `UpdateScrollBars()` (Avalonia keeps its
  post-during-draw guard). The wheel deltas, timestamps, classifier and zoom stay in the hosts (different units).
- Kept in the hosts: `OnPointerReleased` and `OnPointerCaptureLost` (not listed; their releases differ: `ReleasePointerCapture(e.Pointer)` / `ReleaseCapture()`),
  the source filters (WinUI: scroll bar, find widget, overlay; Avalonia: scroll bar, find widget) and WinUI's arrow cursor over the widgets in a move.
- No twin looked wrong.
