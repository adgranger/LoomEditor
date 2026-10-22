// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/hiddenRangeModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.Folding;

/// <summary>
/// The lines hidden by the collapsed regions of a <see cref="FoldingModel"/>, as ranges, kept up to date while the
/// folding model changes (Monaco: <c>HiddenRangeModel</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HiddenRangeModel : IDisposable
{
    private readonly FoldingModel _foldingModel;
    private List<Range> _hiddenRanges = [];
    private bool _isListening = true;
    private bool _hasLineChanges;

    public HiddenRangeModel(FoldingModel model)
    {
        _foldingModel = model;
        model.Changed += OnFoldingModelChanged;
        if (model.Regions.Length > 0)
        {
            UpdateHiddenRanges();
        }
    }

    /// <summary>Monaco: <c>onDidChange</c>.</summary>
    public event EventHandler<IReadOnlyList<Range>>? Changed;

    public IReadOnlyList<Range> HiddenRanges => _hiddenRanges;

    public void NotifyChangeModelContent(ModelContentChangedEventArgs e)
    {
        if (_hiddenRanges.Count > 0 && !_hasLineChanges)
        {
            _hasLineChanges = e.Changes.Any(change =>
                change.Range.EndLineNumber != change.Range.StartLineNumber || change.Text.AsSpan().IndexOfAny('\r', '\n') >= 0);
        }
    }

    private void OnFoldingModelChanged(object? sender, FoldingModelChangeEvent e) => UpdateHiddenRanges();

    private void UpdateHiddenRanges()
    {
        bool updateHiddenAreas = false;
        var newHiddenAreas = new List<Range>();
        int k = 0;

        int lastCollapsedStart = int.MaxValue;
        int lastCollapsedEnd = -1;

        FoldingRegions ranges = _foldingModel.Regions;
        for (int i = 0; i < ranges.Length; i++)
        {
            if (!ranges.IsCollapsed(i))
            {
                continue;
            }

            int startLineNumber = ranges.GetStartLineNumber(i) + 1; // the first line is not hidden
            int endLineNumber = ranges.GetEndLineNumber(i);
            if (lastCollapsedStart <= startLineNumber && endLineNumber <= lastCollapsedEnd)
            {
                // ignore ranges contained in collapsed regions
                continue;
            }

            if (!updateHiddenAreas && k < _hiddenRanges.Count && _hiddenRanges[k].StartLineNumber == startLineNumber && _hiddenRanges[k].EndLineNumber == endLineNumber)
            {
                // reuse the old ranges
                newHiddenAreas.Add(_hiddenRanges[k]);
                k++;
            }
            else
            {
                updateHiddenAreas = true;
                newHiddenAreas.Add(new Range(startLineNumber, 1, endLineNumber, 1));
            }

            lastCollapsedStart = startLineNumber;
            lastCollapsedEnd = endLineNumber;
        }

        if (_hasLineChanges || updateHiddenAreas || k < _hiddenRanges.Count)
        {
            ApplyHiddenRanges(newHiddenAreas);
        }
    }

    private void ApplyHiddenRanges(List<Range> newHiddenAreas)
    {
        _hiddenRanges = newHiddenAreas;
        _hasLineChanges = false;
        Changed?.Invoke(this, newHiddenAreas);
    }

    public bool HasRanges() => _hiddenRanges.Count > 0;

    public bool IsHidden(int line) => FindRange(_hiddenRanges, line) is not null;

    /// <summary>Moves the selections that start or end in a hidden range to the end of the line above it (changes the array in place).</summary>
    /// <returns><see langword="true"/> when a selection changed.</returns>
    public bool AdjustSelections(Selection[] selections)
    {
        bool hasChanges = false;
        TextModel editorModel = _foldingModel.TextModel;
        Range? lastRange = null;

        int? AdjustLine(int line)
        {
            if (lastRange is not { } last || !IsInside(line, last))
            {
                lastRange = FindRange(_hiddenRanges, line);
            }

            return lastRange is { } range ? range.StartLineNumber - 1 : null;
        }

        for (int i = 0, len = selections.Length; i < len; i++)
        {
            Selection selection = selections[i];

            // Monaco tests the line for JavaScript truthiness: line 0 means "no change".
            if (AdjustLine(selection.Range.StartLineNumber) is { } adjustedStartLine && adjustedStartLine != 0)
            {
                selection = selection.WithStart(new Position(adjustedStartLine, editorModel.GetLineMaxColumn(adjustedStartLine)));
                hasChanges = true;
            }

            if (AdjustLine(selection.Range.EndLineNumber) is { } adjustedEndLine && adjustedEndLine != 0)
            {
                selection = selection.WithEnd(new Position(adjustedEndLine, editorModel.GetLineMaxColumn(adjustedEndLine)));
                hasChanges = true;
            }

            selections[i] = selection;
        }

        return hasChanges;
    }

    public void Dispose()
    {
        if (_hiddenRanges.Count > 0)
        {
            _hiddenRanges = [];
            Changed?.Invoke(this, _hiddenRanges);
        }

        if (_isListening)
        {
            _isListening = false;
            _foldingModel.Changed -= OnFoldingModelChanged;
        }

        Changed = null;
    }

    private static bool IsInside(int line, Range range) => line >= range.StartLineNumber && line <= range.EndLineNumber;

    private static Range? FindRange(List<Range> ranges, int line)
    {
        // findFirstIdxMonotonousOrArrLen: the first range that starts after the line.
        int low = 0;
        int high = ranges.Count;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (line < ranges[mid].StartLineNumber)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        int i = low - 1;
        return i >= 0 && ranges[i].EndLineNumber >= line ? ranges[i] : null;
    }
}
