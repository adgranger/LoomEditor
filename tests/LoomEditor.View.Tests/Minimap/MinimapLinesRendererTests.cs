using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

// All the tests are new: Monaco has none for the minimap line rendering.
public class MinimapLinesRendererTests
{
    private const int Height = 600;
    private const int Width = 100;
    private const int MinimapLineHeight = 2;

    private sealed class FakeMinimapModel(MinimapOptions options) : IMinimapModel
    {
        private static readonly LanguageIdCodec Codec = new();

        public List<bool[]> NeededRequests { get; } = [];

        public MinimapTokensColorTracker TokensColorTracker { get; } = new(EditorTheme.Dark);

        public MinimapOptions Options { get; } = options;

        public int GetLineCount() => 1000;

        public int GetRealLineCount() => 1000;

        public string GetLineContent(int lineNumber) => Content(lineNumber);

        public int GetLineMaxColumn(int lineNumber) => Content(lineNumber).Length + 1;

        public ViewLineData?[] GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed)
        {
            NeededRequests.Add((bool[])needed.Clone());
            var result = new ViewLineData?[needed.Length];
            for (int i = 0; i < needed.Length; i++)
            {
                if (needed[i])
                {
                    string content = Content(startLineNumber + i);
                    result[i] = new ViewLineData(content, false, 1, content.Length + 1, 0, LineTokens.CreateEmpty(content, Codec));
                }
            }

            return result;
        }

        public IReadOnlyList<Selection> GetSelections() => [];

        public IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInViewport(int startLineNumber, int endLineNumber) => [];

        public TextModelOptions GetOptions() => TextModelOptions.Default;

        public void RevealLineNumber(int lineNumber)
        {
        }

        public void SetScrollTop(double scrollTop)
        {
        }

