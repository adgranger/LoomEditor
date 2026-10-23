using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

// All the tests are new: Monaco has none for the minimap decorations.
public class MinimapDecorationsRendererTests
{
    private const int Height = 600;
    private const int Width = 100;
    private const int MinimapLineHeight = 2;
    private const int Gutter = 8;

    private sealed class FakeMinimapModel(MinimapOptions options) : IMinimapModel
    {
        public List<Selection> Selections { get; } = [];

        public List<ViewModelDecoration> Decorations { get; } = [];

        public Dictionary<int, string> Contents { get; } = [];

        public MinimapTokensColorTracker TokensColorTracker { get; } = new(EditorTheme.Dark);

        public MinimapOptions Options { get; } = options;

        public int GetLineCount() => 1000;

        public int GetRealLineCount() => 1000;

        public string GetLineContent(int lineNumber) => Contents.GetValueOrDefault(lineNumber, "abcdef");

        public int GetLineMaxColumn(int lineNumber) => GetLineContent(lineNumber).Length + 1;

        public ViewLineData?[] GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed) => new ViewLineData?[needed.Length];

        public IReadOnlyList<Selection> GetSelections() => Selections;

        public IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInViewport(int startLineNumber, int endLineNumber) => Decorations;

        public TextModelOptions GetOptions() => TextModelOptions.Default;

        public void RevealLineNumber(int lineNumber)
        {
        }

