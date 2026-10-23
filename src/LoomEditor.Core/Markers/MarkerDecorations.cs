// Ported from microsoft/vscode: src/vs/editor/common/services/markerDecorationsService.ts (MarkerDecorationsService, MarkerDecorations)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Markers;

/// <summary>
/// Keeps the decorations of a <see cref="TextModel"/> in step with a <see cref="MarkerService"/>: one decoration per marker
/// (the first 500), mapped back to its marker by decoration id.
/// Not ported: suppressed ranges, the per-resource maps, <c>onDidChangeMarker</c> (the model's decoration events tell the view),
/// and the mutation of <c>startColumn</c> of whole-line markers (<see cref="Marker"/> is immutable).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class MarkerDecorations : IDisposable
{
    private const int MaxMarkers = 500;

    private static readonly Dictionary<(MarkerSeverity, bool, bool), ModelDecorationOptions> OptionsCache = [];

    private readonly TextModel _model;
    private readonly MarkerService _service;
    private readonly int _ownerId;
    private readonly Dictionary<Marker, string> _markerToId = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, Marker> _idToMarker = new(StringComparer.Ordinal);

    public MarkerDecorations(TextModel model, MarkerService service, int ownerId)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(service);
        _model = model;
        _service = service;
        _ownerId = ownerId;
        _service.MarkersChanged += OnMarkersChanged;
        Update();
    }

    /// <summary>The marker a decoration of this owner stands for, or null.</summary>
    public Marker? GetMarker(string decorationId) => _idToMarker.GetValueOrDefault(decorationId);

    /// <summary>Every marker with the current range of its decoration (Monaco <c>getMarkers</c>).</summary>
    public IReadOnlyList<(Range Range, Marker Marker)> GetLiveMarkers()
    {
        var result = new List<(Range, Marker)>();
        foreach (var (id, marker) in _idToMarker)
        {
            if (_model.GetDecorationRange(id) is { } range)
            {
                result.Add((range, marker));
            }
        }

        return result;
    }

    public void Dispose()
    {
        _service.MarkersChanged -= OnMarkersChanged;
        _model.DeltaDecorations(_ownerId, [.. _idToMarker.Keys], []);
        _idToMarker.Clear();
        _markerToId.Clear();
    }

    private void OnMarkersChanged(object? sender, EventArgs e) => Update();

    // Monaco: MarkerDecorations.update
    private void Update()
    {
        var markers = _service.Read(take: MaxMarkers);
        var current = new HashSet<Marker>(markers, ReferenceEqualityComparer.Instance);
        var added = markers.Where(m => !_markerToId.ContainsKey(m)).ToList();
        var removed = _markerToId.Where(e => !current.Contains(e.Key)).ToList();
        if (added.Count == 0 && removed.Count == 0)
        {
            return;
        }

        var ids = _model.DeltaDecorations(
            _ownerId,
            [.. removed.Select(e => e.Value)],
            [.. added.Select(m => new ModelDeltaDecoration(CreateDecorationRange(m), CreateDecorationOptions(m)))]);

        foreach (var (marker, id) in removed)
        {
            _markerToId.Remove(marker);
            _idToMarker.Remove(id);
        }

        for (var i = 0; i < added.Count; i++)
        {
            _markerToId[added[i]] = ids[i];
            _idToMarker[ids[i]] = added[i];
        }
    }

    private static bool HasTag(Marker marker, MarkerTag tag) => marker.Tags?.Contains(tag) == true;

    // Monaco: _createDecorationRange
    private Range CreateDecorationRange(Marker marker)
    {
        var range = new Range(marker.StartLineNumber, marker.StartColumn, marker.EndLineNumber, marker.EndColumn);
        if (marker.Severity == MarkerSeverity.Hint && !HasTag(marker, MarkerTag.Unnecessary) && !HasTag(marker, MarkerTag.Deprecated))
        {
            // never render hints on multiple lines; make enough space for three dots
            range = new Range(range.StartLineNumber, range.StartColumn, range.StartLineNumber, range.StartColumn + 2);
        }

        range = _model.ValidateRange(range);
        if (range.IsEmpty)
        {
            var maxColumn = _model.GetLineLastNonWhitespaceColumn(range.StartLineNumber);
            if (maxColumn == 0)
            {
                maxColumn = _model.GetLineMaxColumn(range.StartLineNumber);
            }

            if (maxColumn == 1 || range.EndColumn >= maxColumn)
            {
                // empty line or behind eol: keep the range, it is rendered 1ch wide
                return range;
            }

            if (_model.GetWordAtPosition(range.Start) is { } word)
            {
                range = new Range(range.StartLineNumber, word.StartColumn, range.EndLineNumber, word.EndColumn);
            }
        }
        else if (marker.EndColumn == int.MaxValue && marker.StartColumn == 1 && range.StartLineNumber == range.EndLineNumber)
        {
            var minColumn = _model.GetLineFirstNonWhitespaceColumn(marker.StartLineNumber);
            if (minColumn < range.EndColumn)
            {
                range = new Range(range.StartLineNumber, minColumn, range.EndLineNumber, range.EndColumn);
            }
        }

        return range;
    }

    // Monaco: _createDecorationOption
    private static ModelDecorationOptions CreateDecorationOptions(Marker marker)
    {
        var unnecessary = HasTag(marker, MarkerTag.Unnecessary);
        var deprecated = HasTag(marker, MarkerTag.Deprecated);
        var key = (marker.Severity, unnecessary, deprecated);
        if (OptionsCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        string? className;
        string? ruler = null;
        string? minimap = null;
        int zIndex;
        switch (marker.Severity)
        {
            case MarkerSeverity.Hint:
                className = deprecated ? null : unnecessary ? MarkerClassNames.EditorUnnecessaryDecoration : MarkerClassNames.EditorHintDecoration;
                zIndex = 0;
                break;
            case MarkerSeverity.Info:
                className = MarkerClassNames.EditorInfoDecoration;
                (ruler, minimap, zIndex) = ("editorOverviewRuler.infoForeground", "minimap.infoHighlight", 10);
                break;
            case MarkerSeverity.Warning:
                className = MarkerClassNames.EditorWarningDecoration;
                (ruler, minimap, zIndex) = ("editorOverviewRuler.warningForeground", "minimap.warningHighlight", 20);
                break;
            default:
                className = MarkerClassNames.EditorErrorDecoration;
                (ruler, minimap, zIndex) = ("editorOverviewRuler.errorForeground", "minimap.errorHighlight", 30);
                break;
        }

        // the later tag wins, like Monaco
        var inlineClassName = deprecated ? MarkerClassNames.EditorDeprecatedInlineDecoration
            : unnecessary ? MarkerClassNames.EditorUnnecessaryInlineDecoration : null;

        var options = ModelDecorationOptions.Register(new ModelDecorationOptionsData
        {
            Description = "marker-decoration",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = className,
            ShowIfCollapsed = true,
            OverviewRuler = ruler is null ? null : new ModelDecorationOverviewRulerOptions(DecorationColor.FromThemeColor(ruler), DecorationColor.FromThemeColor(ruler), OverviewRulerLane.Right),
            Minimap = minimap is null ? null : new ModelDecorationMinimapOptions(DecorationColor.FromThemeColor(minimap), DecorationColor.FromThemeColor(minimap), MinimapPosition.Inline),
            ZIndex = zIndex,
            InlineClassName = inlineClassName,
        });
        OptionsCache[key] = options;
        return options;
    }
}
