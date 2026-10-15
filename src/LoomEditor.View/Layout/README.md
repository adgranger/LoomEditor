# Layout

**Phase 2 (done) and 6.** Turns model lines into *view lines*: what is actually displayed once word wrap and folding
are applied, and which lines are visible in the viewport. No pixels here: only line numbers, columns and DIPs.

## What is here (phase 2)

- Scrolling: `ScrollState`, `ScrollEvent`, `Scrollable`, `SmoothScrollingOperation`.
- Vertical layout: `LineHeightsManager`, `LinesLayout` (offsets, whitespaces, viewport data).
- Fonts and horizontal layout: `BareFontInfo`, `FontInfo`, `FontMeasurements`, `EditorLayoutInfo`,
  `EditorLayoutInfoComputer`.
- `ViewLayout`: content size, scroll position and visible lines of an editor.
- Mouse wheel: `MouseWheelClassifier`, `MouseWheelScrolling`.
- `ViewModel`, `ViewportData`: follow the `TextModel`, keep `ViewLayout` and `EditorLayoutInfo` up to date, and build a
  `ViewportData` for each frame (visible lines and their offsets relative to the scroll position).

**View lines = model lines until phase 6.** The `ViewModel` uses an identity mapping: no word wrap, folding or
injected text yet, so a model line number is a view line number. Phase 6 adds the projections
(`viewModelLines.ts`).

Monaco sources: `src/vs/editor/common/viewModel/` (`viewModelImpl.ts`, `viewModelLines.ts`),
`src/vs/editor/common/viewLayout/`, `src/vs/base/common/scrollable.ts`.
