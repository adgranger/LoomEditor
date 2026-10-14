# P2-09 — Mouse wheel: `MouseWheelClassifier` and wheel → scroll position

- **Status:** todo
- **Depends on:** P2-08
- **Size:** M

## Goal

Port the UI-independent part of Monaco's mouse-wheel handling. `MouseWheelClassifier` tells a physical wheel (which
gets smooth scrolling) from a touchpad (which scrolls immediately). The body of `_onMouseWheel` turns a wheel delta
into a new scroll position (sensitivity, Alt = fast scroll, Shift = horizontal, predominant axis). P2-18 plugs it
into WinUI's `PointerWheelChanged`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/browser/ui/scrollbar/scrollableElement.ts` | 23 | `SCROLL_WHEEL_SENSITIVITY = 50` |
| same | 49–190 | `MouseWheelClassifierItem`, `MouseWheelClassifier` (without `acceptStandardWheelEvent`, which reads a DOM event) |
| same | 460–573 | `_onMouseWheel`: the delta computations and the smooth/immediate decision. **Not** inertial scrolling (lines 530–541, macOS only, option off by default), **not** `scrollYToX` and `flipAxes` (list/tree options, not editor options) |
| `src/vs/editor/common/config/editorOptions.ts` | 6374–6376, 6494–6497, 6702–6704 | defaults: `fastScrollSensitivity` 5, `mouseWheelScrollSensitivity` 1, `scrollPredominantAxis` true |
| `src/vs/base/test/browser/ui/scrollbar/scrollableElement.test.ts` | 12–732 | suite `MouseWheelClassifier` (9 tests) |

## Files to create or change

- `src/LoomEditor.View/Layout/MouseWheelClassifier.cs` — create
- `src/LoomEditor.View/Layout/MouseWheelScrolling.cs` — create
- `tests/LoomEditor.View.Tests/Layout/MouseWheelClassifierTests.cs`, `MouseWheelScrollingTests.cs` — create

## Design decisions (already made — do not change)

- `MouseWheelClassifier` is `internal sealed`. Monaco has one global `INSTANCE`; LoomEditor creates one per editor
  (owned by the control in P2-18), so two editors do not share their wheel history. Document this in
  `docs/PORTING.md` ("Behavior differences").
- `MouseWheelScrolling` is an `internal static class` with
  `NewScrollPosition? ComputeScrollPosition(MouseWheelInput input, ScrollPosition futurePosition, MouseWheelOptions options)`,
  which returns `null` when the position does not change, and `bool ShouldScrollSmoothly(MouseWheelClassifier classifier)`
  (Monaco: `SCROLL_WHEEL_SMOOTH_SCROLL_ENABLED && mouseWheelSmoothScroll && classifier.isPhysicalMouseWheel()`;
  the editor's `mouseWheelSmoothScroll` is `true`, and smooth scrolling only animates when `ViewLayoutOptions.SmoothScrolling` is on).
  - `readonly record struct MouseWheelInput(double DeltaX, double DeltaY, bool ShiftKey, bool AltKey)`. Deltas use
    Monaco's `StandardWheelEvent` convention: **positive `DeltaY` scrolls up**, one wheel notch = 1.
  - `sealed record MouseWheelOptions(double MouseWheelScrollSensitivity = 1, double FastScrollSensitivity = 5, bool ScrollPredominantAxis = true)`.
  - The result goes through `ViewLayout.ValidateScrollPosition` in the caller (Monaco validates inside `_onMouseWheel`;
    keeping `MouseWheelScrolling` pure makes it testable without a `ViewLayout`).
- Windows only: the `!platform.isMacintosh` test for Shift is always true.
- Timestamps given to `MouseWheelClassifier.Accept` are milliseconds as `double`, like Monaco.

## Tests

- [ ] Port: `MouseWheelClassifier` › `OSX - Apple Magic Mouse`, `OSX - Apple Touch Pad`, `OSX - Razer Physical Mouse Wheel`,
      `Windows - Microsoft Arc Touch`, `Windows - SurfaceBook TouchPad`, `Windows - Razer physical wheel`,
      `Windows - Logitech physical wheel`, `Windows - Microsoft basic v2 physical wheel`, `Linux Wayland - Logitech G Pro Wireless`
      (the data arrays are long: keep them as `static readonly double[][]` fields, one per test)
- [ ] New: one notch down (`DeltaY = -1`) from scrollTop 0 → scrollTop 50; Alt → 250; Shift → scrollLeft 50, scrollTop unchanged
- [ ] New: `DeltaY = 0.003` from scrollTop 100 → 99 (`50 × 0.003 = 0.15` is rounded up to 1: a tiny delta still moves)
- [ ] New: predominant axis: `(DeltaX 0.2, DeltaY -1)` scrolls vertically only

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map entry `base/browser/ui/scrollbar/scrollableElement.ts` (classifier + wheel arithmetic only), `scrollableElement.test.ts` 9 / 9, behavior difference (one classifier per editor)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`PointerWheelChanged` and the WinUI side (P2-18); inertial scrolling; scrollbar drawing (`AbstractScrollableElement`, ADR 0005).

## Suggested commit message

`feat(view): port the mouse wheel classifier and wheel scrolling`
