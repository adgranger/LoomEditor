// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>The lines that are (partially) visible in a viewport, with their offsets relative to the viewport.</summary>
/// <remarks>Threading: immutable after creation, except for the array contents which the creator does not modify again.</remarks>
/// <param name="BigNumbersDelta">Value to be subtracted from <c>scrollTop</c> (in order to keep vertical offsets small).</param>
/// <param name="StartLineNumber">The first (partially) visible line number.</param>
/// <param name="EndLineNumber">The last (partially) visible line number.</param>
/// <param name="RelativeVerticalOffset"><c>RelativeVerticalOffset[i]</c> is the <c>top</c> position of line <c>i + StartLineNumber</c>.</param>
/// <param name="CenteredLineNumber">The centered line in the viewport.</param>
/// <param name="CompletelyVisibleStartLineNumber">The first completely visible line number.</param>
/// <param name="CompletelyVisibleEndLineNumber">The last completely visible line number.</param>
/// <param name="LineHeight">The default height of a line.</param>
internal sealed record PartialViewLinesViewportData(
    int BigNumbersDelta,
    int StartLineNumber,
    int EndLineNumber,
    int[] RelativeVerticalOffset,
    int CenteredLineNumber,
    int CompletelyVisibleStartLineNumber,
    int CompletelyVisibleEndLineNumber,
    int LineHeight);
