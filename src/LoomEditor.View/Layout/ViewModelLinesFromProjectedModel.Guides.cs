// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel: indent guides)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

// The indent guides of view lines and the active indent guide in view line numbers.
internal sealed partial class ViewModelLinesFromProjectedModel
{
    private enum IndentGuideRepeatOption
    {
        BlockNone = 0,
        BlockSubsequent = 1,
        BlockAll = 2,
    }

    /// <summary>The active indent guide around a view line, in view line numbers (Monaco: <c>getActiveIndentGuide</c>).</summary>
    public ActiveIndentGuideInfo GetActiveIndentGuide(int viewLineNumber, int minLineNumber, int maxLineNumber)
    {
        viewLineNumber = ToValidViewLineNumber(viewLineNumber);
        minLineNumber = ToValidViewLineNumber(minLineNumber);
        maxLineNumber = ToValidViewLineNumber(maxLineNumber);

        Position modelPosition = ConvertViewPositionToModelPosition(viewLineNumber, GetViewLineMinColumn(viewLineNumber));
        Position modelMinPosition = ConvertViewPositionToModelPosition(minLineNumber, GetViewLineMinColumn(minLineNumber));
        Position modelMaxPosition = ConvertViewPositionToModelPosition(maxLineNumber, GetViewLineMinColumn(maxLineNumber));
        ActiveIndentGuideInfo result = _model.Guides.GetActiveIndentGuide(modelPosition.LineNumber, modelMinPosition.LineNumber, modelMaxPosition.LineNumber);

        Position viewStartPosition = ConvertModelPositionToViewPosition(result.StartLineNumber, 1);
        Position viewEndPosition = ConvertModelPositionToViewPosition(result.EndLineNumber, _model.GetLineMaxColumn(result.EndLineNumber));
        return new ActiveIndentGuideInfo(viewStartPosition.LineNumber, viewEndPosition.LineNumber, result.Indent);
    }

    private Position GetModelStartPositionOfViewLine(ViewLineInfo viewLineInfo)
    {
        IModelLineProjection line = _modelLineProjections[viewLineInfo.ModelLineNumber - 1];
        int minViewColumn = line.GetViewLineMinColumn(_model, viewLineInfo.ModelLineNumber, viewLineInfo.ModelLineWrappedLineIdx);
        int column = line.GetModelColumnOfViewPosition(viewLineInfo.ModelLineWrappedLineIdx, minViewColumn);
        return new Position(viewLineInfo.ModelLineNumber, column);
    }

    private Position GetModelEndPositionOfViewLine(ViewLineInfo viewLineInfo)
    {
        IModelLineProjection line = _modelLineProjections[viewLineInfo.ModelLineNumber - 1];
        int maxViewColumn = line.GetViewLineMaxColumn(_model, viewLineInfo.ModelLineNumber, viewLineInfo.ModelLineWrappedLineIdx);
        int column = line.GetModelColumnOfViewPosition(viewLineInfo.ModelLineWrappedLineIdx, maxViewColumn);
        return new Position(viewLineInfo.ModelLineNumber, column);
    }

    private List<ViewLineInfoGroupedByModelRange> GetViewLineInfosGroupedByModelRanges(int viewStartLineNumber, int viewEndLineNumber)
    {
        ViewLineInfo startViewLine = GetViewLineInfo(viewStartLineNumber);
        ViewLineInfo endViewLine = GetViewLineInfo(viewEndLineNumber);

        List<ViewLineInfoGroupedByModelRange> result = [];
        Position? lastVisibleModelPos = GetModelStartPositionOfViewLine(startViewLine);
        List<ViewLineInfo> viewLines = [];

        for (int curModelLine = startViewLine.ModelLineNumber; curModelLine <= endViewLine.ModelLineNumber; curModelLine++)
        {
            IModelLineProjection line = _modelLineProjections[curModelLine - 1];

            if (line.IsVisible)
            {
                int startOffset = curModelLine == startViewLine.ModelLineNumber ? startViewLine.ModelLineWrappedLineIdx : 0;
                int endOffset = curModelLine == endViewLine.ModelLineNumber ? endViewLine.ModelLineWrappedLineIdx + 1 : line.GetViewLineCount();

                for (int i = startOffset; i < endOffset; i++)
                {
                    viewLines.Add(new ViewLineInfo(curModelLine, i));
                }
            }

            if (!line.IsVisible && lastVisibleModelPos is not null)
            {
                Position lastVisibleModelPos2 = new(curModelLine - 1, _model.GetLineMaxColumn(curModelLine - 1) + 1);

                result.Add(new ViewLineInfoGroupedByModelRange(new Range(lastVisibleModelPos.Value, lastVisibleModelPos2), viewLines));
                viewLines = [];

                lastVisibleModelPos = null;
            }
            else if (line.IsVisible && lastVisibleModelPos is null)
            {
                lastVisibleModelPos = new Position(curModelLine, 1);
            }
        }

        if (lastVisibleModelPos is not null)
        {
            result.Add(new ViewLineInfoGroupedByModelRange(new Range(lastVisibleModelPos.Value, GetModelEndPositionOfViewLine(endViewLine)), viewLines));
        }

        return result;
    }

