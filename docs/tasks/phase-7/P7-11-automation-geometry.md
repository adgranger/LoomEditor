# P7-11 — Geometry: bounding rectangles, point to position, visible range

- **Status:** done
- **Depends on:** P7-10
- **Size:** M

## Goal

The UI-free half of `GetBoundingRectangles`, `RangeFromPoint` and `GetVisibleRanges`: from a range (or a point) to editor-relative rectangles (or a position), given the measurements
the renderer provides through a small interface. Screen magnifiers and Narrator's "highlight" use these.

## Sources

None for the logic (specified in `docs/tasks/phase-7/README.md` §4). The pieces it reuses: `TextViewRenderer.TryGetHorizontalRange` (`src/LoomEditor.Rendering/TextViewRenderer.Reveal.cs`, measures a column range of a view line),
`ILineHitTester` (`src/LoomEditor.View/Controller/ILineHitTester.cs`), `ViewLayout` (`GetVerticalOffsetForLineNumber`, `GetLineHeightForLineNumber`, `GetLineNumberAtVerticalOffset`, `GetLinesViewportData`,
`GetCurrentScrollTop`, `GetCurrentScrollLeft`) and `ViewModel.LayoutInfo` (`ContentLeft`, `ContentWidth`, `Height`; check the exact member names in `EditorLayoutInfo.cs`).

## Files to create or change

- `src/LoomEditor.View/Accessibility/IAutomationLineMetrics.cs` — create
- `src/LoomEditor.View/Accessibility/AutomationRect.cs` — create
- `src/LoomEditor.View/Accessibility/AutomationTextGeometry.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/FixedWidthLineMetrics.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AutomationTextGeometryTests.cs` — create

## Design decisions (already made — do not change)

- ```csharp
  internal interface IAutomationLineMetrics : ILineHitTester
  {
      /// Measures the characters startColumn..endColumn of a VIEW line, in DIPs from the start of the line's content. false when the line does not exist.
      bool TryGetHorizontalRange(int viewLineNumber, int startColumn, int endColumn, out double left, out double width);
  }
  internal readonly record struct AutomationRect(double X, double Y, double Width, double Height);   // editor-relative DIPs (the control's coordinates)
  ```
- `AutomationTextGeometry(AutomationTextDocument document, IAutomationLineMetrics metrics)` with:
  - `IReadOnlyList<AutomationRect> GetBoundingRectangles(Range modelRange)`: convert to a view range with `CoordinatesConverter.ConvertModelRangeToViewRange`; for each **visible** view line `v`
    (`ViewLayout.GetLinesViewportData()`: its first and last line numbers; read its members) that the view range touches:
    `startColumn = v == range.StartLineNumber ? range.StartColumn : GetLineMinColumn(v)`, `endColumn = v == range.EndLineNumber ? range.EndColumn : GetLineMaxColumn(v)`;
    measure with `TryGetHorizontalRange`; `width += FontInfo.SpaceWidth` when the range continues after this view line (`v < range.EndLineNumber`: the line break is selected, like the renderer draws it);
    `X = LayoutInfo.ContentLeft - scrollLeft + left`, `Y = ViewLayout.GetVerticalOffsetForLineNumber(v) - scrollTop`, `Height = ViewLayout.GetLineHeightForLineNumber(v)`.
    Each rectangle is **clipped** to the content area `[ContentLeft, ContentLeft + ContentWidth] × [0, LayoutInfo.Height]` (a rectangle fully outside is dropped, a partly visible one is cut).
    A **degenerate** range gives exactly one rectangle of width `1` (a caret) if its line is visible, none otherwise. A segment of width `0` of a non-degenerate range (an empty line) is **kept** with width `FontInfo.SpaceWidth` if it ends with a line break, else dropped.
    Rectangles come in top-to-bottom order.
  - `Range GetVisibleRange()`: the model range of the visible view lines: from `(first, GetLineMinColumn(first))` to `(last, GetLineMaxColumn(last))` converted with `ConvertViewRangeToModelRange`. When no line is visible: the collapsed range at `DocumentStart`.
  - `Position PositionFromPoint(double x, double y)`: `scrollTop`/`scrollLeft` as above; `v = ViewLayout.GetLineNumberAtVerticalOffset(y + scrollTop)` clamped to `[1, ViewModel.GetLineCount()]`;
    `offset = Math.Max(0, x - LayoutInfo.ContentLeft + scrollLeft)`; `viewPosition = metrics.GetPositionAtCoordinate(v, offset) ?? new Position(v, GetLineMinColumn(v))`;
    return `CoordinatesConverter.ConvertViewPositionToModelPosition(viewPosition)`. (UIA asks for the text range nearest to a point; the nearest boundary is what the hit test returns.)
