// Ported from microsoft/vscode: src/vs/editor/browser/controller/mouseTarget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>
/// Finds what is under a point of the editor: a gutter area, a position in the text, or the empty space after a line.
/// </summary>
/// <remarks>
/// <para>
/// Monaco's <c>MouseTargetFactory</c> asks the DOM first. There is no DOM here: the target is found from the geometry
/// (<c>x &lt; contentLeft</c> is the gutter), as in Monaco's GPU path (<c>viewLinesGpu</c>). Not ported: widgets, view
/// zones, the minimap, scrollbars, injected text and right-to-left lines (<c>isRtl</c> is always <c>false</c>).
/// </para>
/// <para>Threading: UI thread only (it reads the view model).</para>
/// </remarks>
internal static class MouseTargetFactory
{
    /// <summary>Monaco: <c>MouseTargetFactory.createMouseTarget</c> for a point relative to the editor, in DIPs.</summary>
    /// <param name="viewModel">The view to hit-test.</param>
    /// <param name="lines">Measures the laid out lines.</param>
    /// <param name="x">X relative to the left of the editor.</param>
    /// <param name="y">Y relative to the top of the editor.</param>
    public static MouseTarget CreateMouseTarget(ViewModel viewModel, ILineHitTester lines, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(lines);

        return HitTest(viewModel, lines, x, y);
    }

    /// <summary>Monaco: <c>_getMouseColumn</c>: the column of the mouse counted in typical character widths.</summary>
    public static int GetMouseColumn(double mouseContentHorizontalOffset, double typicalHalfwidthCharacterWidth)
    {
        if (mouseContentHorizontalOffset < 0)
        {
            return 1;
        }

        // JavaScript's Math.round: .5 rounds up (phase 3 conventions, trap 1).
        int chars = (int)Math.Floor((mouseContentHorizontalOffset / typicalHalfwidthCharacterWidth) + 0.5);
        return chars + 1;
    }

    private static MouseTarget HitTest(ViewModel viewModel, ILineHitTester lines, double x, double y)
    {
        EditorLayoutInfo layoutInfo = viewModel.LayoutInfo;
        ViewLayout viewLayout = viewModel.ViewLayout;

        // Monaco: BareHitTestRequest.
        int mouseVerticalOffset = (int)Math.Max(0, viewLayout.GetCurrentScrollTop() + y);
        double mouseContentHorizontalOffset = viewLayout.GetCurrentScrollLeft() + x - layoutInfo.ContentLeft;
        bool isInMarginArea = x < layoutInfo.ContentLeft && x >= layoutInfo.GlyphMarginLeft;
        int mouseColumn = Math.Max(0, GetMouseColumn(mouseContentHorizontalOffset, viewModel.FontInfo.TypicalHalfwidthCharacterWidth));

        if (isInMarginArea)
        {
            return HitTestMargin(viewModel, layoutInfo, mouseVerticalOffset, x, mouseColumn);
        }

        return HitTestViewLines(viewModel, lines, mouseVerticalOffset, mouseContentHorizontalOffset, mouseColumn);
    }

    // Monaco: _hitTestMargin.
    private static MouseTarget HitTestMargin(ViewModel viewModel, EditorLayoutInfo layoutInfo, int mouseVerticalOffset, double x, int mouseColumn)
    {
        (Range range, bool isAfterLines) = GetFullLineRangeAtCoord(viewModel, mouseVerticalOffset);
        Position position = range.Start;
        double offset = Math.Abs(x);
        int column = GetMouseColumn(viewModel, position, mouseColumn);

        offset -= layoutInfo.GlyphMarginLeft;
        if (offset <= layoutInfo.GlyphMarginWidth)
        {
            return new MouseTarget(MouseTargetType.GutterGlyphMargin, position, column, range, isAfterLines, null);
        }

        offset -= layoutInfo.GlyphMarginWidth;
        if (offset <= layoutInfo.LineNumbersWidth)
        {
            return new MouseTarget(MouseTargetType.GutterLineNumbers, position, column, range, isAfterLines, null);
        }

        return new MouseTarget(MouseTargetType.GutterLineDecorations, position, column, range, isAfterLines, null);
    }

