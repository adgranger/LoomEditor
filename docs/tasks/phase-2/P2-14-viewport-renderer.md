# P2-14 — Draw the viewport: visible lines, scroll offsets, selection

- **Status:** todo
- **Depends on:** P2-13
- **Size:** M

## Goal

The renderer draws what the `ViewModel` (P2-10) says is visible, at the scroll position: only the visible lines, at
their offsets relative to the viewport, shifted by `scrollLeft`, and the selection with real x positions. It reports
the widest line it has seen, so the horizontal scroll range grows like Monaco's. The control creates the
`ViewModel` and gives it the canvas size. Scrollbars and the wheel come later (P2-17, P2-18), so in this card the
scroll position only changes from tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/viewLines/viewLines.ts` | 546–571 | `_updateLineWidths`: max width of the rendered lines; exact reset when every line of the document is visible |
| same | 680–690 | `_ensureMaxLineWidth` (`Math.ceil`, only grows) |
| same | 191, 215, 257 | where `_maxLineWidth` resets to 0 (options, flush) |
| same | 673–675 | `scrollTop - bigNumbersDelta`: the offset applied to the lines container |
| `src/vs/editor/common/viewLayout/linesLayout.ts` | 642–651 | `bigNumbersDelta` (already ported in P2-05): understand why offsets are relative |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.cs` — rewrite `Render` (see below); remove `GetGutterWidth` (the layout comes from `EditorLayoutInfo`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — own a `ViewModel`; size changes → `SetViewportSize`; redraw on `ViewLayout.Scroll` and `LayoutInfoChanged`
- `tests/LoomEditor.Rendering.Tests/TextViewRendererTests.cs` — replace the phase 0 characterization tests with viewport tests

## Design decisions (already made — do not change)

- New signature: `void Render(IRenderSurface surface, ViewModel viewModel, Selection selection)`. The renderer owns
  its `LineLayoutCache` and gets the `ITextMeasurer` in its constructor: `TextViewRenderer(ITextMeasurer measurer)`.
- Coordinates (ADR 0005, decision 2): for line *i* of the viewport,
  `top = viewportData.RelativeVerticalOffset[i] - (scrollTop - viewportData.BigNumbersDelta)`, computed in `int`, then
  converted to `float`. `x = layoutInfo.ContentLeft + characterX - scrollLeft`, computed in `double`, then converted.
  Never convert a value of the order of the document height to `float`.
- Drawing order: background → selection → text → gutter background (`FillRectangle(0, 0, ContentLeft, height, Background)`,
  which hides text scrolled under the gutter) → line numbers. Line numbers and the line highlight become nicer in P2-15.
- Selection: per visible line, from `GetCharacterX(startColumn - 1)` to `GetCharacterX(endColumn - 1)`. A selected line
  break adds `FontInfo.SpaceWidth` (phase 0 drew "one extra character"; this is the same idea with a real width).
  For a line cut at 10,000 characters, columns past the cut are drawn up to the end of the cut layout.
- Max line width: port `_updateLineWidths` / `_ensureMaxLineWidth`. After drawing, the widest drawn line (layout
  width + overflow label) goes through `EnsureMaxLineWidth`, which calls
  `viewModel.ViewLayout.SetMaxLineWidth((int)Math.Ceiling(width))` only when it grows. When every line of the
  document was drawn, reset `_maxLineWidth` to 0 first (Monaco's exact case). `_maxLineWidth` also resets to 0 on
  `ModelRawFlush` and on font changes.
- The control: the `ViewModel` is created in `OnCanvasCreateResources` the first time, when the first `ITextMeasurer`
  (and so the first `FontInfo`) exists. Later device losses only call `SetFontInfo`. Smooth scrolling stays
  off until P2-18, so the animation-frame scheduler is not called yet. Pass one that posts the callback with
  `DispatcherQueue.TryEnqueue` (P2-18 replaces it), and `TimeProvider.System`.
  The canvas `SizeChanged` → `SetViewportSize((int)ActualWidth, (int)ActualHeight)`. Dispose the `ViewModel` in `OnUnloaded`.

## Tests

- [ ] New: 1,000 lines, viewport 200 high, line height 20, scrollTop 0 → lines 1–10 drawn at y 0, 20, … 180
- [ ] New: scrollTop 10 → line 1 drawn at y = -10, line 11 drawn (partially visible)
- [ ] New: a 2,500,000-line model scrolled near the end → every drawn `y` is between `-LineHeight` and the viewport height (no float error)
- [ ] New: scrollLeft 30 → text drawn at `ContentLeft - 30`, and a gutter background is drawn after the text
- [ ] New: a selection across 3 lines draws 3 rectangles with widths from `GetCharacterX`
- [ ] New: in a 1,000-line document showing lines 1–10, a 300-wide line raises the content width once (`ContentSizeChanged`); drawing again does not raise it again
- [ ] New: when every line is visible and the widest line gets shorter, the content width shrinks (Monaco's exact reset)
- [ ] Manual: Gallery starts and shows the text as before (no scrolling yet)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Line number styling and the current line highlight (P2-15), caret (P2-16), scrollbars (P2-17), wheel (P2-18).

## Suggested commit message

`feat(rendering): draw only the visible lines at the scroll position`
