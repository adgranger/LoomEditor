// Ported from microsoft/vscode: src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts (MarkerList)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.GotoError;

/// <summary>
/// The errors, warnings and infos of a <see cref="MarkerService"/>, sorted; knows which one is next or previous from a position, wrapping
/// around (Monaco: <c>MarkerList</c>).
/// </summary>
/// <remarks>
/// Behavior differences: a single resource (a <see cref="MarkerService"/> belongs to one model: no resource filter, no <c>matches</c>, no
/// different-file branch); the sort order is a constructor argument instead of the <c>problems.sortOrder</c> setting. Threading: UI thread only.
/// </remarks>
internal sealed class MarkerList : IDisposable
{
    private const int Visible = (int)MarkerSeverity.Error | (int)MarkerSeverity.Warning | (int)MarkerSeverity.Info;

    private readonly MarkerService _markerService;
    private readonly Comparison<Marker> _compare;
    private List<Marker> _markers = [];
    private int _nextIdx = -1;

    /// <summary>Reads the markers and starts listening to the service.</summary>
    public MarkerList(MarkerService markers, MarkerSortOrder order = MarkerSortOrder.Severity)
    {
        ArgumentNullException.ThrowIfNull(markers);

        _markerService = markers;
        _compare = order == MarkerSortOrder.Position
            ? (a, b) => CompareStarts(a, b) is var res and not 0 ? res : MarkerSeverities.Compare(a.Severity, b.Severity)
            : (a, b) => MarkerSeverities.Compare(a.Severity, b.Severity) is var res and not 0 ? res : CompareStarts(a, b);

        UpdateMarkers();
        _markerService.MarkersChanged += OnMarkersChanged;
    }

    /// <summary>Raised when the sorted list changed (a different range, severity or message); the index is reset first.</summary>
    public event EventHandler? Changed;

    /// <summary>The problem at the current index with its 1-based index and the total, or <see langword="null"/> before the first move.</summary>
    public MarkerCoordinate? Selected =>
        _nextIdx >= 0 && _nextIdx < _markers.Count ? new MarkerCoordinate(_markers[_nextIdx], _nextIdx + 1, _markers.Count) : null;

    /// <summary>Forgets the current index: the next move starts from the position again.</summary>
    public void ResetIndex() => _nextIdx = -1;

    /// <summary>
    /// Moves to the next (<paramref name="forward"/>) or previous problem; the first move starts from <paramref name="position"/>.
    /// False when nothing changed (no problem, or a single one already selected).
    /// </summary>
    public bool Move(bool forward, TextModel model, Position position)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (_markers.Count == 0)
        {
            return false;
        }

        int oldIdx = _nextIdx;
        if (_nextIdx == -1)
        {
            InitIdx(model, position, forward);
        }
        else if (forward)
        {
            _nextIdx = (_nextIdx + 1) % _markers.Count;
        }
        else
        {
            _nextIdx = (_nextIdx - 1 + _markers.Count) % _markers.Count;
        }

        return oldIdx != _nextIdx;
    }

    /// <summary>The first problem whose range contains <paramref name="position"/>, or <see langword="null"/>.</summary>
    public MarkerCoordinate? Find(Position position)
    {
        for (int idx = 0; idx < _markers.Count; idx++)
        {
            if (RangeOf(_markers[idx]).Contains(position))
            {
                return new MarkerCoordinate(_markers[idx], idx + 1, _markers.Count);
            }
        }

        return null;
    }

    /// <summary>Stops listening to the service.</summary>
    public void Dispose() => _markerService.MarkersChanged -= OnMarkersChanged;

    private static Range RangeOf(Marker marker) => new(marker.StartLineNumber, marker.StartColumn, marker.EndLineNumber, marker.EndColumn);

    private static int CompareStarts(Marker a, Marker b) => Range.CompareUsingStarts(RangeOf(a), RangeOf(b));

    private static bool Same(Marker a, Marker b) =>
        a.StartLineNumber == b.StartLineNumber
        && a.StartColumn == b.StartColumn
        && a.EndLineNumber == b.EndLineNumber
        && a.EndColumn == b.EndColumn
        && a.Severity == b.Severity
        && a.Message == b.Message;

    private void OnMarkersChanged(object? sender, EventArgs e)
    {
        if (UpdateMarkers())
        {
            _nextIdx = -1;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    // Monaco: updateMarker; false when the sorted list is the same as before.
    private bool UpdateMarkers()
    {
        // A stable sort, like Array.prototype.sort.
        List<Marker> newMarkers = [.. _markerService.Read(severities: Visible).Order(Comparer<Marker>.Create(_compare))];
        if (newMarkers.Count == _markers.Count && newMarkers.Zip(_markers, Same).All(same => same))
        {
            return false;
        }

        _markers = newMarkers;
        return true;
    }

    // Monaco: _initIdx, for a single resource.
    private void InitIdx(TextModel model, Position position, bool forward)
    {
        bool found = false;
        bool wentPast = false;
        for (int i = 0; i < _markers.Count; i++)
        {
            Range range = RangeOf(_markers[i]);

            if (range.IsEmpty && model.GetWordAtPosition(range.Start) is { } word)
            {
                range = new Range(range.StartLineNumber, word.StartColumn, range.StartLineNumber, word.EndColumn);
            }

            if (range.Contains(position) || position.IsBeforeOrEqual(range.Start))
            {
                _nextIdx = i;
                found = true;
                wentPast = !range.Contains(position);
                break;
            }
        }

        if (!found)
        {
            // After the last problem.
            _nextIdx = forward ? 0 : _markers.Count - 1;
        }
        else if (wentPast && !forward)
        {
            // We went past and have to go one back.
            _nextIdx -= 1;
        }

        if (_nextIdx < 0)
        {
            _nextIdx = _markers.Count - 1;
        }
    }
}