        public void SetScrollTop(double scrollTop)
        {
        }
    }

    private static MinimapOptions CreateOptions()
    {
        var layout = new EditorLayoutInfo(
            Width: 800, Height: Height, GlyphMarginLeft: 0, GlyphMarginWidth: 0, GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0, LineNumbersWidth: 42, DecorationsLeft: 42, DecorationsWidth: 10, ContentLeft: 52, ContentWidth: 634,
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.Text, 686, Width, false, false, 1, MinimapLineHeight, Width, Height, Width, Height),
            ViewportColumn: 90, IsViewportWrapping: false, WrappingColumn: -1, VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12, OverviewRuler: new OverviewRulerPosition(14, Height, 0, 0));
        EditorTheme theme = EditorTheme.Dark;
        return new MinimapOptions(
            layout, new EditorMinimapOptions(), FontInfo.Default, false, theme, new MinimapTokensColorTracker(theme),
            (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
    }

    private static MinimapLayout CreateLayout(MinimapOptions options)
    {
        const int lineCount = 10;
        double scrollHeight = lineCount * (double)options.LineHeight;
        return MinimapLayout.Create(options, 1, lineCount, 0, Height, false, lineCount, lineCount, 0, scrollHeight, null);
    }

    private static ViewModelDecoration Decoration(Range range, string color, MinimapPosition position = MinimapPosition.Inline, int zIndex = 0) =>
        new(range, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "minimap",
            ZIndex = zIndex,
            Minimap = new ModelDecorationMinimapOptions(DecorationColor.FromValue(color), null, position),
        }));

    private static byte[] Pixel(MinimapImageData image, int x, int y)
    {
        int offset = ((y * image.Width) + x) * 4;
        return [image.Data[offset], image.Data[offset + 1], image.Data[offset + 2], image.Data[offset + 3]];
    }

    private static (MinimapDecorationsRenderer Renderer, FakeMinimapModel Model, MinimapLayout Layout, int CharWidth) Create()
    {
        MinimapOptions options = CreateOptions();
        var model = new FakeMinimapModel(options);
        return (new MinimapDecorationsRenderer(model, EditorTheme.Dark), model, CreateLayout(options), (int)options.MinimapCharWidth);
    }

    [Fact]
    public void Selection_PaintsALineHighlightAndAColumnHighlight()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, int cw) = Create();
        model.Selections.Add(new Selection(2, 2, 4, 3));

        MinimapImageData image = renderer.RenderDecorations(layout)!;

        // Monaco's line highlight spans from the first to the start of the last line (the last line only gets the column highlight).
        // The line highlight is half transparent, the column highlight is the (opaque) selection color over it.
        Assert.Equal(127, Pixel(image, 50, 2)[3]);
        Assert.Equal(127, Pixel(image, 50, 4)[3]);
        Assert.Equal(0, Pixel(image, 50, 6)[3]);
        Assert.Equal(0, Pixel(image, 50, 0)[3]);
        Assert.Equal(0, Pixel(image, 2, 2)[3]);
        Assert.Equal(255, Pixel(image, Gutter + cw, 2)[3]);
        Assert.Equal(255, Pixel(image, Gutter, 6)[3]);
        Assert.Equal(0, Pixel(image, Gutter + (2 * cw), 6)[3]);
    }

    [Fact]
    public void InlineDecoration_PaintsItsColumns()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, int cw) = Create();
        model.Decorations.Add(Decoration(new Range(1, 1, 1, 3), "#ff0000"));

        MinimapImageData image = renderer.RenderDecorations(layout)!;

        Assert.Equal([255, 0, 0, 255], Pixel(image, Gutter, 0));
        Assert.Equal([255, 0, 0, 255], Pixel(image, Gutter + (2 * cw) - 1, 0));
        Assert.Equal(127, Pixel(image, Gutter + (2 * cw), 0)[3]);
    }

    [Fact]
    public void GutterDecoration_PaintsTheGutterStrip()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, _) = Create();
        model.Decorations.Add(Decoration(new Range(3, 1, 3, 3), "#00ff00", MinimapPosition.Gutter));

        MinimapImageData image = renderer.RenderDecorations(layout)!;

        Assert.Equal([0, 255, 0, 255], Pixel(image, 2, 4));
        Assert.Equal([0, 255, 0, 255], Pixel(image, 3, 5));
        Assert.Equal(0, Pixel(image, 4, 4)[3]);
        Assert.Equal(0, Pixel(image, Gutter, 4)[3]);
    }

    [Fact]
    public void HigherZIndexDecoration_WinsTheLineHighlight()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, _) = Create();
        model.Decorations.Add(Decoration(new Range(1, 1, 1, 2), "#ff0000", zIndex: 5));
        model.Decorations.Add(Decoration(new Range(1, 1, 1, 2), "#0000ff", zIndex: 1));

        MinimapImageData image = renderer.RenderDecorations(layout)!;

        Assert.Equal([255, 0, 0, 127], Pixel(image, 50, 0));
    }

    [Fact]
    public void Selection_HasPriorityOverDecorations()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, _) = Create();
        model.Selections.Add(new Selection(1, 1, 2, 2));
        model.Decorations.Add(Decoration(new Range(1, 1, 1, 2), "#ff0000", zIndex: 5));

        MinimapImageData image = renderer.RenderDecorations(layout)!;

        // the line highlight is the selection's, not the (red) decoration's
        byte[] pixel = Pixel(image, 50, 0);
        Assert.Equal(127, pixel[3]);
        Assert.NotEqual(255, pixel[0]);
    }

    [Fact]
    public void GetXOffsetForPosition_TabsAndFullWidthCharacters()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, int cw) = Create();
        model.Contents[1] = "\tあb";
        var map = new ContiguousLineMap<int[]?>(1, 10, null);

        Assert.Equal(Gutter, renderer.GetXOffsetForPosition(map, 1, 1, 4, cw));
        Assert.Equal(Gutter + (4 * cw), renderer.GetXOffsetForPosition(map, 1, 2, 4, cw));
        Assert.Equal(Gutter + (6 * cw), renderer.GetXOffsetForPosition(map, 1, 3, 4, cw));
        Assert.Equal(Gutter + (7 * cw), renderer.GetXOffsetForPosition(map, 1, 4, 4, cw));
        Assert.Equal(Width, renderer.GetXOffsetForPosition(map, 1, 5000, 4, cw));
        Assert.NotNull(map.Get(1));
        Assert.NotNull(layout);
    }

    [Fact]
    public void NoChange_DoesNotRepaint()
    {
        (MinimapDecorationsRenderer renderer, FakeMinimapModel model, MinimapLayout layout, _) = Create();
        model.Selections.Add(new Selection(1, 1, 1, 2));
        MinimapImageData first = renderer.RenderDecorations(layout)!;
        first.Data[(Width * 4 * 100) + 3] = 77;

        MinimapImageData second = renderer.RenderDecorations(layout)!;

        Assert.Same(first, second);
        Assert.Equal(77, second.Data[(Width * 4 * 100) + 3]);

        renderer.OnSelectionChanged();
        MinimapImageData third = renderer.RenderDecorations(layout)!;

        Assert.Equal(0, third.Data[(Width * 4 * 100) + 3]);
    }
}
