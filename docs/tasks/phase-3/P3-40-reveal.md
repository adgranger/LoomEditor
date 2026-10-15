# P3-40 — Reveal: scroll so that the cursor stays visible

- **Status:** todo
- **Depends on:** P3-39
- **Size:** M

## Goal

When the cursor moves out of the viewport (arrow keys, typing at the bottom, Ctrl+End…), the view scrolls just enough
to show it — vertically and horizontally — like Monaco. The cursor already emits `ViewRevealRangeRequestEvent`s
(P3-18); port the code that turns them into a scroll position. In Monaco this lives in the `ViewLines` view part; here
it is a pure class in `LoomEditor.View/Layout/` plus a horizontal step that needs the text measurer.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/viewLines/viewLines.ts`. If a line is not exactly where given, search for the name.

| Lines | What to port |
|---|---|
| 44–89 | `HorizontalRevealRangeRequest`, `HorizontalRevealSelectionsRequest` |
| 98 | `HORIZONTAL_EXTRA_PX = 30` |
| 269–300 | `onRevealRangeRequest` (vertical scroll now, horizontal request stored for after rendering) |
| 305–318 | `onScrollChanged`: cancel the pending horizontal request (the two rules) |
| 619–646 | `renderText` step (2): apply the horizontal request once its lines are rendered |
| 693–782 | `_computeScrollTopToRevealRange` |
| 784–836 | `_computeScrollLeftToReveal` |
| 838–880 | `_computeMinimumScrolling` |
| `src/vs/editor/common/config/editorOptions.ts` 6284–6298, 6679–6682 | `cursorSurroundingLines` (default 0), `cursorSurroundingLinesStyle` (default `default`), `revealHorizontalRightPadding` (default 15) |

## Files to create or change

- `src/LoomEditor.View/Layout/RevealCalculator.cs` — create: the vertical computation, `ComputeMinimumScrolling`, the
  horizontal computation given the X boxes, and the pending horizontal request rules
- `src/LoomEditor.View/Cursors/EditorCursorOptions.cs` — change: `CursorSurroundingLines` (0), `CursorSurroundingLinesStyle` (`Default`), `RevealHorizontalRightPadding` (15)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: `bool TryGetHorizontalRange(int lineNumber, int startColumn, int endColumn, out double left, out double width)` using the layout cache
- `src/LoomEditor.WinUI/Controls/LoomEditor.Reveal.cs` — create (partial): implements `IViewEventHandler` (P3-17),
  applies vertical reveals immediately and horizontal reveals after the next draw
- `tests/LoomEditor.View.Tests/Layout/RevealCalculatorTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first (`| 0`, `Math.round` → §4.1, §4.4).
- `RevealCalculator` (`internal sealed class`, one per view model): `int ComputeScrollTopToRevealRange(Viewport viewport, string? source, bool minimalReveal, Range? range, IReadOnlyList<Selection>? selections, VerticalRevealType verticalType)`
  (returns `-1` to abort, like Monaco), `static int ComputeMinimumScrolling(int viewportStart, int viewportEnd, int boxStart, int boxEnd, bool revealAtStart = false, bool revealAtEnd = false)`,
  and `(int ScrollLeft, int MaxHorizontalOffset)? ComputeScrollLeftToReveal(HorizontalRevealRequest request, Func<int, int, int, (double Left, double Width)?> visibleRangeForLineRange)`.
  The delegate stands for Monaco's `_visibleRangesForLineRange` (in phase 3 one range per line: no RTL, no wrap).
  `hasRTL` is always `false`.
- Sticky scroll does not exist: `_stickyScrollEnabled` is `false` (write the cut comment).
- `VerticalRevealType.Simple` / `Bottom` add one line of padding at the bottom (lines ~739–745): keep it.
- The control (`LoomEditor.Reveal.cs`):
  - registers itself with `ViewModel.EventDispatcher.AddViewEventHandler`;
  - on `ViewRevealRangeRequestEvent`: port of `onRevealRangeRequest` with the **future** viewport
    (`ViewLayout.GetFutureViewport()`), `ValidateScrollPosition`, `ScrollType.Immediate` when the delta is ≤ one line
    height, otherwise the event's scroll type; stores the horizontal request;
  - after each `Draw`, if a horizontal request is pending and its lines are inside the drawn viewport, computes the
    horizontal position with `TextViewRenderer.TryGetHorizontalRange`, calls `ViewLayout.SetMaxLineWidth` if needed
    (`_ensureMaxLineWidth`), sets `scrollLeft`, and clears the request;
  - cancels the pending request on scroll changes as in `onScrollChanged` (two rules).

## Tests

No Monaco test file covers `viewLines.ts`. New tests for `RevealCalculator`:

- [ ] New: `ComputeMinimumScrolling` — box above the viewport → box start; below → `boxEnd - viewportLength`; inside →
      unchanged; larger than the viewport → box start; `revealAtStart` / `revealAtEnd`
- [ ] New: cursor on line 50 of 100, viewport lines 1–20 (line height 19, height 380), `Simple` → scroll top so that line
      50 is the second-to-last visible line (one line of bottom padding)
- [ ] New: `CenterIfOutsideViewport` centers a range outside the viewport and does nothing for a range inside it
- [ ] New: several selections taller than the viewport → `-1` (do not reveal)
- [ ] New: horizontal — a caret at x = 900 with viewport width 500 and `minimalReveal = false` → scroll left so that
      `x + 15` (padding) is visible; `x - 30` (extra px) for a caret left of the viewport
- [ ] Manual (Gallery): Ctrl+End jumps to the end of a long file and shows the last line; holding → on a long line scrolls horizontally

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `viewLines.ts` (+ reveal, P3-40); behavior difference "no sticky scroll, no RTL in reveal"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard input (P3-41) — until then, test the reveal through `Commands.Execute` in a test or a temporary Gallery button
(do not commit the button).

## Suggested commit message

`feat(view): reveal the cursor when it moves out of the viewport`
