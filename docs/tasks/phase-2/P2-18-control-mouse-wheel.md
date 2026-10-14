# P2-18 — Control: mouse wheel and smooth scrolling

- **Status:** todo
- **Depends on:** P2-17
- **Size:** S

## Goal

Scroll with the mouse wheel and the touchpad like VS Code on Windows: 50 DIPs per notch, Shift scrolls horizontally,
Alt scrolls 5× faster. When `SmoothScrolling` is on, a physical wheel animates (125 ms ease-out) and a touchpad
scrolls immediately. Animation frames come from `CompositionTarget.Rendering`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/browser/ui/scrollbar/scrollableElement.ts` | 460–573 | the glue around `_onMouseWheel` (the arithmetic was ported in P2-09): accept the event in the classifier, compute, validate, smooth or immediate, consume the event if it scrolled |
| `src/vs/base/browser/mouseEvent.ts` | 122–230 | `StandardWheelEvent`: on Windows, `wheelDeltaY / 120` (one notch = 1, positive = up) and `wheelDeltaX / 120` (positive = left) |
| `src/vs/base/browser/dom.ts` | 362, 481–… | `scheduleAtNextAnimationFrame`: one callback at the next frame, cancellable |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs` — `OnPointerWheelChanged`, the animation-frame scheduler
- `src/LoomEditor.WinUI/Controls/AnimationFrameScheduler.cs` — create (`internal sealed`, see below)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — `SmoothScrolling` dependency property
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — `SmoothScrolling` and `SmoothScrollingProperty`
- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — a "Smooth scrolling" `ToggleSwitch` bound in code to `Editor.SmoothScrolling`

## Design decisions (already made — do not change)

- Public API: `public bool SmoothScrolling` (dependency property, default `false` like Monaco's `smoothScrolling`).
  Changing it calls `ViewModel.SetOptions(...)` with `ViewLayoutOptions.SmoothScrolling` updated.
- `PointerWheelChanged`: `delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta / 120.0` (120 = one notch,
  `WHEEL_DELTA`). `IsHorizontalMouseWheel` → `DeltaX = -delta`, else `DeltaY = delta` (positive = up, Monaco's
  convention; check the sign of horizontal tilt manually, see Tests). Shift and Alt come from `e.KeyModifiers`.
  The timestamp is `e.GetCurrentPoint(this).Timestamp / 1000.0` (µs → ms).
- One `MouseWheelClassifier` per control (P2-09).
- If the position changed: smooth when `MouseWheelScrolling.ShouldScrollSmoothly(classifier)`, else immediate. Set
  `e.Handled = true` when it scrolled (Monaco's `alwaysConsumeMouseWheel` is `true` in the editor, so also set it when it
  did not scroll but a scrollbar is visible).
- `AnimationFrameScheduler` implements Monaco's `scheduleAtNextAnimationFrame`. It subscribes to
  `CompositionTarget.Rendering` only while at least one callback is pending, and runs each callback once at the next
  frame. The returned `IDisposable` cancels the callback. It replaces the immediate scheduler of P2-14.
- Precision touchpads: WinUI delivers touchpad scrolling as `PointerWheelChanged` with small deltas for a control
  that does not use a `ScrollViewer`. Nothing else is needed. If a touchpad does not scroll at all during the manual
  test, **stop** and report: that would need DirectManipulation and a new card.

## Tests

- [ ] Existing tests pass (the wheel arithmetic is tested in P2-09, smooth scrolling in P2-02/P2-08)
- [ ] Manual: one wheel notch scrolls 50 DIPs (~2.6 lines at 19 DIPs); Shift+wheel scrolls horizontally; Alt+wheel scrolls 5× more
- [ ] Manual: with "Smooth scrolling" on, a physical wheel animates and a touchpad does not
- [ ] Manual: wheel over the Gallery's buttons does not scroll the editor; wheel at the end of the document is still consumed

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, manual checks done
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.WinUI`: only `SmoothScrolling` / `SmoothScrollingProperty` added
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard scrolling (Page Up/Down, Ctrl+Up/Down: phase 3 commands), inertial scrolling, middle-click autoscroll.

## Suggested commit message

`feat(winui): scroll with the mouse wheel, with optional smooth scrolling`
