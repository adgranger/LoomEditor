// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts (setHiddenAreas, getVisibleRanges, getHiddenAreas, _toModelVisibleRanges, HiddenAreasModel, StableViewport)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

// Hidden areas: model lines that no view line shows (folding). Each source (folding, a test...) owns its ranges; the
// merged ranges are what the view lines hide.
internal sealed partial class ViewModel
{
    private static readonly object DefaultHiddenAreasSource = new();

    private readonly HiddenAreasModel _hiddenAreasModel = new();
    private IReadOnlyList<Range> _previousHiddenAreas = [];

    /// <summary>
    /// Hides the model lines of <paramref name="ranges"/> on behalf of <paramref name="source"/> (sources are compared by
    /// reference; <see langword="null"/> is one source of its own) and shows the lines the other sources do not hide.
    /// </summary>
    /// <param name="ranges">The model ranges to hide.</param>
    /// <param name="source">Who hides them.</param>
    /// <param name="forceUpdate">Apply the ranges even if they equal the previous ones: a model flush resets the hidden areas, not the cached value.</param>
    public void SetHiddenAreas(IReadOnlyList<Range> ranges, object? source = null, bool forceUpdate = false)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        _hiddenAreasModel.SetHiddenAreas(source ?? DefaultHiddenAreasSource, ranges);
        IReadOnlyList<Range> mergedRanges = _hiddenAreasModel.GetMergedRanges();
        if (ReferenceEquals(mergedRanges, _previousHiddenAreas) && !forceUpdate)
        {
            return;
        }

        _previousHiddenAreas = mergedRanges;

        StableViewport stableViewport = CaptureStableViewport();

        bool lineMappingChanged = false;
        EmitViewEvent(eventsCollector =>
        {
            lineMappingChanged = _lines.SetHiddenAreas(mergedRanges);
            if (lineMappingChanged)
            {
                _cursor.OnLineMappingChanged(eventsCollector);
                _decorations.OnLineMappingChanged();
                ViewLayout.OnFlushed(GetLineCount(), []);
                ViewLayout.OnHeightMaybeChanged();
                ViewZones.RecomputeWhitespaceProps();
            }

            int? firstModelLineInViewPort = stableViewport.ViewportStartModelPosition?.LineNumber;
            bool firstModelLineIsHidden = firstModelLineInViewPort is { } line
                && mergedRanges.Any(range => range.StartLineNumber <= line && line <= range.EndLineNumber);
            if (!firstModelLineIsHidden)
            {
                stableViewport.RecoverViewportStart(CoordinatesConverter, ViewLayout);
            }
        });

