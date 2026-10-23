# Layout

**Phases 2, 5 and 6 (done).** Turns model lines into *view lines*: what is actually displayed once word wrap and folding
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

## What is here (phase 5 and 6)

- Hidden areas (folding): `ViewModel.HiddenAreas.cs`, `ViewModelLinesFromProjectedModel*.cs`, `CoordinatesConverter`.
- Word wrap and injected text (phase 6): `MonospaceLineBreaksComputer*.cs` (line breaks), `ModelLineProjectionData*.cs`,
  `ModelLineProjection.cs` and `WrappedModelLineProjection*.cs` (one model line as view lines), `ViewModel.Wrapping.cs`,
  `InjectedTextInlineDecorationsComputer.cs`, `EditorWrappingOptions.cs`.
- Guides (phase 6): `IndentGuidesComputer`, `GuideColors`, `ViewModel.Guides.cs`, `EditorGuidesOptions`.
- Overview ruler and minimap data (phase 6): `OverviewRulerDecorations*.cs`, `MinimapLinesRenderingData`, `EditorMinimapOptions`,
  `EditorLayoutInfoComputer.Minimap.cs`. The minimap logic itself is in `LoomEditor.View/Minimap/`.

**View lines ≠ model lines.** A view line is one display row: a model line gives one or several view lines when it wraps,
and none when it is hidden by folding. Positions are converted with the `CoordinatesConverter`; with word wrap off and no
folding the mapping is the identity. Design: [ARCHITECTURE.md](../../../docs/ARCHITECTURE.md#view-lines-with-word-wrap-and-injected-text-phase-6).

Monaco sources: `src/vs/editor/common/viewModel/` (`viewModelImpl.ts`, `viewModelLines.ts`),
`src/vs/editor/common/viewLayout/`, `src/vs/base/common/scrollable.ts`.
