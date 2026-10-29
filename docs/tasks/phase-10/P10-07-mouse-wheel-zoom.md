# P10-07 — `Ctrl` + mouse wheel zoom

- **Status:** done
- **Depends on:** P10-06
- **Size:** S

## Goal

With `MouseWheelZoom = true`, `Ctrl` + wheel zooms the editor like Monaco: one step per notch for a physical wheel, a smooth accumulated zoom for a touchpad. The default is `false`, so `Ctrl` + wheel does nothing special until an application opts in.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/controller/mouseHandler.ts` | 140–199 | `_setupMouseWheelZoomListener`: the physical-wheel branch (`delta = deltaY > 0 ? 1 : -1`), the gesture branch (50 ms window, `gestureStartZoomLevel + accumulated / 5`) and `hasMouseWheelZoomModifiers` (Windows: `ctrlKey && !metaKey && !shiftKey && !altKey`) |
| `src/vs/editor/common/config/editorOptions.ts` | 6499–6506 | `mouseWheelZoom` option (default `false`) |

## Files to create or change

- `src/LoomEditor.View/Controller/MouseWheelZoomGesture.cs` — create (pure; header: ported from `mouseHandler.ts`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs` — change: call the gesture first in the wheel handler (the one that builds `MouseWheelInput`, ~line 58–91) and stop there when it handled the event
- `src/LoomEditor.WinUI/Controls/LoomEditor.Zoom.cs` — change: the `MouseWheelZoom` property (`bool`, default `false`)
- `tests/LoomEditor.View.Tests/Controller/MouseWheelZoomGestureTests.cs` — create
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`, `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` (a switch "Ctrl + wheel zoom"), manual plan — change

## Design decisions (already made — do not change)

- `MouseWheelZoomGesture` is a `sealed class` with state (`_prevTime`, `_gestureStartLevel`, `_gestureHasModifiers`, `_accumulated`) and one method: `double? OnWheel(double deltaY, bool isPhysicalWheel, bool ctrl, bool shift, bool alt, bool win, long timestampMs, double currentLevel)` → the new zoom level, or `null` when the event is not a zoom (the caller then scrolls normally). Time is a parameter (never `DateTime.Now`: tests pass it). `deltaY` is `MouseWheelDelta / 120.0` (positive = wheel forward = up = zoom in, as in Monaco where `StandardWheelEvent.deltaY` is positive upwards).
- `isPhysicalWheel` is `MouseWheelClassifier.IsPhysicalMouseWheel` of the classifier the control already owns (`_wheelClassifier`, `LoomEditor.Scrolling.cs`); call `AcceptStandardWheelEvent` first, exactly like the scrolling code does (read it, do not create a second classifier).
- The caller applies `Zoom.SetLevel(level)` and marks the pointer event `Handled = true` when the result is not `null`. When `MouseWheelZoom` is `false` the gesture is not called.
- The diff overview's delegated wheel events (comment at `LoomEditor.Scrolling.cs` ~58) do not zoom: only real pointer wheel events on the editor do.

## Tests

- [x] New (all with explicit timestamps): `PhysicalWheel_CtrlUp_ZoomsInByOne`, `PhysicalWheel_CtrlDown_ZoomsOutByOne`, `PhysicalWheel_NoCtrl_ReturnsNull`, `PhysicalWheel_CtrlShift_ReturnsNull`, `PhysicalWheel_CtrlAlt_ReturnsNull`, `Touchpad_Gesture_AccumulatesDividedByFive` (three events 10 ms apart with deltaY 2, 2, 1 from level 0 → 0.4, 0.8, 1.0), `Touchpad_NewGestureAfter50Ms_RestartsFromCurrentLevel`, `Touchpad_InertiaWithoutCtrl_DoesNotZoom` (the gesture started without Ctrl keeps ignoring events until a 50 ms pause), `Touchpad_CtrlPressedMidGesture_DoesNotZoom`
- [ ] Manual (section "P10-07"): option off → `Ctrl` + wheel scrolls as before; option on → zooms, plain wheel still scrolls; a touchpad pinch / two-finger scroll with `Ctrl` zooms smoothly.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `mouseHandler.ts` (wheel zoom listener) row
- [x] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

The macOS `cmd` / pinch variant, `Ctrl` + `+` / `-` keys, zoom on the minimap.

## Suggested commit message

`feat(winui): add Ctrl + mouse wheel zoom`
