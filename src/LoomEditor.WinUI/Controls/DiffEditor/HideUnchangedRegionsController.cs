// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/features/hideUnchangedRegionsFeature.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;
using Microsoft.UI.Dispatching;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// Hides the unchanged regions of the two editors behind bars and lets the user reveal them (Monaco:
/// <c>HideUnchangedRegionsFeature</c> and <c>CollapsedCodeOverlayWidget</c>).
/// </summary>
/// <remarks>
/// <para>
/// Deliberate differences: no compact mode, no outline breadcrumb, no fold icon in the glyph margin, no resize cursor over the
/// strips (the editor sets its own cursor on every move). The scroll anchor on a click (not on a drag: the bar must follow the pointer) has no Monaco source: the standalone editor passes no anchor callback.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class HideUnchangedRegionsController : IDisposable
{
    private const double HandleHeight = 6;
    private const double ClickTolerance = 2;

    private readonly LoomEditor _original;
    private readonly LoomEditor _modified;
    private readonly DiffEditorViewModel _viewModel;
    private readonly DispatcherQueue _dispatcher;
    private readonly Func<bool> _inline;
    private readonly Func<int> _revealLineCount;
    private readonly EditorDecorationTarget _originalDecorations;
    private readonly EditorDecorationTarget _modifiedDecorations;
    private readonly Dictionary<UnchangedRegion, string> _originalZones = [];
    private readonly Dictionary<UnchangedRegion, string> _modifiedZones = [];
    private List<UnchangedRegion> _subscribed = [];
    private Drag? _drag;
    private bool _isApplying;
    private bool _isDisposed;

    /// <summary>Starts listening and applies the current regions.</summary>
    public HideUnchangedRegionsController(LoomEditor original, LoomEditor modified, DiffEditorViewModel viewModel, DispatcherQueue dispatcher, Func<bool> inline, Func<int> revealLineCount)
    {
        _original = original;
        _modified = modified;
        _viewModel = viewModel;
        _dispatcher = dispatcher;
        _inline = inline;
        _revealLineCount = revealLineCount;
        _originalDecorations = new EditorDecorationTarget(original.CreateModelDecorationsCollection());
        _modifiedDecorations = new EditorDecorationTarget(modified.CreateModelDecorationsCollection());
        viewModel.UnchangedRegionsChanged += OnRegionsChanged;
        original.ViewZonePointer += OnZonePointer;
        modified.ViewZonePointer += OnZonePointer;
        original.SelectionChanged += OnSelectionChanged;
        modified.SelectionChanged += OnSelectionChanged;
        Apply();
    }

    /// <summary>Applies the regions again (the view mode changed).</summary>
    public void Invalidate() => Apply();

    /// <summary>Stops listening and removes everything the controller added.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _viewModel.UnchangedRegionsChanged -= OnRegionsChanged;
        _original.ViewZonePointer -= OnZonePointer;
        _modified.ViewZonePointer -= OnZonePointer;
        _original.SelectionChanged -= OnSelectionChanged;
        _modified.SelectionChanged -= OnSelectionChanged;
        foreach (UnchangedRegion region in _subscribed)
        {
            region.Changed -= OnRegionChanged;
        }

        _subscribed = [];
        Apply(UnchangedRegionLayout.Empty, []);
        _originalDecorations.Clear();
        _modifiedDecorations.Clear();
    }

    private void OnRegionsChanged(object? sender, EventArgs e) => Apply();

    private void OnRegionChanged(object? sender, EventArgs e) => Apply();

    private void Apply()
    {
        if (_isDisposed)
        {
            return;
        }

        IReadOnlyList<UnchangedRegion> regions = _viewModel.UnchangedRegions;
        foreach (UnchangedRegion region in _subscribed)
        {
            region.Changed -= OnRegionChanged;
        }

        _subscribed = [.. regions];
        foreach (UnchangedRegion region in _subscribed)
        {
            region.Changed += OnRegionChanged;
        }

        Apply(UnchangedRegionZones.Compute(regions, _modified.Model.LineCount), regions);
    }

    private void Apply(UnchangedRegionLayout layout, IReadOnlyList<UnchangedRegion> regions)
    {
        _isApplying = true;
        try
        {
            // The hidden original keeps its hidden areas in the inline view so that its view lines match.
            _original.SetHiddenAreas(layout.OriginalHiddenAreas, this);
            _modified.SetHiddenAreas(layout.ModifiedHiddenAreas, this);
            ApplyZones(_original, _originalZones, _inline() ? [] : layout.OriginalZones);
            ApplyZones(_modified, _modifiedZones, layout.ModifiedZones);
            _originalDecorations.SetDecorations(layout.OriginalDecorations);
            _modifiedDecorations.SetDecorations(layout.ModifiedDecorations);
        }
        finally
        {
            _isApplying = false;
        }
    }

    // A bar keeps its zone (and so its id) while its region lives: a pointer capture on the zone survives a drag.
    private static void ApplyZones(LoomEditor editor, Dictionary<UnchangedRegion, string> own, IReadOnlyList<ZoneSpec> specs)
    {
        HashSet<UnchangedRegion> wanted = [.. specs.Select(s => ((CollapsedZoneData)s.Data!).Region)];
        editor.ViewZones.Change(accessor =>
        {
            foreach (UnchangedRegion region in own.Keys.Where(r => !wanted.Contains(r)).ToList())
            {
                accessor.RemoveZone(own[region]);
                own.Remove(region);
            }

            foreach (ZoneSpec spec in specs)
            {
                UnchangedRegion region = ((CollapsedZoneData)spec.Data!).Region;
                if (own.TryGetValue(region, out string? id) && editor.ViewZones.GetZone(id) is { } zone)
                {
                    zone.AfterLineNumber = spec.AfterLineNumber;
                    zone.Data = spec.Data;
                    accessor.LayoutZone(id);
                }
                else
                {
                    own[region] = accessor.AddZone(new ViewZone
                    {
                        AfterLineNumber = spec.AfterLineNumber,
                        HeightInPx = spec.HeightInPx,
                        Kind = spec.Kind,
                        Data = spec.Data,
                        ShowInHiddenAreas = true,
                        SuppressMouseDown = spec.SuppressMouseDown,
                    });
                }
            }
        });
    }

    // Monaco: hideUnchangedRegionsFeature.ts 56–75.
    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_isApplying || sender is not LoomEditor editor || editor.LastSelectionChangeReason == CursorChangeReason.ContentFlush)
        {
            return;
        }

        foreach (Selection selection in editor.Selections)
        {
            foreach (int line in new[] { selection.SelectionStartLineNumber, selection.PositionLineNumber })
            {
                if (ReferenceEquals(editor, _original))
                {
                    _viewModel.EnsureOriginalLineIsVisible(line, RevealPreference.FromCloserSide);
                }
                else
                {
                    _viewModel.EnsureModifiedLineIsVisible(line, RevealPreference.FromCloserSide);
                }
            }
        }
    }

    // Monaco: CollapsedCodeOverlayWidget, lines 413–540.
    private void OnZonePointer(object? sender, ViewZonePointerEventArgs e)
    {
        if (sender is not LoomEditor editor)
        {
            return;
        }

        switch (e.Phase)
        {
            case ViewZonePointerPhase.Pressed when e.IsLeftButton && e.Zone.Data is CollapsedZoneData data:
                UnchangedRegion region = data.Region;
                DragHandle handle = e.YInZone < HandleHeight ? DragHandle.Top
                    : e.YInZone >= UnchangedRegionZones.BarHeight - HandleHeight ? DragHandle.Bottom : DragHandle.Center;
                _drag = new Drag(region, handle, e.Y, e.Y, region.VisibleLineCountTop, region.VisibleLineCountBottom, editor);
                if (handle != DragHandle.Center)
                {
                    region.IsDragged = handle == DragHandle.Top ? UnchangedRegionDragSide.Top : UnchangedRegionDragSide.Bottom;
                }

                e.CapturePointerForZone();
                break;
            case ViewZonePointerPhase.Moved when _drag is { } drag && drag.Editor == editor:
                Move(drag, e.Y);
                break;
            case ViewZonePointerPhase.Released when _drag is { } drag && drag.Editor == editor:
                Release(drag, e.Y);
                break;
        }
    }

    private static void Move(Drag drag, double y)
    {
        if (double.IsNaN(y))
        {
            return;
        }

        drag.LastY = y;
        if (drag.Handle == DragHandle.Center)
        {
            return;
        }

        int lineDelta = (int)Math.Round((y - drag.StartY) / drag.Editor.LineHeight);
        UnchangedRegion region = drag.Region;
        if (drag.Handle == DragHandle.Top)
        {
            region.SetState(Math.Clamp(drag.StartTop + lineDelta, 0, region.GetMaxVisibleLineCountTop()), region.VisibleLineCountBottom);
        }
        else
        {
            region.SetState(region.VisibleLineCountTop, Math.Clamp(drag.StartBottom - lineDelta, 0, region.GetMaxVisibleLineCountBottom()));
        }
    }

    private void Release(Drag drag, double y)
    {
        _drag = null;
        double endY = double.IsNaN(y) ? drag.LastY : y;
        bool isClick = Math.Abs(endY - drag.StartY) <= ClickTolerance;
        UnchangedRegion region = drag.Region;
        (int Line, double Offset)? anchor = isClick ? CaptureAnchor(region) : null;

        region.IsDragged = null;
        if (isClick)
        {
            int count = Math.Max(1, _revealLineCount());
            switch (drag.Handle)
            {
                case DragHandle.Top:
                    region.ShowMoreAbove(count);
                    break;
                case DragHandle.Bottom:
                    region.ShowMoreBelow(count);
                    break;
                default:
                    region.ShowAll();
                    break;
            }
        }

        if (anchor is { } a)
        {
            // After the alignment of the two sides has settled (it runs in a normal priority tick).
            _dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => RestoreAnchor(a));
        }
    }

    // The screen position of the first modified line after the hidden range, to put it back after the layout changed.
    private (int Line, double Offset)? CaptureAnchor(UnchangedRegion region)
    {
        int line = region.GetHiddenModifiedRange().EndLineNumberExclusive;
        return line <= _modified.Model.LineCount ? (line, _modified.GetTopForLineNumber(line, true) - _modified.ScrollTop) : null;
    }

    private void RestoreAnchor((int Line, double Offset) anchor)
    {
        if (_isDisposed || anchor.Line > _modified.Model.LineCount)
        {
            return;
        }

        _modified.SetScrollPositionImmediate(null, Math.Max(0, _modified.GetTopForLineNumber(anchor.Line, true) - anchor.Offset));
    }

    private enum DragHandle
    {
        Top,
        Bottom,
        Center,
    }

    private sealed class Drag(UnchangedRegion region, DragHandle handle, double startY, double lastY, int startTop, int startBottom, LoomEditor editor)
    {
        public UnchangedRegion Region { get; } = region;

        public DragHandle Handle { get; } = handle;

        public double StartY { get; } = startY;

        public double LastY { get; set; } = lastY;

        public int StartTop { get; } = startTop;

        public int StartBottom { get; } = startBottom;

        public LoomEditor Editor { get; } = editor;
    }
}