    // Monaco: _hitTestViewLines, the `viewLinesGpu` branch.
    private static MouseTarget HitTestViewLines(
        ViewModel viewModel, ILineHitTester lines, int mouseVerticalOffset, double mouseContentHorizontalOffset, int mouseColumn)
    {
        ViewLayout viewLayout = viewModel.ViewLayout;

        if (viewLayout.IsInTopPadding(mouseVerticalOffset))
        {
            var start = new Position(1, 1);
            return new MouseTarget(MouseTargetType.ContentEmpty, start, GetMouseColumn(viewModel, start, mouseColumn), null, true, null);
        }

        // Below any lines.
        if (viewLayout.IsAfterLines(mouseVerticalOffset) || viewLayout.IsInBottomPadding(mouseVerticalOffset))
        {
            // This most likely indicates it happened after the last view-line.
            int lineCount = viewModel.GetLineCount();
            var end = new Position(lineCount, viewModel.GetLineMaxColumn(lineCount));
            return new MouseTarget(MouseTargetType.ContentEmpty, end, GetMouseColumn(viewModel, end, mouseColumn), null, true, null);
        }

        int lineNumber = viewLayout.GetLineNumberAtVerticalOffset(mouseVerticalOffset);
        double lineWidth = lines.GetLineWidth(lineNumber);
        if (viewModel.GetLineLength(lineNumber) == 0)
        {
            var position = new Position(lineNumber, 1);
            return ContentEmpty(viewModel, position, mouseColumn, mouseContentHorizontalOffset - lineWidth);
        }

        if (mouseContentHorizontalOffset >= lineWidth)
        {
            var position = new Position(lineNumber, viewModel.GetLineMaxColumn(lineNumber));
            return ContentEmpty(viewModel, position, mouseColumn, mouseContentHorizontalOffset - lineWidth);
        }

        if (lines.GetPositionAtCoordinate(lineNumber, mouseContentHorizontalOffset) is { } hit)
        {
            Range range = Range.Collapsed(hit);
            if (viewModel.CursorConfig.StickyTabStops)
            {
                // Snap to the nearest soft tab boundary if atomic soft tabs are enabled.
                hit = SnapToSoftTabBoundary(hit, viewModel);
                range = Range.Collapsed(hit).Plus(range);
            }

            return new MouseTarget(MouseTargetType.ContentText, hit, GetMouseColumn(viewModel, hit, mouseColumn), range, false, null);
        }

        // We have tried everything...
        return new MouseTarget(MouseTargetType.Unknown, null, mouseColumn, null, false, null);
    }

    private static MouseTarget ContentEmpty(ViewModel viewModel, Position position, int mouseColumn, double horizontalDistanceToText) =>
        new(MouseTargetType.ContentEmpty, position, GetMouseColumn(viewModel, position, mouseColumn), null, false, horizontalDistanceToText);

    // Monaco: HitTestContext.getFullLineRangeAtCoord.
    private static (Range Range, bool IsAfterLines) GetFullLineRangeAtCoord(ViewModel viewModel, int mouseVerticalOffset)
    {
        if (viewModel.ViewLayout.IsAfterLines(mouseVerticalOffset))
        {
            // Below the last line.
            int lastLine = viewModel.GetLineCount();
            int maxLineColumn = viewModel.GetLineMaxColumn(lastLine);
            return (new Range(lastLine, maxLineColumn, lastLine, maxLineColumn), true);
        }

        int lineNumber = viewModel.ViewLayout.GetLineNumberAtVerticalOffset(mouseVerticalOffset);
        return (new Range(lineNumber, 1, lineNumber, viewModel.GetLineMaxColumn(lineNumber)), false);
    }

    // Monaco: HitTestRequest._getMouseColumn(position).
    private static int GetMouseColumn(ViewModel viewModel, Position position, int mouseColumn)
    {
        if (position.Column < viewModel.GetLineMaxColumn(position.LineNumber))
        {
            // Most likely, the line contains foreign decorations...
            return CursorColumns.VisibleColumnFromColumn(
                viewModel.GetLineContent(position.LineNumber), position.Column, viewModel.Model.Options.TabSize) + 1;
        }

        return mouseColumn;
    }

    // Monaco: _snapToSoftTabBoundary.
    private static Position SnapToSoftTabBoundary(Position position, ViewModel viewModel)
    {
        string lineContent = viewModel.GetLineContent(position.LineNumber);
        int newPosition = AtomicTabMoveOperations.AtomicPosition(
            lineContent, position.Column - 1, viewModel.Model.Options.TabSize, AtomicMoveDirection.Nearest);
        return newPosition != -1 ? new Position(position.LineNumber, newPosition + 1) : position;
    }
}
