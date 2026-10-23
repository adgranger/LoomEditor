// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (InnerMinimap)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.Rendering;

// Behavior differences (minimap.css): the 100 ms slider transition and the 0.5 s auto-hide fade are not animated; the
// shadow is a solid 6-DIP band filled with `scrollbar.shadow` instead of Monaco's `--vscode-shadow-md` box shadow (no
// gradient). The two canvases are two bitmaps drawn at 90 % opacity; the DOM node and its CSS classes are the
// `MinimapPresentationState`.
internal sealed partial class TextViewRenderer
{
    private const float MinimapCanvasOpacity = 0.9f;
    private const int MinimapShadowWidth = 6;

    private ViewModel? _minimapViewModel;
    private MinimapModel? _minimapModel;
    private MinimapLinesRenderer? _minimapLines;
    private MinimapDecorationsRenderer? _minimapDecorations;
    private MinimapLayout? _lastMinimapLayout;
    private double _lastMinimapScrollTop = double.NaN;
    private double _lastMinimapScrollHeight = double.NaN;

    /// <summary>
    /// Gives the font sample data (RGBA bytes, see <c>MinimapCharSheet</c>) of a font family; needed for minimap scales above 2.
    /// Without it the pre-baked scale 2 characters are used.
    /// </summary>
    public Func<string, byte[]>? MinimapSampleDataProvider { get; set; }

    /// <summary>Hover, drag and auto-hide state of the minimap.</summary>
    public MinimapPresentationState MinimapPresentation { get; }

    /// <summary>Pointer input of the minimap; <c>null</c> until the minimap was drawn once or while it is not rendered.</summary>
    public MinimapPointerController? MinimapPointer { get; private set; }

    /// <summary>The minimap rectangle in editor DIPs, or <c>false</c> when there is no minimap to hit.</summary>
    public bool TryGetMinimapRectangle(out double left, out double top, out double width, out double height)
    {
        if (_minimapModel?.Options is { RenderMinimap: not RenderMinimap.None } options && MinimapPointer is not null)
        {
            (left, top, width, height) = (options.MinimapLeft, 0, options.MinimapWidth, options.MinimapHeight);
            return true;
        }

        (left, top, width, height) = (0, 0, 0, 0);
        return false;
    }

    // Monaco: `InnerMinimap.render`.
    private void DrawMinimap(IRenderSurface surface, ViewModel viewModel, ViewportData data, EditorLayoutInfo layoutInfo)
    {
        if (layoutInfo.Minimap.RenderMinimap == RenderMinimap.None)
        {
            DisposeMinimap();
            return;
        }

        MinimapModel model = EnsureMinimapModel(viewModel);
        model.UpdateOptions(Theme);
        model.PrepareRender();
        MinimapOptions options = model.Options;

        ViewLayout viewLayout = viewModel.ViewLayout;
        Viewport viewport = viewLayout.GetCurrentViewport();
        double scrollTop = viewLayout.GetCurrentScrollTop();
        double scrollHeight = viewLayout.GetScrollHeight();
        if (scrollTop != _lastMinimapScrollTop || scrollHeight != _lastMinimapScrollHeight)
        {
            // Monaco: `onScrollChanged`; the first frame is not a scroll.
            if (!double.IsNaN(_lastMinimapScrollTop) && options.Autohide == MinimapAutohide.Scroll)
            {
                MinimapPresentation.OnScrolled();
            }

            _minimapDecorations!.OnLayoutChanged();
            _lastMinimapScrollTop = scrollTop;
            _lastMinimapScrollHeight = scrollHeight;
        }

        var layout = MinimapLayout.Create(
            options, data.StartLineNumber, data.EndLineNumber, viewLayout.GetVerticalOffsetForLineNumber(data.StartLineNumber),
            viewport.Height, data.WhitespaceViewportData.Count > 0, model.GetLineCount(), model.GetRealLineCount(),
            scrollTop, scrollHeight, _lastMinimapLayout);
        _lastMinimapLayout = layout;
        MinimapImageData? decorations = _minimapDecorations!.RenderDecorations(layout);
        MinimapImageData? lines = _minimapLines!.RenderLines(layout);

        if (!IsMinimapVisible(options))
        {
            return;
        }

        if (viewLayout.GetCurrentScrollLeft() + viewport.Width < viewLayout.GetScrollWidth()
            && Theme.GetColor("scrollbar.shadow") is { A: > 0 } shadow)
        {
            surface.FillRectangle(options.MinimapLeft - MinimapShadowWidth, 0, MinimapShadowWidth, options.MinimapHeight, shadow);
        }

        DrawMinimapImage(surface, lines, options);
        DrawMinimapImage(surface, decorations, options);

        if (layout.SliderNeeded
            && (options.ShowSlider == MinimapShowSlider.Always || MinimapPresentation.MouseOverMinimap || MinimapPresentation.SliderActive)
            && GetSliderColor() is { } sliderColor)
        {
            surface.FillRectangle(options.MinimapLeft, (float)layout.SliderTop, options.MinimapWidth, (float)layout.SliderHeight, sliderColor);
        }
    }

