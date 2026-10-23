using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

public class MinimapPointerControllerTests
{
    private const int Height = 600;

    private sealed class FakeModel(MinimapOptions options, int lineCount) : IMinimapModel
    {
        public int? RevealedLine { get; private set; }

        public double? ScrollTop { get; private set; }

        public MinimapTokensColorTracker TokensColorTracker => throw new NotSupportedException();

        public MinimapOptions Options => options;

        public int GetLineCount() => lineCount;

        public int GetRealLineCount() => lineCount;

        public string GetLineContent(int lineNumber) => throw new NotSupportedException();

        public int GetLineMaxColumn(int lineNumber) => throw new NotSupportedException();

        public ViewLineData?[] GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed) => throw new NotSupportedException();

        public IReadOnlyList<Selection> GetSelections() => throw new NotSupportedException();

        public IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInViewport(int startLineNumber, int endLineNumber) => throw new NotSupportedException();

        public TextModelOptions GetOptions() => throw new NotSupportedException();

        public void RevealLineNumber(int lineNumber) => RevealedLine = lineNumber;

        public void SetScrollTop(double scrollTop) => ScrollTop = scrollTop;
    }

    private sealed class Presentation : IMinimapPresentation
    {
        public bool MouseOverMinimap { get; set; }

        public bool SliderHovered { get; set; }

        public bool SliderActive { get; set; }
    }

    private static MinimapOptions CreateOptions(MinimapSize size)
    {
        bool heightIsEditorHeight = size != MinimapSize.Proportional;
        var layout = new EditorLayoutInfo(
            Width: 800, Height: Height, GlyphMarginLeft: 0, GlyphMarginWidth: 0, GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0, LineNumbersWidth: 42, DecorationsLeft: 42, DecorationsWidth: 10, ContentLeft: 52, ContentWidth: 634,
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.Text, 686, 100, heightIsEditorHeight, false, 1, 2, 100, Height, 100, Height),
            ViewportColumn: 90, IsViewportWrapping: false, WrappingColumn: -1, VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12, OverviewRuler: new OverviewRulerPosition(14, Height, 0, 0));
        var theme = EditorTheme.Dark;
        return new MinimapOptions(
            layout, new EditorMinimapOptions { Size = size }, FontInfo.Default, false, theme, new MinimapTokensColorTracker(theme),
            (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
    }

    private static (MinimapPointerController Controller, FakeModel Model, Presentation State, MinimapLayout Layout, MinimapOptions Options) Create(
        MinimapSize size, int lineCount = 300, double scrollTop = 0)
    {
        MinimapOptions options = CreateOptions(size);
        double scrollHeight = lineCount * (double)options.LineHeight;
        int viewportStart = (int)(scrollTop / options.LineHeight) + 1;
        int viewportEnd = Math.Min(lineCount, viewportStart + (Height / options.LineHeight));
        MinimapLayout layout = MinimapLayout.Create(
            options, viewportStart, viewportEnd, (viewportStart - 1) * (double)options.LineHeight, Height, false,
            lineCount, lineCount, scrollTop, scrollHeight, null);
        var model = new FakeModel(options, lineCount);
        var state = new Presentation();
        return (new MinimapPointerController(model, () => layout, state), model, state, layout, options);
    }

    [Fact]
    public void Click_Proportional_RevealsTheLineUnderThePointer()
    {
        var (controller, model, _, layout, options) = Create(MinimapSize.Proportional);
        double y = layout.SliderTop + layout.SliderHeight + 40;

        bool handled = controller.PointerDown(10, y, isLeftButton: true, isMouse: true);

        int expected = (int)Math.Floor(options.CanvasInnerHeight / options.CanvasOuterHeight * y / options.MinimapLineHeight)
            + layout.StartLineNumber - layout.TopPaddingLineCount;
        Assert.True(handled);
        Assert.Equal(expected, model.RevealedLine);
        Assert.False(controller.IsActive);
    }

    [Fact]
    public void Click_FitSize_StartsDraggingFromTheSliderCenter()
    {
        var (controller, model, state, layout, _) = Create(MinimapSize.Fit);
        double center = layout.SliderTop + (layout.SliderHeight / 2);

        controller.PointerDown(10, center + 50, isLeftButton: true, isMouse: true);

        Assert.True(controller.IsActive);
        Assert.True(state.SliderActive);
        Assert.Null(model.RevealedLine);
        Assert.Equal(layout.GetDesiredScrollTopFromDelta(50), model.ScrollTop);
    }

    [Fact]
    public void DragSlider_ScrollsByTheSliderRatio()
    {
        var (controller, model, state, layout, _) = Create(MinimapSize.Proportional);
        double y = layout.SliderTop + (layout.SliderHeight / 2);

        controller.PointerDown(10, y, isLeftButton: true, isMouse: true);
        Assert.Null(model.ScrollTop);
        controller.PointerMove(12, y + 30);
        Assert.Equal(layout.GetDesiredScrollTopFromDelta(30), model.ScrollTop);
        controller.PointerUp();

        Assert.False(controller.IsActive);
        Assert.False(state.SliderActive);
    }

    [Fact]
    public void DragSlider_PointerFarAway_ResetsTheScroll()
    {
        var (controller, model, _, layout, _) = Create(MinimapSize.Proportional, scrollTop: 1000);
        double y = layout.SliderTop + (layout.SliderHeight / 2);

        controller.PointerDown(10, y, isLeftButton: true, isMouse: true);
        controller.PointerMove(-MinimapOptions.PointerDragResetDistance - 10, y + 30);

        Assert.Equal(layout.ScrollTop, model.ScrollTop);
    }

    [Fact]
    public void RightClick_Mouse_DoesNothing()
    {
        var (controller, model, state, layout, _) = Create(MinimapSize.Proportional);

        controller.PointerDown(10, layout.SliderTop + layout.SliderHeight + 40, isLeftButton: false, isMouse: true);

        Assert.Null(model.RevealedLine);
        Assert.Null(model.ScrollTop);
        Assert.False(state.SliderActive);
    }

    [Fact]
    public void Touch_ScrollsToTheTouchLocation()
    {
        var (controller, model, state, layout, _) = Create(MinimapSize.Proportional);

        controller.PointerDown(10, 200, isLeftButton: true, isMouse: false);
        Assert.Equal(layout.GetDesiredScrollTopFromTouchLocation(200), model.ScrollTop);
        Assert.True(state.SliderActive);

        controller.PointerMove(10, 260);
        Assert.Equal(layout.GetDesiredScrollTopFromTouchLocation(260), model.ScrollTop);

        controller.PointerUp();
        Assert.False(state.SliderActive);
    }

    [Fact]
    public void Hover_TracksTheSliderAndTheMinimap()
    {
        var (controller, _, state, layout, _) = Create(MinimapSize.Proportional);

        controller.PointerEntered();
        controller.PointerMove(10, layout.SliderTop + 1);
        Assert.True(state.MouseOverMinimap);
        Assert.True(state.SliderHovered);

        controller.PointerMove(10, layout.SliderTop + layout.SliderHeight + 20);
        Assert.False(state.SliderHovered);

        controller.PointerExited();
        Assert.False(state.MouseOverMinimap);
    }
}
