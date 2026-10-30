using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;

namespace LoomEditor.WinUI.Controls;

// What a host of this control needs: the public members for applications (phase 13) and the internal ones for the diff
// editor. Monaco: the `ICodeEditor` members used by `DiffEditorEditors`.
public sealed partial class LoomEditor
{
    private string? _capturedZoneId;
    private double _capturedZoneTop;
    private bool _overviewRulerEnabled = true;
    private bool _hideLineNumbers;

    /// <summary>Raised when the scroll position or the scroll height changes: scrolling, edits, folding, view zones, wrapping.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public event EventHandler<EditorScrollChangedEventArgs>? ScrollChanged;

    /// <summary>Raised when the layout of the editor changes: size, gutter widths, minimap, line numbers, font and line height.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public event EventHandler? LayoutChanged;

    /// <summary>Raised for the pointer over a view zone (press, move, release).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal event EventHandler<ViewZonePointerEventArgs>? ViewZonePointer;

    /// <summary>The view zones of the editor.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal ViewZones ViewZones => _viewModel.ViewZones;

    /// <summary>Converts between model and view positions (needed to map whitespaces and wrapped lines to model lines).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal ICoordinatesConverter CoordinatesConverter => _viewModel.CoordinatesConverter;

    /// <summary>Whether lines wrap.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal bool IsWrapping => WordWrap != EditorWordWrap.Off;
    /// <summary>Why the selections changed last (a flush means the text was reset).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal global::LoomEditor.View.Cursors.CursorChangeReason LastSelectionChangeReason => _context.LastSelectionChangeReason;

    /// <summary>The height of a line, in DIPs. It changes with the font size and the zoom: read it again on <see cref="LayoutChanged"/>.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public double LineHeight => _viewModel.FontInfo.LineHeight;

    /// <summary>The layout of the editor (read-only).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal EditorLayoutInfo LayoutInfo => _viewModel.LayoutInfo;

    internal IReadOnlyList<ViewWhitespaceViewportData> GetWhitespaceViewportData() => _viewModel.ViewLayout.GetWhitespaceViewportData();

    internal void InvalidateCanvas() => _canvas?.Invalidate();

    /// <summary>The whitespaces of the layout, the view zones included (Monaco: <c>getWhitespaces</c>).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal IReadOnlyList<IEditorWhitespace> GetWhitespaces() => _viewModel.ViewLayout.GetWhitespaces();

