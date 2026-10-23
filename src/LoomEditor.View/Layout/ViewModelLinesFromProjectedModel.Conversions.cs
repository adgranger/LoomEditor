// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel: conversions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

// Coordinate conversions between model and view lines, decorations of a view range and position normalization.
internal sealed partial class ViewModelLinesFromProjectedModel
{
    /// <summary>The converter that forwards to this class (Monaco: <c>createCoordinatesConverter</c>).</summary>
    public ICoordinatesConverter CreateCoordinatesConverter() => new CoordinatesConverter(this);

    /// <summary>Clamps a view position to the view line and checks it still maps to <paramref name="expectedModelPosition"/>, else converts that one.</summary>
    public Position ValidateViewPosition(int viewLineNumber, int viewColumn, Position expectedModelPosition)
    {
        viewLineNumber = ToValidViewLineNumber(viewLineNumber);

        PrefixSumIndexOfResult r = _projectedModelLineLineCounts.GetIndexOf(viewLineNumber - 1);
        int lineIndex = r.Index;
        int remainder = r.Remainder;

        IModelLineProjection line = _modelLineProjections[lineIndex];

        int minColumn = line.GetViewLineMinColumn(_model, lineIndex + 1, remainder);
        int maxColumn = line.GetViewLineMaxColumn(_model, lineIndex + 1, remainder);
        if (viewColumn < minColumn)
        {
            viewColumn = minColumn;
        }

        if (viewColumn > maxColumn)
        {
            viewColumn = maxColumn;
        }

        int computedModelColumn = line.GetModelColumnOfViewPosition(remainder, viewColumn);
        Position computedModelPosition = _model.ValidatePosition(lineIndex + 1, computedModelColumn);

        if (computedModelPosition == expectedModelPosition)
        {
            return new Position(viewLineNumber, viewColumn);
        }

        return ConvertModelPositionToViewPosition(expectedModelPosition.LineNumber, expectedModelPosition.Column);
    }

    public Range ValidateViewRange(Range viewRange, Range expectedModelRange)
    {
        Position validViewStart = ValidateViewPosition(viewRange.StartLineNumber, viewRange.StartColumn, expectedModelRange.Start);
        Position validViewEnd = ValidateViewPosition(viewRange.EndLineNumber, viewRange.EndColumn, expectedModelRange.End);
        return new Range(validViewStart.LineNumber, validViewStart.Column, validViewEnd.LineNumber, validViewEnd.Column);
    }

    public Position ConvertViewPositionToModelPosition(int viewLineNumber, int viewColumn)
    {
        ViewLineInfo info = GetViewLineInfo(viewLineNumber);

        int inputColumn = _modelLineProjections[info.ModelLineNumber - 1].GetModelColumnOfViewPosition(info.ModelLineWrappedLineIdx, viewColumn);
        return _model.ValidatePosition(info.ModelLineNumber, inputColumn);
    }

    public Range ConvertViewRangeToModelRange(Range viewRange)
    {
        Position start = ConvertViewPositionToModelPosition(viewRange.StartLineNumber, viewRange.StartColumn);
        Position end = ConvertViewPositionToModelPosition(viewRange.EndLineNumber, viewRange.EndColumn);
        return new Range(start.LineNumber, start.Column, end.LineNumber, end.Column);
    }

