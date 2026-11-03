using Avalonia;
using Avalonia.Controls;
using LoomEditor.Avalonia.Controls.Hover;
using LoomEditor.Avalonia.Controls.Suggest;
using LoomEditor.Avalonia.Controls.Overlay;

namespace LoomEditor.Avalonia.Controls;

// The optional PART_OverlayCanvas and PART_ZoneWidgetCanvas of the template; without a part its widgets have no UI.
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class LoomEditor
{
    private OverlayHost? _overlay;
    private HoverPresenter? _hoverPresenter;
    private SuggestPresenter? _suggestPresenter;

    // The overlay for the widgets that float above the text, for the widget cards that follow.
    internal OverlayHost? Overlay => _overlay;

    private void AttachOverlays(INameScope nameScope)
    {
        _overlay = nameScope.Find<Canvas>(OverlayCanvasPartName) is { } overlay ? new OverlayHost(overlay) : null;
        if (_overlay is null)
        {
            _hoverPresenter = null;
            _suggestPresenter = null;
        }
        else
        {
            _hoverPresenter = new HoverPresenter(_overlay, this);
            _hoverPresenter.ApplyTheme(EffectiveTheme);
            _context.HoverPresenter = _hoverPresenter;

            _suggestPresenter = new SuggestPresenter(_overlay, this);
            _suggestPresenter.ApplyTheme(EffectiveTheme);
            _context.SuggestPresenter = _suggestPresenter;
        }

        _hostZoneLayer.Attach(nameScope.Find<Canvas>(ZoneWidgetCanvasPartName) is { } zones ? new OverlayHost(zones) : null);
    }

    // The bounds, in editor coordinates, of the first visible line of a model range: from its start to its end on that line (a caret-wide
    // box for an empty range). False when that line is not in the viewport. The twin of GetBoundingRectangles of the WinUI automation geometry.
    internal bool TryGetRangeStartBounds(Range modelRange, out Rect bounds)
    {
        bounds = default;
        if (_renderer is null)
        {
            return false;
        }

        Range range = _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(_context.Model.ValidateRange(modelRange));
        var data = _viewModel.ViewLayout.GetLinesViewportData();
        int line = range.StartLineNumber;
        if (line < data.StartLineNumber || line > data.EndLineNumber)
        {
            return false;
        }

        int endColumn = range.EndLineNumber == line ? range.EndColumn : _viewModel.GetLineMaxColumn(line);
        if (!_renderer.TryGetHorizontalRange(_viewModel, line, range.StartColumn, endColumn, out double left, out double width))
        {
            return false;
        }

        var layout = _viewModel.LayoutInfo;
        double x = Math.Max(layout.ContentLeft, layout.ContentLeft + left - _viewModel.ViewLayout.GetCurrentScrollLeft());
        double y = _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(line) - _viewModel.ViewLayout.GetCurrentScrollTop();
        bounds = new Rect(x, y, range.IsEmpty ? 1 : Math.Max(1, width), _viewModel.FontInfo.LineHeight);
        return true;
    }
}