    // Monaco: the `minimap-autohide-*` classes of minimap.css.
    private bool IsMinimapVisible(MinimapOptions options) => options.Autohide switch
    {
        MinimapAutohide.MouseOver => MinimapPresentation.MouseOverMinimap || MinimapPresentation.SliderActive,
        MinimapAutohide.Scroll => MinimapPresentation.ScrollActive,
        _ => true,
    };

    private static void DrawMinimapImage(IRenderSurface surface, MinimapImageData? image, MinimapOptions options)
    {
        if (image is not null)
        {
            surface.DrawRgbaImage(
                image.Data, image.Width, image.Height, options.MinimapLeft, 0, (float)options.CanvasOuterWidth, (float)options.CanvasOuterHeight,
                MinimapCanvasOpacity, image.Version);
        }
    }

    // Monaco: `.minimap-slider-horizontal`, `:hover` and `.active` of minimap.css.
    private ThemeColor? GetSliderColor() => Theme.GetColor(
        MinimapPresentation.SliderActive ? "minimapSlider.activeBackground"
        : MinimapPresentation.SliderHovered ? "minimapSlider.hoverBackground"
        : "minimapSlider.background");

    private MinimapModel EnsureMinimapModel(ViewModel viewModel)
    {
        if (_minimapModel is not null && ReferenceEquals(_minimapViewModel, viewModel))
        {
            return _minimapModel;
        }

        DisposeMinimap();
        _minimapViewModel = viewModel;
        var target = new MinimapRenderTarget(this);
        _minimapModel = new MinimapModel(
            viewModel, target, Theme,
            (scale, fontFamily) => MinimapCharRendererFactory.Create(scale, fontFamily, MinimapSampleDataProvider));
        _minimapLines = new MinimapLinesRenderer(_minimapModel);
        _minimapDecorations = new MinimapDecorationsRenderer(_minimapModel, Theme);
        MinimapPointer = new MinimapPointerController(_minimapModel, () => _lastMinimapLayout, MinimapPresentation);
        return _minimapModel;
    }

    private void DisposeMinimap()
    {
        _minimapModel?.Dispose();
        _minimapModel = null;
        _minimapViewModel = null;
        _minimapLines = null;
        _minimapDecorations = null;
        MinimapPointer = null;
        _lastMinimapLayout = null;
        _lastMinimapScrollTop = double.NaN;
        _lastMinimapScrollHeight = double.NaN;
    }

    /// <summary>What the minimap model reports, forwarded to the two renderers (Monaco: the <c>InnerMinimap</c> event handlers).</summary>
    private sealed class MinimapRenderTarget(TextViewRenderer owner) : IMinimapRenderTarget
    {
        public void OnDidChangeOptions()
        {
            owner._minimapLines?.OnDidChangeOptions();
            owner._minimapDecorations?.OnDidChangeOptions();
        }

        public void OnFlushed()
        {
            owner._minimapLines?.OnFlushed();
            owner._minimapDecorations?.OnFlushed();
        }

        public void OnLinesChanged(int changeFromLineNumber, int changeCount) => owner._minimapLines?.OnLinesChanged(changeFromLineNumber, changeCount);

        public void OnLinesDeleted(int deleteFromLineNumber, int deleteToLineNumber) => owner._minimapLines?.OnLinesDeleted(deleteFromLineNumber, deleteToLineNumber);

        public void OnLinesInserted(int insertFromLineNumber, int insertToLineNumber) => owner._minimapLines?.OnLinesInserted(insertFromLineNumber, insertToLineNumber);

        public void OnTokensChanged(IReadOnlyList<ViewLineRange> ranges) => owner._minimapLines?.OnTokensChanged(ranges);

        public void OnSelectionChanged() => owner._minimapDecorations?.OnSelectionChanged();

        public void OnDecorationsChanged() => owner._minimapDecorations?.OnDecorationsChanged();

        // Monaco: `onThemeChanged` and `onTokensColorsChanged`.
        public void OnThemeChanged()
        {
            owner._minimapLines?.OnTokensColorsChanged();
            owner._minimapDecorations?.OnThemeChanged(owner.Theme);
        }
    }
}
