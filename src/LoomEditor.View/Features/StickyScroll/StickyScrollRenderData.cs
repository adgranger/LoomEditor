// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts (_findRenderingData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>The model lines the sticky widget draws and how far the last one is pushed up. Monaco: the result of <c>_findRenderingData</c>.</summary>
/// <remarks>Threading: immutable by convention; UI thread only.</remarks>
internal sealed record StickyScrollRenderData(IReadOnlyList<int> LineNumbers, int LastLineRelativePosition)
{
    /// <summary>
    /// The data for a widget state, or <see langword="null"/> when there is nothing to draw (no state, or lines with no height).
    /// </summary>
    public static StickyScrollRenderData? From(StickyScrollWidgetState? state, ViewModel viewModel)
    {
        if (state is null)
        {
            return null;
        }

        int[] candidateLineNumbers = [.. state.StartLineNumbers];
        if (state.ShowEndForLine is { } showEndForLine)
        {
            candidateLineNumbers[showEndForLine] = state.EndLineNumbers[showEndForLine];
        }

        int totalHeight = 0;
        foreach (int lineNumber in candidateLineNumbers)
        {
            if (lineNumber <= viewModel.GetLineCount())
            {
                int viewLineNumber = viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(lineNumber, 1)).LineNumber;
                totalHeight += viewModel.ViewLayout.GetLineHeightForLineNumber(viewLineNumber);
            }
        }

        return totalHeight == 0 ? null : new StickyScrollRenderData(candidateLineNumbers, state.LastLineRelativePosition);
    }
}