        if (lineMappingChanged)
        {
            UpdateLayoutViewLineCount();
            HandleVisibleLinesChanged();
            EventDispatcher.EmitOutgoingEvent(new HiddenAreasChangedEvent());
        }
    }

    /// <summary>The hidden model ranges, as the decorations track them now.</summary>
    public IReadOnlyList<Range> GetHiddenAreas() => _lines.GetHiddenAreas();

    /// <summary>The model ranges of the completely visible view lines, without the hidden areas.</summary>
    public List<Range> GetVisibleRanges() => ToModelVisibleRanges(GetCompletelyVisibleViewRange());

    private List<Range> GetModelVisibleRanges()
    {
        PartialViewLinesViewportData linesViewportData = ViewLayout.GetLinesViewportData();
        var viewVisibleRange = new Range(
            linesViewportData.StartLineNumber,
            GetLineMinColumn(linesViewportData.StartLineNumber),
            linesViewportData.EndLineNumber,
            GetLineMaxColumn(linesViewportData.EndLineNumber));
        return ToModelVisibleRanges(viewVisibleRange);
    }

    private List<Range> ToModelVisibleRanges(Range visibleViewRange)
    {
        Range visibleRange = CoordinatesConverter.ConvertViewRangeToModelRange(visibleViewRange);
        IReadOnlyList<Range> hiddenAreas = _lines.GetHiddenAreas();

        if (hiddenAreas.Count == 0)
        {
            return [visibleRange];
        }

        var result = new List<Range>();
        int startLineNumber = visibleRange.StartLineNumber;
        int startColumn = visibleRange.StartColumn;
        int endLineNumber = visibleRange.EndLineNumber;
        int endColumn = visibleRange.EndColumn;
        foreach (Range hiddenArea in hiddenAreas)
        {
            int hiddenStartLineNumber = hiddenArea.StartLineNumber;
            int hiddenEndLineNumber = hiddenArea.EndLineNumber;

            if (hiddenEndLineNumber < startLineNumber)
            {
                continue;
            }

            if (hiddenStartLineNumber > endLineNumber)
            {
                continue;
            }

            if (startLineNumber < hiddenStartLineNumber)
            {
                result.Add(new Range(
                    startLineNumber, startColumn, hiddenStartLineNumber - 1, _model.GetLineMaxColumn(hiddenStartLineNumber - 1)));
            }

            startLineNumber = hiddenEndLineNumber + 1;
            startColumn = 1;
        }

        if (startLineNumber < endLineNumber || (startLineNumber == endLineNumber && startColumn < endColumn))
        {
            result.Add(new Range(startLineNumber, startColumn, endLineNumber, endColumn));
        }

        return result;
    }

    // Monaco: _captureStableViewport. Behavior difference: the _viewportStart tracker of Monaco is not ported, so the
    // first view line of the viewport is read from the layout when the view is scrolled.
    private StableViewport CaptureStableViewport()
    {
        double scrollTop = ViewLayout.GetCurrentScrollTop();
        if (scrollTop > 0)
        {
            int viewLineNumber = ViewLayout.GetLinesViewportData().StartLineNumber;
            var viewPosition = new Position(viewLineNumber, GetLineMinColumn(viewLineNumber));
            Position modelPosition = CoordinatesConverter.ConvertViewPositionToModelPosition(viewPosition);
            return new StableViewport(modelPosition, scrollTop - ViewLayout.GetVerticalOffsetForLineNumber(viewLineNumber));
        }

        return new StableViewport(null, 0);
    }

    // Monaco: HiddenAreasModel
    private sealed class HiddenAreasModel
    {
        private readonly Dictionary<object, IReadOnlyList<Range>> _hiddenAreas = new(ReferenceEqualityComparer.Instance);
        private bool _shouldRecompute;
        private IReadOnlyList<Range> _ranges = [];

        public void SetHiddenAreas(object source, IReadOnlyList<Range> ranges)
        {
            if (_hiddenAreas.TryGetValue(source, out IReadOnlyList<Range>? existing) && RangeArraysEqual(existing, ranges))
            {
                return;
            }

            _hiddenAreas[source] = ranges;
            _shouldRecompute = true;
        }

        /// <summary>The same list instance as last time when nothing changed. The list is immutable.</summary>
        public IReadOnlyList<Range> GetMergedRanges()
        {
            if (!_shouldRecompute)
            {
                return _ranges;
            }

            _shouldRecompute = false;
            IReadOnlyList<Range> newRanges = [];
            foreach (IReadOnlyList<Range> hiddenAreas in _hiddenAreas.Values)
            {
                newRanges = MergeLineRangeArray(newRanges, hiddenAreas);
            }

            if (RangeArraysEqual(_ranges, newRanges))
            {
                return _ranges;
            }

            _ranges = newRanges;
            return _ranges;
        }
    }

    // Monaco: mergeLineRangeArray, with its known bug (both indexes advance after a merge): the lines normalize the result.
    private static List<Range> MergeLineRangeArray(IReadOnlyList<Range> arr1, IReadOnlyList<Range> arr2)
    {
        var result = new List<Range>();
        int i = 0;
        int j = 0;
        while (i < arr1.Count && j < arr2.Count)
        {
            Range item1 = arr1[i];
            Range item2 = arr2[j];

            if (item1.EndLineNumber < item2.StartLineNumber - 1)
            {
                result.Add(arr1[i++]);
            }
            else if (item2.EndLineNumber < item1.StartLineNumber - 1)
            {
                result.Add(arr2[j++]);
            }
            else
            {
                int startLineNumber = Math.Min(item1.StartLineNumber, item2.StartLineNumber);
                int endLineNumber = Math.Max(item1.EndLineNumber, item2.EndLineNumber);
                result.Add(new Range(startLineNumber, 1, endLineNumber, 1));
                i++;
                j++;
            }
        }

        while (i < arr1.Count)
        {
            result.Add(arr1[i++]);
        }

        while (j < arr2.Count)
        {
            result.Add(arr2[j++]);
        }

        return result;
    }

    private static bool RangeArraysEqual(IReadOnlyList<Range> arr1, IReadOnlyList<Range> arr2)
    {
        if (arr1.Count != arr2.Count)
        {
            return false;
        }

        for (int i = 0; i < arr1.Count; i++)
        {
            if (arr1[i] != arr2[i])
            {
                return false;
            }
        }

        return true;
    }

    // Monaco: StableViewport. Maintains a stable viewport by trying to keep the first line in the viewport constant.
    private sealed class StableViewport(Position? viewportStartModelPosition, double startLineDelta)
    {
        public Position? ViewportStartModelPosition { get; } = viewportStartModelPosition;

        public void RecoverViewportStart(ICoordinatesConverter coordinatesConverter, ViewLayout viewLayout)
        {
            if (ViewportStartModelPosition is not { } modelPosition)
            {
                return;
            }

            Position viewPosition = coordinatesConverter.ConvertModelPositionToViewPosition(modelPosition);
            int viewPositionTop = viewLayout.GetVerticalOffsetForLineNumber(viewPosition.LineNumber);
            viewLayout.SetScrollPosition(new NewScrollPosition(ScrollTop: viewPositionTop + startLineDelta), ScrollType.Immediate);
        }
    }
}