    /// <summary>
    /// The bracket guides of each view line of a range, in view columns (Monaco: <c>getViewLinesBracketGuides</c>). Guides with a
    /// visible column stay as they are (like Monaco, wrapped lines do not shift them).
    /// </summary>
    public List<IndentGuide>[] GetViewLinesBracketGuides(int viewStartLineNumber, int viewEndLineNumber, Position? activeViewPosition, BracketGuideOptions options)
    {
        Position? modelActivePosition = activeViewPosition is { } active ? ConvertViewPositionToModelPosition(active.LineNumber, active.Column) : null;
        List<List<IndentGuide>> resultPerViewLine = [];

        foreach (ViewLineInfoGroupedByModelRange group in GetViewLineInfosGroupedByModelRanges(viewStartLineNumber, viewEndLineNumber))
        {
            int modelRangeStartLineNumber = group.ModelRange.StartLineNumber;
            List<IndentGuide>[] bracketGuidesPerModelLine = _model.Guides.GetLinesBracketGuides(
                modelRangeStartLineNumber, group.ModelRange.EndLineNumber, modelActivePosition, options);

            foreach (ViewLineInfo viewLineInfo in group.ViewLines)
            {
                List<IndentGuide> bracketGuides = bracketGuidesPerModelLine[viewLineInfo.ModelLineNumber - modelRangeStartLineNumber];
                IModelLineProjection projection = _modelLineProjections[viewLineInfo.ModelLineNumber - 1];
                List<IndentGuide> result = [];
                foreach (IndentGuide g in bracketGuides)
                {
                    IndentGuide? mapped = MapBracketGuide(g, viewLineInfo, projection);
                    if (mapped is not null)
                    {
                        result.Add(mapped);
                    }
                }

                resultPerViewLine.Add(result);
            }
        }

        return [.. resultPerViewLine];
    }

    private IndentGuide? MapBracketGuide(IndentGuide g, ViewLineInfo viewLineInfo, IModelLineProjection projection)
    {
        // Monaco passes a delta of 0 and compares 0-based indexes; a Position cannot hold line 0, so use a delta of 1 and 1-based lines.
        int wrappedIdx = viewLineInfo.ModelLineWrappedLineIdx + 1;
        if (g.ForWrappedLinesAfterColumn != -1
            && projection.GetViewPositionOfModelPosition(1, g.ForWrappedLinesAfterColumn).LineNumber >= wrappedIdx)
        {
            return null;
        }

        if (g.ForWrappedLinesBeforeOrAtColumn != -1
            && projection.GetViewPositionOfModelPosition(1, g.ForWrappedLinesBeforeOrAtColumn).LineNumber < wrappedIdx)
        {
            return null;
        }

        if (g.HorizontalLine is not { } horizontalLine)
        {
            return g;
        }

        int column = -1;
        if (g.Column != -1)
        {
            Position p = projection.GetViewPositionOfModelPosition(1, g.Column);
            if (p.LineNumber == wrappedIdx)
            {
                column = p.Column;
            }
            else if (p.LineNumber < wrappedIdx)
            {
                column = GetMinColumnOfViewLine(viewLineInfo);
            }
            else
            {
                return null;
            }
        }

        Position viewPosition = ConvertModelPositionToViewPosition(viewLineInfo.ModelLineNumber, horizontalLine.EndColumn);
        Position end = projection.GetViewPositionOfModelPosition(1, horizontalLine.EndColumn);
        if (end.LineNumber == wrappedIdx)
        {
            return new IndentGuide(g.VisibleColumn, column, g.ClassName, new IndentGuideHorizontalLine(horizontalLine.Top, viewPosition.Column), -1, -1);
        }

        if (end.LineNumber < wrappedIdx)
        {
            return null;
        }

        if (g.VisibleColumn != -1)
        {
            // Don't repeat horizontal lines that use visibleColumn for unrelated lines.
            return null;
        }

        return new IndentGuide(g.VisibleColumn, column, g.ClassName, new IndentGuideHorizontalLine(horizontalLine.Top, GetMaxColumnOfViewLine(viewLineInfo)), -1, -1);
    }