        private static string Content(int lineNumber) => lineNumber == 1 ? "abc" : "line " + lineNumber;
    }

    private static MinimapOptions CreateOptions(RenderMinimap renderMinimap = RenderMinimap.Text)
    {
        var layout = new EditorLayoutInfo(
            Width: 800, Height: Height, GlyphMarginLeft: 0, GlyphMarginWidth: 0, GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0, LineNumbersWidth: 42, DecorationsLeft: 42, DecorationsWidth: 10, ContentLeft: 52, ContentWidth: 634,
            Minimap: new EditorMinimapLayoutInfo(renderMinimap, 686, Width, false, false, 1, MinimapLineHeight, Width, Height, Width, Height),
            ViewportColumn: 90, IsViewportWrapping: false, WrappingColumn: -1, VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12, OverviewRuler: new OverviewRulerPosition(14, Height, 0, 0));
        EditorTheme theme = EditorTheme.Dark;
        return new MinimapOptions(
            layout, new EditorMinimapOptions(), FontInfo.Default, false, theme, new MinimapTokensColorTracker(theme),
            (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
    }

    private static MinimapLayout CreateLayout(MinimapOptions options, int lineCount, double scrollTop)
    {
        double scrollHeight = lineCount * (double)options.LineHeight;
        int viewportStart = (int)(scrollTop / options.LineHeight) + 1;
        int viewportEnd = Math.Min(lineCount, viewportStart + (Height / options.LineHeight));
        return MinimapLayout.Create(
            options, viewportStart, viewportEnd, (viewportStart - 1) * (double)options.LineHeight, Height, false,
            lineCount, lineCount, scrollTop, scrollHeight, null);
    }

    private static bool IsBackground(MinimapImageData image, int x, int y, Rgba8 background)
    {
        int offset = ((y * image.Width) + x) * 4;
        return image.Data[offset] == background.R && image.Data[offset + 1] == background.G
            && image.Data[offset + 2] == background.B && image.Data[offset + 3] == background.A;
    }

    private static int CountNonBackground(MinimapImageData image, int fromRow, int toRow, Rgba8 background)
    {
        int count = 0;
        for (int y = fromRow; y < toRow; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (!IsBackground(image, x, y, background))
                {
                    count++;
                }
            }
        }

        return count;
    }

    [Fact]
    public void RenderLines_OneLine_PaintsItsCharacters()
    {
        MinimapOptions options = CreateOptions();
        var renderer = new MinimapLinesRenderer(new FakeMinimapModel(options));

        MinimapImageData? image = renderer.RenderLines(CreateLayout(options, 1, 0));

        Assert.NotNull(image);
        Assert.True(CountNonBackground(image, 0, MinimapLineHeight, options.BackgroundColor) > 0);
        Assert.Equal(0, CountNonBackground(image, MinimapLineHeight, Height, options.BackgroundColor));
    }

    [Fact]
    public void RenderLines_SameLayoutTwice_ReusesTheBuffer()
    {
        MinimapOptions options = CreateOptions();
        var model = new FakeMinimapModel(options);
        var renderer = new MinimapLinesRenderer(model);
        MinimapLayout layout = CreateLayout(options, 1, 0);

        MinimapImageData? first = renderer.RenderLines(layout);
        MinimapImageData? second = renderer.RenderLines(layout);

        Assert.Same(first, second);
        Assert.Single(model.NeededRequests);
    }

    [Fact]
    public void RenderLines_AfterFlush_RepaintsEverything()
    {
        MinimapOptions options = CreateOptions();
        var model = new FakeMinimapModel(options);
        var renderer = new MinimapLinesRenderer(model);
        MinimapLayout layout = CreateLayout(options, 3, 0);
        MinimapImageData first = renderer.RenderLines(layout)!;

        renderer.OnFlushed();
        MinimapImageData second = renderer.RenderLines(layout)!;

        Assert.NotSame(first, second);
        Assert.Equal(2, model.NeededRequests.Count);
        Assert.All(model.NeededRequests[1], needed => Assert.True(needed));
        Assert.Equal(first.Data, second.Data);
    }

    [Fact]
    public void RenderLines_ScrolledByOneLine_CopiesUntouchedLines()
    {
        MinimapOptions options = CreateOptions();
        var model = new FakeMinimapModel(options);
        var renderer = new MinimapLinesRenderer(model);
        MinimapLayout layout1 = CreateLayout(options, 1000, 500 * (double)options.LineHeight);
        MinimapLayout layout2 = CreateLayout(options, 1000, 501 * (double)options.LineHeight);
        Assert.Equal(layout1.StartLineNumber + 1, layout2.StartLineNumber);
        Assert.Equal(layout1.EndLineNumber + 1, layout2.EndLineNumber);
        MinimapImageData first = renderer.RenderLines(layout1)!;
        byte[] firstData = (byte[])first.Data.Clone();

        MinimapImageData second = renderer.RenderLines(layout2)!;

        bool[] needed = model.NeededRequests[1];
        Assert.True(needed[^1]);
        Assert.All(needed.Take(needed.Length - 1), line => Assert.False(line));
        int rowBytes = first.Width * 4;
        int copiedBytes = (needed.Length - 1) * MinimapLineHeight * rowBytes;
        Assert.Equal(
            firstData.AsSpan(MinimapLineHeight * rowBytes, copiedBytes).ToArray(),
            second.Data.AsSpan(0, copiedBytes).ToArray());
    }

    [Fact]
    public void RenderLines_Blocks_UsesBlockRendering()
    {
        MinimapOptions options = CreateOptions(RenderMinimap.Blocks);
        var renderer = new MinimapLinesRenderer(new FakeMinimapModel(options));

        MinimapImageData image = renderer.RenderLines(CreateLayout(options, 1, 0))!;

        // "abc" = 3 blocks of 1 px wide, from the gutter (8 px); a block is a solid rectangle: every painted pixel of a row is contiguous.
        Assert.False(IsBackground(image, 8, 0, options.BackgroundColor));
        Assert.False(IsBackground(image, 9, 0, options.BackgroundColor));
        Assert.False(IsBackground(image, 10, 0, options.BackgroundColor));
        Assert.True(IsBackground(image, 11, 0, options.BackgroundColor));
    }
}
