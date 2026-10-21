// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Model;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

/// <summary>
/// The view lines of a model: one projection per model line, the prefix sum of their view line counts and the hidden areas
/// (Monaco: <c>ViewModelLinesFromProjectedModel</c>).
/// </summary>
/// <remarks>
/// <para>
/// Phase 6 cut points: word wrapping (<c>createLineBreaksComputer</c>, the line break factories, <c>setWrappingSettings</c> and the
/// wrapping fields), injected text (<c>getInjectedTextAt</c>) and the indent and bracket guides (<c>getViewLinesIndentGuides</c>,
/// <c>getActiveIndentGuide</c>, <c>getViewLinesBracketGuides</c>). <c>IViewModelLines</c> and <c>ViewModelLinesFromModelAsIs</c> are
/// not ported (one implementation).
/// </para>
/// <para>
/// Behavior difference: the model change methods return the affected view line ranges instead of view events; the caller
/// builds the events. The hidden areas are model decorations owned by the editor id, like Monaco.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed partial class ViewModelLinesFromProjectedModel : IDisposable
{
    private readonly int _editorId;
    private readonly TextModel _model;
    private int _validModelVersionId = -1;
    private int _tabSize;
    private List<IModelLineProjection> _modelLineProjections = [];

    // Reflects the sum of the line counts of all projected model lines.
    private ConstantTimePrefixSumComputer _projectedModelLineLineCounts = null!;

    private string[] _hiddenAreasDecorationIds = [];

    public ViewModelLinesFromProjectedModel(int editorId, TextModel model, int tabSize)
    {
        ArgumentNullException.ThrowIfNull(model);

        _editorId = editorId;
        _model = model;
        _tabSize = tabSize;

        ConstructLines(resetHiddenAreas: true);
    }

    public void Dispose() => _hiddenAreasDecorationIds = _model.DeltaDecorations(_editorId, _hiddenAreasDecorationIds, []);

    private List<Range> GetSortedHiddenAreaDecorationRanges() =>
        [.. _hiddenAreasDecorationIds
            .Select(areaId => _model.GetDecorationRange(areaId)!.Value)
            .OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts))];

    private void ConstructLines(bool resetHiddenAreas)
    {
        _modelLineProjections = [];

        if (resetHiddenAreas)
        {
            _hiddenAreasDecorationIds = _model.DeltaDecorations(_editorId, _hiddenAreasDecorationIds, []);
        }

        int lineCount = _model.LineCount;
        List<int> values = [];

        List<Range> hiddenAreas = GetSortedHiddenAreaDecorationRanges();
        int hiddenAreaStart = 1;
        int hiddenAreaEnd = 0;
        int hiddenAreaIdx = -1;
        int nextLineNumberToUpdateHiddenArea = (hiddenAreaIdx + 1 < hiddenAreas.Count) ? hiddenAreaEnd + 1 : lineCount + 2;

        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = i + 1;

            if (lineNumber == nextLineNumberToUpdateHiddenArea)
            {
                hiddenAreaIdx++;
                hiddenAreaStart = hiddenAreas[hiddenAreaIdx].StartLineNumber;
                hiddenAreaEnd = hiddenAreas[hiddenAreaIdx].EndLineNumber;
                nextLineNumberToUpdateHiddenArea = (hiddenAreaIdx + 1 < hiddenAreas.Count) ? hiddenAreaEnd + 1 : lineCount + 2;
            }

            bool isInHiddenArea = lineNumber >= hiddenAreaStart && lineNumber <= hiddenAreaEnd;
            IModelLineProjection line = ModelLineProjection.Create(!isInHiddenArea);
            values.Add(line.GetViewLineCount());
            _modelLineProjections.Add(line);
        }

        _validModelVersionId = _model.VersionId;

        _projectedModelLineLineCounts = new ConstantTimePrefixSumComputer(values);

        EnsureAtLeastOneVisibleLine();
    }

    /// <summary>The hidden areas, as the model decorations track them now.</summary>
    public IReadOnlyList<Range> GetHiddenAreas() => [.. _hiddenAreasDecorationIds.Select(decId => _model.GetDecorationRange(decId)!.Value)];

    /// <summary>Hides the lines of <paramref name="ranges"/>; returns whether the line mapping changed.</summary>
    public bool SetHiddenAreas(IReadOnlyList<Range> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        List<Range> validatedRanges = [.. ranges.Select(_model.ValidateRange)];
        List<Range> newRanges = LineRanges.Normalize(validatedRanges);

        // This checks if there really was a change
        List<Range> oldRanges = GetSortedHiddenAreaDecorationRanges();
        if (newRanges.Count == oldRanges.Count)
        {
            bool hasDifference = false;
            for (int i = 0; i < newRanges.Count; i++)
            {
                if (newRanges[i] != oldRanges[i])
                {
                    hasDifference = true;
                    break;
                }
            }

            if (!hasDifference)
            {
                return false;
            }
        }

        _hiddenAreasDecorationIds = _model.DeltaDecorations(
            _editorId, _hiddenAreasDecorationIds, [.. newRanges.Select(r => new ModelDeltaDecoration(r, ModelDecorationOptions.Empty))]);

        List<Range> hiddenAreas = newRanges;
        int hiddenAreaStart = 1;
        int hiddenAreaEnd = 0;
        int hiddenAreaIdx = -1;
        int nextLineNumberToUpdateHiddenArea = (hiddenAreaIdx + 1 < hiddenAreas.Count) ? hiddenAreaEnd + 1 : _modelLineProjections.Count + 2;

        bool hasVisibleLine = false;
        for (int i = 0; i < _modelLineProjections.Count; i++)
        {
            int lineNumber = i + 1;

            if (lineNumber == nextLineNumberToUpdateHiddenArea)
            {
                hiddenAreaIdx++;
                hiddenAreaStart = hiddenAreas[hiddenAreaIdx].StartLineNumber;
                hiddenAreaEnd = hiddenAreas[hiddenAreaIdx].EndLineNumber;
                nextLineNumberToUpdateHiddenArea = (hiddenAreaIdx + 1 < hiddenAreas.Count) ? hiddenAreaEnd + 1 : _modelLineProjections.Count + 2;
            }

            bool lineChanged = false;
            if (lineNumber >= hiddenAreaStart && lineNumber <= hiddenAreaEnd)
            {
                // Line should be hidden
                if (_modelLineProjections[i].IsVisible)
                {
                    _modelLineProjections[i] = _modelLineProjections[i].SetVisible(false);
                    lineChanged = true;
                }
            }
            else
            {
                hasVisibleLine = true;

                // Line should be visible
                if (!_modelLineProjections[i].IsVisible)
                {
                    _modelLineProjections[i] = _modelLineProjections[i].SetVisible(true);
                    lineChanged = true;
                }
            }

            if (lineChanged)
            {
                _projectedModelLineLineCounts.SetValue(i, _modelLineProjections[i].GetViewLineCount());
            }
        }

        if (!hasVisibleLine)
        {
            // Cannot have everything be hidden => reveal everything!
            SetHiddenAreas([]);
        }

        return true;
    }

    /// <summary>Whether a model position is shown by a view line (false for an invalid line number).</summary>
    public bool ModelPositionIsVisible(int modelLineNumber, int modelColumn)
    {
        if (modelLineNumber < 1 || modelLineNumber > _modelLineProjections.Count)
        {
            // invalid arguments
            return false;
        }

        return _modelLineProjections[modelLineNumber - 1].IsVisible;
    }

    /// <summary>The number of view lines of a model line (1 for an invalid line number).</summary>
    public int GetModelLineViewLineCount(int modelLineNumber)
    {
        if (modelLineNumber < 1 || modelLineNumber > _modelLineProjections.Count)
        {
            // invalid arguments
            return 1;
        }

        return _modelLineProjections[modelLineNumber - 1].GetViewLineCount();
    }

    /// <summary>Returns whether the tab size changed (the lines are built again).</summary>
    public bool SetTabSize(int newTabSize)
    {
        if (_tabSize == newTabSize)
        {
            return false;
        }

        _tabSize = newTabSize;

        ConstructLines(resetHiddenAreas: false);

        return true;
    }

    // ----- model changes

    /// <summary>The model was replaced entirely.</summary>
    public void OnModelFlushed() => ConstructLines(resetHiddenAreas: true);

    // Monaco: `!versionId` is true for null and for 0.
    private bool IsStale(int? versionId) => versionId is null or 0 || versionId <= _validModelVersionId;

    /// <summary>Removes the projections of deleted model lines; returns the view lines that disappeared.</summary>
    public ViewLineRange? OnModelLinesDeleted(int? versionId, int fromLineNumber, int toLineNumber)
    {
        if (IsStale(versionId))
        {
            // Here we check for versionId in case the lines were reconstructed in the meantime.
            // We don't want to apply stale change events on top of a newer read model state.
            return null;
        }

        int outputFromLineNumber = fromLineNumber == 1 ? 1 : _projectedModelLineLineCounts.GetPrefixSum(fromLineNumber - 1) + 1;
        int outputToLineNumber = _projectedModelLineLineCounts.GetPrefixSum(toLineNumber);

        _modelLineProjections.RemoveRange(fromLineNumber - 1, toLineNumber - fromLineNumber + 1);
        _projectedModelLineLineCounts.RemoveValues(fromLineNumber - 1, toLineNumber - fromLineNumber + 1);

        return new ViewLineRange(outputFromLineNumber, outputToLineNumber);
    }

    /// <summary>Adds the projections of <paramref name="count"/> inserted model lines; returns the view lines that appeared.</summary>
    public ViewLineRange? OnModelLinesInserted(int? versionId, int fromLineNumber, int toLineNumber, int count)
    {
        Guard.Assert(count == toLineNumber - fromLineNumber + 1, "count must match the inserted line range");
        if (IsStale(versionId))
        {
            // Here we check for versionId in case the lines were reconstructed in the meantime.
            // We don't want to apply stale change events on top of a newer read model state.
            return null;
        }

        // cannot use GetHiddenAreas() because those decorations have already seen the effect of this model change
        bool isInHiddenArea = fromLineNumber > 2 && !_modelLineProjections[fromLineNumber - 2].IsVisible;

        int outputFromLineNumber = fromLineNumber == 1 ? 1 : _projectedModelLineLineCounts.GetPrefixSum(fromLineNumber - 1) + 1;

        int totalOutputLineCount = 0;
        List<IModelLineProjection> insertLines = [];
        List<int> insertPrefixSumValues = [];

        for (int i = 0; i < count; i++)
        {
            IModelLineProjection line = ModelLineProjection.Create(!isInHiddenArea);
            insertLines.Add(line);

            int outputLineCount = line.GetViewLineCount();
            totalOutputLineCount += outputLineCount;
            insertPrefixSumValues.Add(outputLineCount);
        }

        _modelLineProjections.InsertRange(fromLineNumber - 1, insertLines);
        _projectedModelLineLineCounts.InsertValues(fromLineNumber - 1, insertPrefixSumValues);

        return new ViewLineRange(outputFromLineNumber, outputFromLineNumber + totalOutputLineCount - 1);
    }

    /// <summary>
    /// A model line changed: recreates its projection and returns whether the line mapping changed with the view line ranges to
    /// report as changed, inserted and deleted.
    /// </summary>
    public (bool LineMappingChanged, ViewLineRange? Changed, ViewLineRange? Inserted, ViewLineRange? Deleted) OnModelLineChanged(int? versionId, int lineNumber)
    {
        if (versionId is not null && versionId <= _validModelVersionId)
        {
            // Here we check for versionId in case the lines were reconstructed in the meantime.
            // We don't want to apply stale change events on top of a newer read model state.
            return (false, null, null, null);
        }

        int lineIndex = lineNumber - 1;

        int oldOutputLineCount = _modelLineProjections[lineIndex].GetViewLineCount();
        bool isVisible = _modelLineProjections[lineIndex].IsVisible;
        _modelLineProjections[lineIndex] = ModelLineProjection.Create(isVisible);
        int newOutputLineCount = _modelLineProjections[lineIndex].GetViewLineCount();

        bool lineMappingChanged = false;
        int changeFrom;
        int changeTo;
        int insertFrom = 0;
        int insertTo = -1;
        int deleteFrom = 0;
        int deleteTo = -1;

        if (oldOutputLineCount > newOutputLineCount)
        {
            changeFrom = _projectedModelLineLineCounts.GetPrefixSum(lineNumber - 1) + 1;
            changeTo = changeFrom + newOutputLineCount - 1;
            deleteFrom = changeTo + 1;
            deleteTo = deleteFrom + (oldOutputLineCount - newOutputLineCount) - 1;
            lineMappingChanged = true;
        }
        else if (oldOutputLineCount < newOutputLineCount)
        {
            changeFrom = _projectedModelLineLineCounts.GetPrefixSum(lineNumber - 1) + 1;
            changeTo = changeFrom + oldOutputLineCount - 1;
            insertFrom = changeTo + 1;
            insertTo = insertFrom + (newOutputLineCount - oldOutputLineCount) - 1;
            lineMappingChanged = true;
        }
        else
        {
            changeFrom = _projectedModelLineLineCounts.GetPrefixSum(lineNumber - 1) + 1;
            changeTo = changeFrom + newOutputLineCount - 1;
        }

        _projectedModelLineLineCounts.SetValue(lineIndex, newOutputLineCount);

        return (
            lineMappingChanged,
            changeFrom <= changeTo ? new ViewLineRange(changeFrom, changeTo) : null,
            insertFrom <= insertTo ? new ViewLineRange(insertFrom, insertTo) : null,
            deleteFrom <= deleteTo ? new ViewLineRange(deleteFrom, deleteTo) : null);
    }

    /// <summary>
    /// Tells the lines the model version they are valid for. Returns whether this revealed a line because every line was hidden
    /// (Behavior difference: Monaco does not report it, its layout copes with a line count of 0).
    /// </summary>
    public bool AcceptVersionId(int versionId)
    {
        _validModelVersionId = versionId;
        int viewLineCount = GetViewLineCount();
        EnsureAtLeastOneVisibleLine();
        return GetViewLineCount() != viewLineCount;
    }

    private void EnsureAtLeastOneVisibleLine()
    {
        if (GetViewLineCount() == 0 && _modelLineProjections.Count > 0)
        {
            _modelLineProjections[0] = _modelLineProjections[0].SetVisible(true);
            _projectedModelLineLineCounts.SetValue(0, _modelLineProjections[0].GetViewLineCount());
        }
    }

    // ----- view lines

    public int GetViewLineCount() => _projectedModelLineLineCounts.GetTotalSum();

    private int ToValidViewLineNumber(int viewLineNumber)
    {
        if (viewLineNumber < 1)
        {
            return 1;
        }

        int viewLineCount = GetViewLineCount();
        return viewLineNumber > viewLineCount ? viewLineCount : viewLineNumber;
    }

    private ViewLineInfo GetViewLineInfo(int viewLineNumber)
    {
        viewLineNumber = ToValidViewLineNumber(viewLineNumber);
        PrefixSumIndexOfResult r = _projectedModelLineLineCounts.GetIndexOf(viewLineNumber - 1);
        return new ViewLineInfo(r.Index + 1, r.Remainder);
    }

    private int GetMinColumnOfViewLine(ViewLineInfo info) =>
        _modelLineProjections[info.ModelLineNumber - 1].GetViewLineMinColumn(_model, info.ModelLineNumber, info.ModelLineWrappedLineIdx);

    private int GetMaxColumnOfViewLine(ViewLineInfo info) =>
        _modelLineProjections[info.ModelLineNumber - 1].GetViewLineMaxColumn(_model, info.ModelLineNumber, info.ModelLineWrappedLineIdx);

    public string GetViewLineContent(int viewLineNumber)
    {
        ViewLineInfo info = GetViewLineInfo(viewLineNumber);
        return _modelLineProjections[info.ModelLineNumber - 1].GetViewLineContent(_model, info.ModelLineNumber, info.ModelLineWrappedLineIdx);
    }

    public int GetViewLineLength(int viewLineNumber)
    {
        ViewLineInfo info = GetViewLineInfo(viewLineNumber);
        return _modelLineProjections[info.ModelLineNumber - 1].GetViewLineLength(_model, info.ModelLineNumber, info.ModelLineWrappedLineIdx);
    }

    public int GetViewLineMinColumn(int viewLineNumber) => GetMinColumnOfViewLine(GetViewLineInfo(viewLineNumber));

    public int GetViewLineMaxColumn(int viewLineNumber) => GetMaxColumnOfViewLine(GetViewLineInfo(viewLineNumber));

    public bool GetViewLineContinuesWithWrappedLine(int viewLineNumber)
    {
        ViewLineInfo info = GetViewLineInfo(viewLineNumber);
        return _modelLineProjections[info.ModelLineNumber - 1].GetViewLineContinuesWithWrappedLine(info.ModelLineWrappedLineIdx);
    }

    public ViewLineData GetViewLineData(int viewLineNumber)
    {
        ViewLineInfo info = GetViewLineInfo(viewLineNumber);
        return _modelLineProjections[info.ModelLineNumber - 1].GetViewLineData(_model, info.ModelLineNumber, info.ModelLineWrappedLineIdx);
    }

    /// <summary>The data of the view lines from <paramref name="viewStartLineNumber"/> to <paramref name="viewEndLineNumber"/>; only the <paramref name="needed"/> ones are filled.</summary>
    public ViewLineData?[] GetViewLinesData(int viewStartLineNumber, int viewEndLineNumber, bool[] needed)
    {
        viewStartLineNumber = ToValidViewLineNumber(viewStartLineNumber);
        viewEndLineNumber = ToValidViewLineNumber(viewEndLineNumber);

        PrefixSumIndexOfResult start = _projectedModelLineLineCounts.GetIndexOf(viewStartLineNumber - 1);
        int viewLineNumber = viewStartLineNumber;
        int startModelLineIndex = start.Index;
        int startRemainder = start.Remainder;

        ViewLineData?[] result = new ViewLineData?[Math.Max(0, viewEndLineNumber - viewStartLineNumber + 1)];
        for (int modelLineIndex = startModelLineIndex, len = _model.LineCount; modelLineIndex < len; modelLineIndex++)
        {
            IModelLineProjection line = _modelLineProjections[modelLineIndex];
            if (!line.IsVisible)
            {
                continue;
            }

            int fromViewLineIndex = modelLineIndex == startModelLineIndex ? startRemainder : 0;
            int remainingViewLineCount = line.GetViewLineCount() - fromViewLineIndex;

            bool lastLine = false;
            if (viewLineNumber + remainingViewLineCount > viewEndLineNumber)
            {
                lastLine = true;
                remainingViewLineCount = viewEndLineNumber - viewLineNumber + 1;
            }

            line.GetViewLinesData(_model, modelLineIndex + 1, fromViewLineIndex, remainingViewLineCount, viewLineNumber - viewStartLineNumber, needed, result);

            viewLineNumber += remainingViewLineCount;

            if (lastLine)
            {
                break;
            }
        }

        return result;
    }
}