    /// <summary>The indent guide of each view line of a range; wrapped continuations repeat the guide of their model line, hidden lines are skipped (Monaco: <c>getViewLinesIndentGuides</c>).</summary>
    public int[] GetViewLinesIndentGuides(int viewStartLineNumber, int viewEndLineNumber)
    {
        viewStartLineNumber = ToValidViewLineNumber(viewStartLineNumber);
        viewEndLineNumber = ToValidViewLineNumber(viewEndLineNumber);

        Position modelStart = ConvertViewPositionToModelPosition(viewStartLineNumber, GetViewLineMinColumn(viewStartLineNumber));
        Position modelEnd = ConvertViewPositionToModelPosition(viewEndLineNumber, GetViewLineMaxColumn(viewEndLineNumber));

        List<int> result = [];
        List<int> resultRepeatCount = [];
        List<IndentGuideRepeatOption> resultRepeatOption = [];
        int modelStartLineIndex = modelStart.LineNumber - 1;
        int modelEndLineIndex = modelEnd.LineNumber - 1;

        int? reqStart = null;
        for (int modelLineIndex = modelStartLineIndex; modelLineIndex <= modelEndLineIndex; modelLineIndex++)
        {
            IModelLineProjection line = _modelLineProjections[modelLineIndex];
            if (line.IsVisible)
            {
                int viewLineStartIndex = line.GetViewLineNumberOfModelPosition(0, modelLineIndex == modelStartLineIndex ? modelStart.Column : 1);
                int viewLineEndIndex = line.GetViewLineNumberOfModelPosition(0, _model.GetLineMaxColumn(modelLineIndex + 1));
                int count = viewLineEndIndex - viewLineStartIndex + 1;
                IndentGuideRepeatOption option = IndentGuideRepeatOption.BlockNone;
                if (count > 1 && line.GetViewLineMinColumn(_model, modelLineIndex + 1, viewLineEndIndex) == 1)
                {
                    // wrapped lines should block indent guides
                    option = viewLineStartIndex == 0 ? IndentGuideRepeatOption.BlockSubsequent : IndentGuideRepeatOption.BlockAll;
                }

                resultRepeatCount.Add(count);
                resultRepeatOption.Add(option);
                // merge into previous request
                reqStart ??= modelLineIndex + 1;
            }
            else if (reqStart is not null)
            {
                // hit invisible line => flush request
                result.AddRange(_model.Guides.GetLinesIndentGuides(reqStart.Value, modelLineIndex));
                reqStart = null;
            }
        }

        if (reqStart is not null)
        {
            result.AddRange(_model.Guides.GetLinesIndentGuides(reqStart.Value, modelEnd.LineNumber));
        }

        int viewLineCount = viewEndLineNumber - viewStartLineNumber + 1;
        int[] viewIndents = new int[viewLineCount];
        int currIndex = 0;
        for (int i = 0; i < result.Count; i++)
        {
            int value = result[i];
            int count = Math.Min(viewLineCount - currIndex, resultRepeatCount[i]);
            int blockAtIndex = resultRepeatOption[i] switch
            {
                IndentGuideRepeatOption.BlockAll => 0,
                IndentGuideRepeatOption.BlockSubsequent => 1,
                _ => count,
            };

            for (int j = 0; j < count; j++)
            {
                if (j == blockAtIndex)
                {
                    value = 0;
                }

                viewIndents[currIndex++] = value;
            }
        }

        return viewIndents;
    }
}