    /// <summary>
    /// The vertical offset of a model line in scroll coordinates (Monaco: <c>getTopForLineNumber</c>); subtract <see cref="ScrollTop"/> for a y in the editor.
    /// <paramref name="includeViewZones"/> adds the zones above the line, so the result is the top of the line's text. A wrapped line gives the top of its first
    /// row. A line inside a hidden area gives the top of the last row of the nearest visible line above it (the top of the first line when none is visible).
    /// </summary>
    /// <param name="lineNumber">The model line number; it is clamped to <c>1 … line count</c>.</param>
    /// <param name="includeViewZones">Whether to add the height of the view zones above the line.</param>
    /// <returns>The offset, in DIPs.</returns>
    /// <remarks>Threading: UI thread only.</remarks>
    public double GetTopForLineNumber(int lineNumber, bool includeViewZones = false)
    {
        int clamped = Math.Clamp(lineNumber, 1, _context.Model.LineCount);
        int viewLine = _viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(clamped, 1)).LineNumber;
        return _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(viewLine, includeViewZones);
    }

    /// <summary>The top of a view line in scroll coordinates (Monaco: <c>viewLayout.getVerticalOffsetForLineNumber</c>).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal int GetVerticalOffsetForViewLine(int viewLineNumber) => _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(viewLineNumber);

    /// <summary>The height of the scrollable content, in DIPs.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal double GetScrollHeight() => _viewModel.ViewLayout.GetScrollable().GetScrollDimensions().ScrollHeight;

    /// <summary>
    /// Scrolls without animation (Monaco: <c>ScrollType.Immediate</c>). The caller guards against feedback loops through
    /// <see cref="ScrollChanged"/>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal void SetScrollPositionImmediate(double? scrollLeft, double? scrollTop)
    {
        if (scrollLeft is { } left)
        {
            _pendingScrollLeft = left;
        }

        if (scrollTop is { } top)
        {
            _pendingScrollTop = top;
        }

        if (_measurer is not null)
        {
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(scrollLeft, scrollTop), ScrollType.Immediate);
        }
    }

    /// <summary>
    /// Replaces the text of the model without going through <see cref="Text"/>, whose value is not kept in sync with the edits (a
    /// host that sets the same string twice must still reset the model).
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal void ApplyText(string text)
    {
        _context.SetText(text);
        _viewModel.SetIsDominatedByLongLines(_context.Model.IsDominatedByLongLines());
    }

    /// <summary>Hides ranges of lines (Monaco: <c>setHiddenAreas</c>).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal void SetHiddenAreas(IReadOnlyList<Core.Text.Range> ranges, object source) => _viewModel.SetHiddenAreas(ranges, source);

    /// <summary>Creates a collection of decorations owned by the caller.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal EditorDecorationsCollection CreateModelDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>? decorations = null) =>
        _context.CreateDecorationsCollection(decorations);

    /// <summary>Whether the decorations overview ruler of the editor is on. The diff editor draws its own.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal bool OverviewRulerEnabled
    {
        get => _overviewRulerEnabled;
        set
        {
            _overviewRulerEnabled = value;
            ApplyHostLayoutOptions();
        }
    }

    /// <summary>Whether the line numbers are hidden.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal bool HideLineNumbers
    {
        get => _hideLineNumbers;
        set
        {
            _hideLineNumbers = value;
            ApplyHostLayoutOptions();
        }
    }

    private void ApplyHostLayoutOptions()
    {
        EditorLayoutOptions options = _viewModel.LayoutOptions;
        EditorOverviewRulerOptions ruler = options.OverviewRuler with { Lanes = _overviewRulerEnabled ? new EditorOverviewRulerOptions().Lanes : 0 };
        _viewModel.SetOptions(options with { OverviewRuler = ruler, LineNumbers = !_hideLineNumbers }, _viewModel.ViewLayoutOptions);
    }

    private void RaiseScrollChanged(ScrollEvent e) =>
        ScrollChanged?.Invoke(
            this, new EditorScrollChangedEventArgs(e.ScrollTopChanged, e.ScrollLeftChanged, e.ScrollHeightChanged, e.ScrollTop, e.ScrollLeft));

    private void RaiseContentSizeChanged() =>
        ScrollChanged?.Invoke(
            this,
            new EditorScrollChangedEventArgs(
                false, false, true, _viewModel.ViewLayout.GetCurrentScrollTop(), _viewModel.ViewLayout.GetCurrentScrollLeft()));

    // The zone under the pointer (the point is in editor coordinates) and the top of the zone in the document.
    private (ViewZone Zone, double YInZone, double Top)? HitTestZone(EditorMouseEvent mouseEvent)
    {
        if (!_viewModel.ViewZones.Zones.Any())
        {
            return null;
        }

        EditorLayoutInfo layout = _viewModel.LayoutInfo;
        double y = mouseEvent.Y + _viewModel.ViewLayout.GetCurrentScrollTop();
        IReadOnlyList<ViewZoneViewportData> zones = _viewModel.GetViewportData(_context.Selections).ZoneViewportData;
        return ViewZoneViewportData.HitTest(zones, mouseEvent.X, y, layout.ContentLeft, layout.ContentWidth) is { } hit
            ? (hit.Zone, hit.YInZone, y - hit.YInZone)
            : null;
    }

    // Returns true when the press is over a zone that swallows it (`SuppressMouseDown`).
    private bool HandleZonePress(EditorMouseEvent mouseEvent, Microsoft.UI.Xaml.Input.Pointer pointer)
    {
        if (HitTestZone(mouseEvent) is not { } hit)
        {
            return false;
        }

        var args = new ViewZonePointerEventArgs(hit.Zone, ViewZonePointerPhase.Pressed, mouseEvent.X, hit.YInZone, mouseEvent.LeftButton, mouseEvent.Y);
        ViewZonePointer?.Invoke(this, args);
        if (args.CaptureRequested)
        {
            _capturedZoneId = hit.Zone.Id;
            _capturedZoneTop = hit.Top;
            CapturePointer(pointer);
        }

        return _viewModel.ViewZones.ShouldSuppressMouseDown(hit.Zone.Id);
    }

    // Raises a move or a release for the captured zone, else a move for the hovered one. True when a capture consumed it.
    private bool HandleZoneMove(EditorMouseEvent mouseEvent, ViewZonePointerPhase phase)
    {
        if (_capturedZoneId is { } id)
        {
            if (_viewModel.ViewZones.GetZone(id) is { } captured)
            {
                double y = mouseEvent.Y + _viewModel.ViewLayout.GetCurrentScrollTop();
                foreach (ViewZoneViewportData data in _viewModel.GetViewportData(_context.Selections).ZoneViewportData)
                {
                    if (data.Zone.Id == id)
                    {
                        _capturedZoneTop = data.VerticalOffset;
                    }
                }

                ViewZonePointer?.Invoke(
                    this, new ViewZonePointerEventArgs(captured, phase, mouseEvent.X, y - _capturedZoneTop, mouseEvent.LeftButton, mouseEvent.Y));
            }

            if (phase == ViewZonePointerPhase.Released)
            {
                _capturedZoneId = null;
            }

            return true;
        }

        if (phase == ViewZonePointerPhase.Moved && HitTestZone(mouseEvent) is { } hit)
        {
            ViewZonePointer?.Invoke(this, new ViewZonePointerEventArgs(hit.Zone, phase, mouseEvent.X, hit.YInZone, mouseEvent.LeftButton, mouseEvent.Y));
        }

        return false;
    }

    // The capture ended without a release (Monaco has no equivalent: the browser always sends mouseup).
    private void EndZoneCapture()
    {
        if (_capturedZoneId is { } id && _viewModel.ViewZones.GetZone(id) is { } zone)
        {
            ViewZonePointer?.Invoke(this, new ViewZonePointerEventArgs(zone, ViewZonePointerPhase.Released, 0, 0, false));
        }

        _capturedZoneId = null;
    }
}

