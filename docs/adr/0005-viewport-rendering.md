# ADR 0005 — Viewport rendering: a viewport-sized canvas, Monaco's scrolling, XAML scrollbars

- **Status:** accepted
- **Date:** 2026-10-14

## Context

Phase 2 must make a 100 MB file scroll fluidly. Such a file has about 2.5 million lines; at 19 DIPs per line, the
document is about 50 million DIPs high. ADR 0002 expected a move from `CanvasControl` to `CanvasVirtualControl`
(a large virtual canvas inside a `ScrollViewer`). That does not work at this size: XAML layout and composition use
single-precision floats, and at 50 million a float can only store multiples of 4. Lines would jitter, and the
`ScrollViewer` extent would be wrong.

Monaco has the same problem in the browser. It only puts the visible lines in the DOM and keeps all scroll
arithmetic in its own classes (`Scrollable`, `LinesLayout`, `ViewLayout`). Those classes use integers, and lines are
positioned relative to the viewport.

## Decisions

1. **The canvas is the size of the viewport.** `LoomEditor.WinUI` keeps a `CanvasControl` (`PART_Canvas`) that fills
   the control and is redrawn when something changes (scroll, edit, caret blink). Each frame draws only the visible
   lines. There is no `CanvasVirtualControl` and no `ScrollViewer`.
2. **Scrolling state is Monaco's, ported to `LoomEditor.View/Layout/`.** `ScrollState`, `Scrollable` (including smooth
   scrolling), `LineHeightsManager`, `LinesLayout`, `ViewLayout`, `MouseWheelClassifier` are ported from Monaco with
   their tests. Vertical offsets are `int` (Monaco truncates with `| 0`). The renderer subtracts `scrollTop` in
   integer arithmetic *before* converting to `float`, so drawing coordinates stay small.
3. **Scrollbars are XAML `ScrollBar` primitives** (`PART_VerticalScrollBar`, `PART_HorizontalScrollBar`) laid over
   the canvas, driven by the `Scrollable`. This keeps the Windows look and the accessibility of a standard scrollbar.
   Monaco's custom scrollbar (`scrollbarState.ts`, `abstractScrollbar.ts`) is not ported; an overview ruler (phase 5+)
   will be drawn on the canvas under the vertical scrollbar.
4. **One DirectWrite text layout per line, cached by line content.** A `CanvasTextLayout` is created for each visible
   line and kept in an LRU cache keyed by the line's text. The cache survives scrolling and edits, because unchanged
   lines keep the same text. Lines longer than `stopRenderingLineAfter` (10,000 characters, Monaco's default) are cut
   there, and a "Show more (…)" label is drawn after them, as in Monaco.
5. **Tabs use DirectWrite tab stops** (`IncrementalTabStop = tabSize × space width`) instead of Monaco's expansion of
   tabs into spaces (`viewLineRenderer.ts`). Each UTF-16 index of the line is then also an index in the layout, so no
   `CharacterMapping` is needed. Difference: a tab after a full-width character ends on a pixel tab stop, not on a
   visible-column tab stop. This is documented in `docs/PORTING.md`.
6. **View lines are model lines until word wrap (phase 6).** Phase 2 does not port `viewModelLines.ts`. The
   `ViewModel` of phase 2 forwards the model's raw line events to `ViewLayout` the way `viewModelImpl.ts` does,
   with an identity mapping.
7. **Frame timing.** Smooth-scroll animation frames come from `CompositionTarget.Rendering` (the
   `scheduleAtNextAnimationFrame` of Monaco). The caret blinks with a `DispatcherQueueTimer` (500 ms, Monaco's
   `BLINK_INTERVAL`).
8. **Visibility across our own assemblies.** The layout and rendering machinery is `internal`.
   `LoomEditor.View` gives `InternalsVisibleTo` to `LoomEditor.Rendering` and `LoomEditor.WinUI`.
   `LoomEditor.Rendering` gives it to `LoomEditor.WinUI`. `LoomEditor.Rendering` then has no public API:
   `TextViewRenderer`, `IRenderSurface` and the Win2D classes become `internal`. Nothing outside the control needs
   them, and rule 7 of `ARCHITECTURE.md` asks for a minimal public API. The public API of phase 2 is on the
   `LoomEditor` control only.

## Consequences

- ADR 0002 still holds (Win2D behind `IRenderSurface`). Only its expected move to `CanvasVirtualControl` is replaced
  by this ADR.
- If profiling shows that `CanvasControl` redraws are the bottleneck, `CanvasSwapChainPanel` or a native swap chain
  can replace it inside `LoomEditor.WinUI` without touching View or the renderer.
- The roadmap item "`CanvasVirtualControl` (or swap chain)" is delivered as "viewport-sized canvas" (this ADR).