    /// <summary>
    /// The view position of a model position. In a hidden area it is the end of the previous visible line, or the start of the
    /// next one with <paramref name="belowHiddenRanges"/>; when no line can be reached it is (1, 1). <paramref name="allowZeroLineNumber"/> is accepted but ignored (<see cref="Position"/> cannot hold line 0).
    /// </summary>
    public Position ConvertModelPositionToViewPosition(
        int modelLineNumber,
        int modelColumn,
        PositionAffinity affinity = PositionAffinity.None,
        bool allowZeroLineNumber = false,
        bool belowHiddenRanges = false)
    {
        Position validPosition = _model.ValidatePosition(modelLineNumber, modelColumn);
        int inputLineNumber = validPosition.LineNumber;
        int inputColumn = validPosition.Column;

        int lineIndex = inputLineNumber - 1;
        bool lineIndexChanged = false;
        if (belowHiddenRanges)
        {
            while (lineIndex < _modelLineProjections.Count && !_modelLineProjections[lineIndex].IsVisible)
            {
                lineIndex++;
                lineIndexChanged = true;
            }
        }
        else
        {
            while (lineIndex > 0 && !_modelLineProjections[lineIndex].IsVisible)
            {
                lineIndex--;
                lineIndexChanged = true;
            }
        }

        if (lineIndex == 0 && !_modelLineProjections[lineIndex].IsVisible)
        {
            // Could not reach a real line
            // Behavior difference: Monaco returns line 0 with allowZeroLineNumber; Position cannot hold it, so the flag is ignored.
            return new Position(1, 1);
        }

        int deltaLineNumber = 1 + _projectedModelLineLineCounts.GetPrefixSum(lineIndex);

        if (lineIndexChanged)
        {
            return belowHiddenRanges
                ? _modelLineProjections[lineIndex].GetViewPositionOfModelPosition(deltaLineNumber, 1, affinity)
                : _modelLineProjections[lineIndex].GetViewPositionOfModelPosition(deltaLineNumber, _model.GetLineMaxColumn(lineIndex + 1), affinity);
        }

        return _modelLineProjections[inputLineNumber - 1].GetViewPositionOfModelPosition(deltaLineNumber, inputColumn, affinity);
    }

    /// <param name="modelRange">The model range.</param>
    /// <param name="affinity">The affinity in case of an empty range. Has no effect for non-empty ranges.</param>
    public Range ConvertModelRangeToViewRange(Range modelRange, PositionAffinity affinity = PositionAffinity.Left)
    {
        if (modelRange.IsEmpty)
        {
            Position start = ConvertModelPositionToViewPosition(modelRange.StartLineNumber, modelRange.StartColumn, affinity);
            return Range.Collapsed(start);
        }
        else
        {
            Position start = ConvertModelPositionToViewPosition(modelRange.StartLineNumber, modelRange.StartColumn, PositionAffinity.Right);
            Position end = ConvertModelPositionToViewPosition(modelRange.EndLineNumber, modelRange.EndColumn, PositionAffinity.Left);
            return new Range(start.LineNumber, start.Column, end.LineNumber, end.Column);
        }
    }

    public int GetViewLineNumberOfModelPosition(int modelLineNumber, int modelColumn)
    {
        int lineIndex = modelLineNumber - 1;
        if (_modelLineProjections[lineIndex].IsVisible)
        {
            // this model line is visible
            int visibleDeltaLineNumber = 1 + _projectedModelLineLineCounts.GetPrefixSum(lineIndex);
            return _modelLineProjections[lineIndex].GetViewLineNumberOfModelPosition(visibleDeltaLineNumber, modelColumn);
        }

        // this model line is not visible
        while (lineIndex > 0 && !_modelLineProjections[lineIndex].IsVisible)
        {
            lineIndex--;
        }

        if (lineIndex == 0 && !_modelLineProjections[lineIndex].IsVisible)
        {
            // Could not reach a real line
            return 1;
        }

        int deltaLineNumber = 1 + _projectedModelLineLineCounts.GetPrefixSum(lineIndex);
        return _modelLineProjections[lineIndex].GetViewLineNumberOfModelPosition(deltaLineNumber, _model.GetLineMaxColumn(lineIndex + 1));
    }

