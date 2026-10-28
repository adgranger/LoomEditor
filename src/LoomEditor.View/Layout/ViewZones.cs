// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/viewZones/viewZones.ts (model part: no DOM nodes)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>
/// The view zones of an editor, kept as whitespaces of the <see cref="ViewLayout"/>. A zone keeps its model line: it is
/// not moved when lines are deleted above it (like Monaco, owners re-layout).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ViewZones
{
    private readonly ViewModel _viewModel;
    private readonly Dictionary<string, ViewZone> _zones = [];

    public ViewZones(ViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>Raised when zones were added, removed or moved, or their height changed.</summary>
    public event EventHandler? ZonesChanged;

    /// <summary>The zones, in no particular order.</summary>
    public IEnumerable<ViewZone> Zones => _zones.Values;

    /// <summary>The zone with this id, or <see langword="null"/>.</summary>
    public ViewZone? GetZone(string id) => _zones.GetValueOrDefault(id);

    /// <summary>Monaco: <c>shouldSuppressMouseDownOnViewZone</c>.</summary>
    public bool ShouldSuppressMouseDown(string id) => _zones.TryGetValue(id, out ViewZone? zone) && zone.SuppressMouseDown;

    /// <summary>Monaco: <c>changeViewZones</c>. Returns <see langword="true"/> when something changed.</summary>
    public bool Change(Action<IViewZoneChangeAccessor> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        bool zonesHaveChanged = false;
        _viewModel.ViewLayout.ChangeWhitespace(whitespaceAccessor =>
        {
            var accessor = new ChangeAccessor(this, whitespaceAccessor, () => zonesHaveChanged = true);
            try
            {
                callback(accessor);
            }
            finally
            {
                accessor.Invalidate();
            }
        });

        if (zonesHaveChanged)
        {
            ZonesChanged?.Invoke(this, EventArgs.Empty);
        }

        return zonesHaveChanged;
    }

    /// <summary>Monaco: <c>_recomputeWhitespacesProps</c>; call when the line mapping or the line height changed.</summary>
    public bool RecomputeWhitespaceProps()
    {
        if (_zones.Count == 0)
        {
            return false;
        }

        Dictionary<string, IEditorWhitespace> oldWhitespaces = _viewModel.ViewLayout.GetWhitespaces().ToDictionary(w => w.Id);
        bool hadAChange = false;
        _viewModel.ViewLayout.ChangeWhitespace(whitespaceAccessor =>
        {
            foreach ((string id, ViewZone zone) in _zones)
            {
                (int afterViewLineNumber, int heightInPx, _) = ComputeWhitespaceProps(zone);
                if (oldWhitespaces.TryGetValue(id, out IEditorWhitespace? old)
                    && (old.AfterLineNumber != afterViewLineNumber || old.Height != heightInPx))
                {
                    whitespaceAccessor.ChangeOneWhitespace(id, afterViewLineNumber, heightInPx);
                    zone.OnComputedHeight?.Invoke(heightInPx);
                    hadAChange = true;
                }
            }
        });

        if (hadAChange)
        {
            ZonesChanged?.Invoke(this, EventArgs.Empty);
        }

        return hadAChange;
    }

    // Monaco: _computeWhitespaceProps
    private (int AfterViewLineNumber, int HeightInPx, int MinWidthInPx) ComputeWhitespaceProps(ViewZone zone)
    {
        int minWidth = zone.MinWidthInPx ?? 0;
        if (zone.AfterLineNumber == 0)
        {
            return (0, HeightInPixels(zone), minWidth);
        }

        var model = _viewModel.Model;
        Position afterPosition;
        if (zone.AfterColumn is { } afterColumn)
        {
            afterPosition = model.ValidatePosition(zone.AfterLineNumber, afterColumn);
        }
        else
        {
            int validLineNumber = model.ValidatePosition(zone.AfterLineNumber, 1).LineNumber;
            afterPosition = new Position(validLineNumber, model.GetLineMaxColumn(validLineNumber));
        }

        Position beforePosition = afterPosition.Column == model.GetLineMaxColumn(afterPosition.LineNumber)
            ? model.ValidatePosition(afterPosition.LineNumber + 1, 1)
            : model.ValidatePosition(afterPosition.LineNumber, afterPosition.Column + 1);

        ICoordinatesConverter converter = _viewModel.CoordinatesConverter;
        Position viewPosition = converter.ConvertModelPositionToViewPosition(afterPosition, null, true);
        bool isVisible = zone.ShowInHiddenAreas || converter.ModelPositionIsVisible(beforePosition);
        return (viewPosition.LineNumber, isVisible ? HeightInPixels(zone) : 0, minWidth);
    }

    private int HeightInPixels(ViewZone zone)
    {
        int lineHeight = _viewModel.ViewLayoutOptions.LineHeight;
        if (zone.HeightInPx is { } px)
        {
            return px;
        }

        return zone.HeightInLines is { } lines ? (int)(lineHeight * lines) : lineHeight;
    }

    private sealed class ChangeAccessor(ViewZones owner, IWhitespaceChangeAccessor whitespaces, Action markChanged) : IViewZoneChangeAccessor
    {
        private bool _valid = true;

        public void Invalidate() => _valid = false;

        public string AddZone(ViewZone zone)
        {
            Check();
            ArgumentNullException.ThrowIfNull(zone);
            markChanged();
            (int afterViewLineNumber, int heightInPx, int minWidth) = owner.ComputeWhitespaceProps(zone);
            string id = whitespaces.InsertWhitespace(afterViewLineNumber, zone.Ordinal ?? zone.AfterColumn ?? 10000, heightInPx, minWidth);
            zone.Id = id;
            owner._zones[id] = zone;
            zone.OnComputedHeight?.Invoke(heightInPx);
            return id;
        }

        public void RemoveZone(string id)
        {
            Check();
            if (owner._zones.Remove(id))
            {
                whitespaces.RemoveWhitespace(id);
                markChanged();
            }
        }

        public void LayoutZone(string id)
        {
            Check();
            if (owner._zones.TryGetValue(id, out ViewZone? zone))
            {
                (int afterViewLineNumber, int heightInPx, _) = owner.ComputeWhitespaceProps(zone);
                whitespaces.ChangeOneWhitespace(id, afterViewLineNumber, heightInPx);
                zone.OnComputedHeight?.Invoke(heightInPx);
                markChanged();
            }
        }

        private void Check()
        {
            if (!_valid)
            {
                throw new InvalidOperationException("Invalid change accessor");
            }
        }
    }
}