- Everything is in DIPs relative to the control; conversion to screen pixels is `ScreenCoordinates` in WinUI (P7-17). No WinUI types here.
- `FixedWidthLineMetrics` (test helper): `CharacterWidth` (default `10`), `TryGetHorizontalRange` = `left = (startColumn - 1) * width`, `width = (endColumn - startColumn) * width`; `GetPositionAtCoordinate(line, x)` = `new Position(line, (int)Math.Round(x / width) + 1)`
  clamped to the line's max column of the **view model** (pass the view model to its constructor); `GetLineWidth` = `GetLineLength * width`.
- `internal sealed`, XML doc + `Threading: UI thread only` remark.

## Tests

(Do not hard-code pixel offsets of the gutter: compute `ContentLeft`, line height and `SpaceWidth` from `viewModel.LayoutInfo`, `viewModel.ViewLayout` and `viewModel.FontInfo`.)

- [x] New: `Rectangles_WithinOneLine` (`"hello world"`, `[(1,1),(1,6))`: one rectangle, `X == ContentLeft`, `Width == 50`, `Height == lineHeight`, `Y == 0` at scroll 0), `Rectangles_StartAfterTheFirstColumn` (`X == ContentLeft + 10 * (col - 1)`),
      `Rectangles_AcrossTwoLines_OnePerLineWithTheLineBreakWidth` (the first rectangle's width includes `SpaceWidth`), `Rectangles_Degenerate_IsOneRectangleOfWidthOne`,
      `Rectangles_EmptyLineInTheRange_KeepsTheLineBreakWidth`, `Rectangles_WrappedLine_OnePerViewLine` (wrap column 10, `"aaaa bbbb cccc"`, whole model line → 2 rectangles with different `Y`),
      `Rectangles_ScrolledOffLines_AreDropped` (a 100-line document, `viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(scrollTop: 19 * 50), ScrollType.Immediate)` — use the helper the existing layout tests use —, a range on line 1 → none, on the first visible line → `Y == 0`),
      `Rectangles_ClippedToTheContentArea` (horizontal scroll cuts the left of a rectangle: `X >= ContentLeft`), `Rectangles_RangeFullyRightOfTheContentArea_IsDropped`
- [x] New: `VisibleRange_FirstScreen` (a 100-line document: from `(1,1)` to the end of the last visible line), `VisibleRange_Scrolled`, `VisibleRange_Wrapped_ConvertsViewLinesToModelPositions`, `VisibleRange_EmptyViewport_IsDocumentStart` (`OuterHeight: 0`)
- [x] New: `PositionFromPoint_InsideALine` (`x = ContentLeft + 26` → `Math.Round(2.6) = 3` → `(1,4)` with the fixed metrics), `PositionFromPoint_SecondLine` (`y = lineHeight + 1`), `PositionFromPoint_LeftOfTheContent_IsTheLineStart`,
      `PositionFromPoint_BelowTheLastLine_IsTheLastLine`, `PositionFromPoint_AboveTheFirstLine_IsTheFirstLine`, `PositionFromPoint_WrappedLine_ReturnsAModelPosition` (a click on the second view line gives a model column past the break),
      `PositionFromPoint_Scrolled_AddsTheScrollOffsets`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The renderer-backed `IAutomationLineMetrics` and the DIP → screen conversion (P7-17), scrolling a range into view (P7-18), RTL and bidirectional text.

## Suggested commit message

`feat(view): compute automation bounding rectangles and points`