    /// <summary>
    /// The decorations of a view range. Behavior difference: Monaco's <c>onlyMarginDecorations</c> is not ported
    /// (glyph margin lanes).
    /// </summary>
    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(Range range, int ownerId, bool filterOutValidation, bool filterFontDecorations, bool onlyMinimapDecorations = false)
    {
        Position modelStart = ConvertViewPositionToModelPosition(range.StartLineNumber, range.StartColumn);
        Position modelEnd = ConvertViewPositionToModelPosition(range.EndLineNumber, range.EndColumn);

        if (modelEnd.LineNumber - modelStart.LineNumber <= range.EndLineNumber - range.StartLineNumber)
        {
            // most likely there are no hidden lines => fast path
            // fetch decorations from column 1 to cover the case of wrapped lines that have whole line decorations at column 1
            return _model.GetDecorationsInRange(new Range(modelStart.LineNumber, 1, modelEnd.LineNumber, modelEnd.Column), ownerId, filterOutValidation, filterFontDecorations, onlyMinimapDecorations);
        }

        List<ModelDecoration> result = [];
        int modelStartLineIndex = modelStart.LineNumber - 1;
        int modelEndLineIndex = modelEnd.LineNumber - 1;

        Position? reqStart = null;
        for (int modelLineIndex = modelStartLineIndex; modelLineIndex <= modelEndLineIndex; modelLineIndex++)
        {
            IModelLineProjection line = _modelLineProjections[modelLineIndex];
            if (line.IsVisible)
            {
                // merge into previous request
                reqStart ??= new Position(modelLineIndex + 1, modelLineIndex == modelStartLineIndex ? modelStart.Column : 1);
            }
            else if (reqStart is { } start)
            {
                // hit invisible line => flush request
                int maxLineColumn = _model.GetLineMaxColumn(modelLineIndex);
                result.AddRange(_model.GetDecorationsInRange(new Range(start.LineNumber, start.Column, modelLineIndex, maxLineColumn), ownerId, filterOutValidation, filterFontDecorations, onlyMinimapDecorations));
                reqStart = null;
            }
        }

        if (reqStart is { } lastStart)
        {
            result.AddRange(_model.GetDecorationsInRange(new Range(lastStart.LineNumber, lastStart.Column, modelEnd.LineNumber, modelEnd.Column), ownerId, filterOutValidation, filterFontDecorations, onlyMinimapDecorations));
        }

        // OrderBy is stable, like Array.prototype.sort
        List<ModelDecoration> sorted =
        [
            .. result
                .OrderBy(d => d.Range, Comparer<Range>.Create(Range.CompareUsingStarts))
                .ThenBy(d => d.Id, StringComparer.Ordinal),
        ];

        // Eliminate duplicate decorations that might have intersected our visible ranges multiple times
        List<ModelDecoration> finalResult = [];
        string? prevDecId = null;
        foreach (ModelDecoration dec in sorted)
        {
            if (prevDecId == dec.Id)
            {
                // skip
                continue;
            }

            prevDecId = dec.Id;
            finalResult.Add(dec);
        }

        return finalResult;
    }

    public Position NormalizePosition(Position position, PositionAffinity affinity)
    {
        ViewLineInfo info = GetViewLineInfo(position.LineNumber);
        return _modelLineProjections[info.ModelLineNumber - 1].NormalizePosition(info.ModelLineWrappedLineIdx, position, affinity);
    }

    /// <summary>The injected text at a view position, if any.</summary>
    public InjectedText? GetInjectedTextAt(Position viewPosition)
    {
        ViewLineInfo info = GetViewLineInfo(viewPosition.LineNumber);
        return _modelLineProjections[info.ModelLineNumber - 1].GetInjectedTextAt(info.ModelLineWrappedLineIdx, viewPosition.Column);
    }

    public int GetLineIndentColumn(int lineNumber)
    {
        ViewLineInfo info = GetViewLineInfo(lineNumber);
        if (info.ModelLineWrappedLineIdx == 0)
        {
            return _model.GetLineIndentColumn(info.ModelLineNumber);
        }

        // wrapped lines have no indentation.
        // We deliberately don't handle the case that indentation is wrapped
        // to avoid two view lines reporting indentation for the very same model line.
        return 0;
    }
}
