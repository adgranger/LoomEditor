# P2-02 — `Scrollable` and smooth scrolling

- **Status:** todo
- **Depends on:** P2-01
- **Size:** M

## Goal

Monaco's `Scrollable`: it owns the current `ScrollState`, raises `Scroll` events, and animates smooth scrolling
(`SmoothScrollingOperation`, ease-out cubic, 125 ms in the editor). The host gives it a function that schedules a
callback at the next animation frame. The WinUI control (P2-18) will use `CompositionTarget.Rendering` for it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/scrollable.ts` | 210–223 | `IScrollableOptions` |
| same | 225–395 | `Scrollable` |
| same | 397–409 | `SmoothScrollingUpdate` |
| same | 411–429 | `createEaseOutCubic`, `createComposed` |
| same | 431–514 | `SmoothScrollingOperation` |
| same | 516–522 | `easeInCubic`, `easeOutCubic` |
| `src/vs/base/test/common/scrollable.test.ts` | 1–122 | suite `SmoothScrollingOperation` (5 tests) |

## Files to create or change

- `src/LoomEditor.View/Layout/Scrollable.cs` — create
- `src/LoomEditor.View/Layout/SmoothScrollingOperation.cs` — create (with `SmoothScrollingUpdate` and the easing functions)
- `tests/LoomEditor.View.Tests/Layout/SmoothScrollingOperationTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ScrollableTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class Scrollable : IDisposable`. `onScroll` becomes `event EventHandler<ScrollEvent>? Scroll`.
- Options: `internal sealed record ScrollableOptions(bool ForceIntegerValues, int SmoothScrollDuration, Func<Action, IDisposable> ScheduleAtNextAnimationFrame, TimeProvider TimeProvider)`.
  `TimeProvider` replaces `Date.now()` (`TimeProvider.GetUtcNow().ToUnixTimeMilliseconds()`), so tests can use a fake
  clock. Write a small `FakeTimeProvider` in `tests/LoomEditor.View.Tests/Layout/TestHelpers/`. Do not add the
  `Microsoft.Extensions.TimeProvider.Testing` package.
- `SmoothScrollingOperation.Start(from, to, duration, TimeProvider)` replaces Monaco's `Date.now()` in `start`. The
  protected `_tick(now)` becomes `internal SmoothScrollingUpdate Tick(double now)`, and the parameterless `Tick()` reads
  the clock. The tests call `Tick(now)` directly, like Monaco's `testTick`.
- `animationFrameDisposable` stays a nullable `IDisposable` field; `Dispose` cancels it.
- Keep `reuseAnimation` (`SetScrollPositionSmooth(NewScrollPosition update, bool reuseAnimation = false)`).

## Tests

- [ ] Port: `SmoothScrollingOperation` › `scroll 25 lines (40 fit)`, `scroll 75 lines (40 fit)`, `scroll 100 lines (40 fit)`,
      `scroll 125 lines (40 fit)`, `scroll 500 lines (40 fit)` (keep Monaco's `TestSmoothScrollingOperation` helper as a
      private method that builds the operation with the same `+10`/`-10` adjustments)
- [ ] New: `SetScrollPositionNow` raises one `Scroll` event with `InSmoothScrolling == false`; setting the same position again raises none
- [ ] New: with `SmoothScrollDuration = 0`, `SetScrollPositionSmooth` behaves like `SetScrollPositionNow`
- [ ] New: smooth scrolling with a fake scheduler and a fake clock reaches the target in the last frame, raises
      events with `InSmoothScrolling == true`, and `HasPendingScrollAnimation` becomes false at the end
- [ ] New: `SetScrollDimensions` during an animation clamps the animation target (`AcceptScrollDimensions`)
- [ ] New: `Dispose` during an animation disposes the scheduled frame

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `base/common/scrollable.ts` ✅ ported, and `scrollable.test.ts` 5 / 5
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The editor's scroll dimensions (`ViewLayout`, P2-08), mouse wheel (P2-09, P2-18), any UI.

## Suggested commit message

`feat(view): port Scrollable with smooth scrolling`
