// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLinesViewportData.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Layout;

/// <summary>Everything needed to render one viewport. Monaco: <c>ViewportData</c>.</summary>
/// <remarks>
/// Threading: immutable after creation (the arrays are not modified again); UI thread only because it is built from the
/// model. Not ported: <c>getViewLineRenderingData</c>, <c>getViewLineContinuesWithWrappedLine</c> (rendering data comes
/// in a later phase). <c>getDecorationsInViewport</c> is ported (P5-07).
/// </remarks>
internal sealed class ViewportData
{
    private readonly TextModel _model;
    private readonly ViewModelDecorations _decorations;

    public ViewportData(
        IReadOnlyList<Selection> selections,
        PartialViewLinesViewportData partialData,
        IReadOnlyList<ViewWhitespaceViewportData> whitespaceViewportData,
        TextModel model,
        ViewModelDecorations decorations)
    {
        _model = model;
        _decorations = decorations;
        Selections = selections;
        StartLineNumber = partialData.StartLineNumber;
        EndLineNumber = partialData.EndLineNumber;
        RelativeVerticalOffset = partialData.RelativeVerticalOffset;
        BigNumbersDelta = partialData.BigNumbersDelta;
        LineHeight = partialData.LineHeight;
        WhitespaceViewportData = whitespaceViewportData;

        VisibleRange = new Range(
            partialData.StartLineNumber,
            model.GetLineMinColumn(partialData.StartLineNumber),
            partialData.EndLineNumber,
            model.GetLineMaxColumn(partialData.EndLineNumber));
    }

    /// <summary>The tokens of a line, computed when asked (Monaco: <c>getViewLineData</c>); view lines are model lines.</summary>
    public IViewLineTokens GetTokens(int lineNumber) => _model.Tokenization.GetLineTokens(lineNumber).Inflate();

    /// <summary>The decorations of the viewport, in view coordinates (Monaco: <c>getDecorationsInViewport</c>).</summary>
    public IReadOnlyList<ViewModelDecoration> GetDecorationsInViewport() =>
        _decorations.GetDecorationsViewportData(VisibleRange).Decorations;

    /// <summary>The inline decorations of a visible line (Monaco: the <c>inlineDecorations</c> of the line rendering data).</summary>
    public IReadOnlyList<InlineDecoration> GetInlineDecorations(int lineNumber) =>
        _decorations.GetDecorationsViewportData(VisibleRange).InlineDecorations[lineNumber - VisibleRange.StartLineNumber];

    /// <summary>The selections (one for now).</summary>
    public IReadOnlyList<Selection> Selections { get; }

    /// <summary>The line number at which to start rendering (inclusive).</summary>
    public int StartLineNumber { get; }

    /// <summary>The line number at which to end rendering (inclusive).</summary>
    public int EndLineNumber { get; }

    /// <summary><c>RelativeVerticalOffset[i]</c> is the <c>top</c> position of line <c>i + StartLineNumber</c>.</summary>
    public int[] RelativeVerticalOffset { get; }

    /// <summary>The viewport as a range: (start, 1) to (end, max column of end).</summary>
    public Range VisibleRange { get; }

    /// <summary>Value to be subtracted from <c>scrollTop</c> (to keep vertical offsets small).</summary>
    public int BigNumbersDelta { get; }

    /// <summary>The default height of a line.</summary>
    public int LineHeight { get; }

    /// <summary>Positioning information about whitespace gaps.</summary>
    public IReadOnlyList<ViewWhitespaceViewportData> WhitespaceViewportData { get; }
}