/// <summary>The phases of a pointer interaction with a view zone.</summary>
internal enum ViewZonePointerPhase
{
    /// <summary>A button went down on the zone.</summary>
    Pressed,

    /// <summary>The pointer moved over the zone, or during a capture.</summary>
    Moved,

    /// <summary>The button went up (or the capture was lost).</summary>
    Released,
}

/// <summary>A pointer event on a view zone.</summary>
/// <param name="zone">The zone.</param>
/// <param name="phase">Press, move or release.</param>
/// <param name="x">The x of the pointer in the editor.</param>
/// <param name="yInZone">The y of the pointer from the top of the zone.</param>
/// <param name="isLeftButton">Whether the left button is involved.</param>
/// <param name="y">The y of the pointer in the editor (not the zone), or <see cref="double.NaN"/> when unknown.</param>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ViewZonePointerEventArgs(ViewZone zone, ViewZonePointerPhase phase, double x, double yInZone, bool isLeftButton, double y = double.NaN) : EventArgs
{
    /// <summary>The zone.</summary>
    public ViewZone Zone { get; } = zone;

    /// <summary>Press, move or release.</summary>
    public ViewZonePointerPhase Phase { get; } = phase;

    /// <summary>The x of the pointer in the editor.</summary>
    public double X { get; } = x;

    /// <summary>The y of the pointer from the top of the zone.</summary>
    public double YInZone { get; } = yInZone;

    /// <summary>Whether the left button is involved.</summary>
    public bool IsLeftButton { get; } = isLeftButton;

    /// <summary>The y of the pointer in the editor (not the zone), or NaN when unknown.</summary>
    public double Y { get; } = y;

    internal bool CaptureRequested { get; private set; }

    /// <summary>Makes the control send the next moves and the release of this pointer to the zone, even outside it.</summary>
    internal void CapturePointerForZone() => CaptureRequested = true;
}
