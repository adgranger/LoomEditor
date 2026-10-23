using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class EditorLayoutInfoComputerTests
{
    // Same inputs as editorLayoutProvider.test.ts (glyph margin off, folding off, no minimap, no word wrap).
    private static EditorLayoutInfo Compute(
        int outerWidth,
        int outerHeight,
        bool showLineNumbers,
        int lineNumbersMinChars,
        int lineNumbersDigitCount,
        double charWidth,
        int verticalScrollbarWidth = 0,
        int horizontalScrollbarHeight = 0,
        int arrowSize = 0,
        bool verticalHasArrows = false)
    {
        var options = new EditorLayoutOptions
        {
            GlyphMargin = false,
            LineNumbers = showLineNumbers,
            LineNumbersMinChars = lineNumbersMinChars,
            LineDecorationsWidth = 10,
            Folding = false,
            Minimap = new() { Enabled = false },
            VerticalScrollbarSize = verticalScrollbarWidth,
            HorizontalScrollbarSize = horizontalScrollbarHeight,
            VerticalHasArrows = verticalHasArrows,
            ArrowSize = arrowSize,
        };
        var env = new EditorLayoutInfoComputerEnv(
            outerWidth, outerHeight, 16, lineNumbersDigitCount, charWidth, charWidth, 1, 1);
        return EditorLayoutInfoComputer.ComputeLayout(options, env);
    }

    private static void AssertLayout(
        EditorLayoutInfo actual,
        int width,
        int height,
        int lineNumbersWidth,
        int contentLeft,
        int contentWidth,
        int viewportColumn,
        OverviewRulerPosition overviewRuler,
        int verticalScrollbarWidth = 0,
        int horizontalScrollbarHeight = 0)
    {
        var expected = new EditorLayoutInfo(
            Width: width,
            Height: height,
            GlyphMarginLeft: 0,
            GlyphMarginWidth: 0,
            GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0,
            LineNumbersWidth: lineNumbersWidth,
            DecorationsLeft: lineNumbersWidth,
            DecorationsWidth: 10,
            ContentLeft: contentLeft,
            ContentWidth: contentWidth,
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.None, 0, 0, false, false, 1, 1, 0, height, 0, height),
            ViewportColumn: viewportColumn,
            IsViewportWrapping: false,
            WrappingColumn: -1,
            VerticalScrollbarWidth: verticalScrollbarWidth,
            HorizontalScrollbarHeight: horizontalScrollbarHeight,
            OverviewRuler: overviewRuler);
        Assert.Equal(expected, actual);
    }

    private static OverviewRulerPosition Ruler(int height) => new(Width: 0, Height: height, Top: 0, Right: 0);

    // Monaco: 'EditorLayoutProvider 1'
    [Fact]
    public void ComputeLayout_Provider1()
    {
        AssertLayout(Compute(1000, 800, false, 0, 1, 10), 1000, 800, 0, 10, 990, 98, Ruler(800));
    }

    // Monaco: 'EditorLayoutProvider 1.1'
    [Fact]
    public void ComputeLayout_Provider1_1_ScrollbarsWithArrows()
    {
        EditorLayoutInfo actual = Compute(1000, 800, false, 0, 1, 10, 11, 12, 13, true);

        AssertLayout(
            actual, 1000, 800, 0, 10, 990, 97,
            new OverviewRulerPosition(Width: 11, Height: 800 - 2 * 13, Top: 13, Right: 0),
            verticalScrollbarWidth: 11,
            horizontalScrollbarHeight: 12);
    }

    // Monaco: 'EditorLayoutProvider 2'
    [Fact]
    public void ComputeLayout_Provider2()
    {
        AssertLayout(Compute(900, 800, false, 0, 1, 10), 900, 800, 0, 10, 890, 88, Ruler(800));
    }

    // Monaco: 'EditorLayoutProvider 3'
    [Fact]
    public void ComputeLayout_Provider3()
    {
        AssertLayout(Compute(900, 900, false, 0, 1, 10), 900, 900, 0, 10, 890, 88, Ruler(900));
    }

    // Monaco: 'EditorLayoutProvider 4'
    [Fact]
    public void ComputeLayout_Provider4()
    {
        AssertLayout(Compute(900, 900, false, 5, 1, 10), 900, 900, 0, 10, 890, 88, Ruler(900));
    }

    // Monaco: 'EditorLayoutProvider 5'
    [Fact]
    public void ComputeLayout_Provider5()
    {
        AssertLayout(Compute(900, 900, true, 5, 1, 10), 900, 900, 50, 60, 840, 83, Ruler(900));
    }

    // Monaco: 'EditorLayoutProvider 6'
    [Fact]
    public void ComputeLayout_Provider6()
    {
        AssertLayout(Compute(900, 900, true, 5, 5, 10), 900, 900, 50, 60, 840, 83, Ruler(900));
    }

    // Monaco: 'EditorLayoutProvider 7'
    [Fact]
    public void ComputeLayout_Provider7()
    {
        AssertLayout(Compute(900, 900, true, 5, 6, 10), 900, 900, 60, 70, 830, 82, Ruler(900));
    }

    // Monaco: 'EditorLayoutProvider 8'
    [Fact]
    public void ComputeLayout_Provider8()
    {
        AssertLayout(Compute(900, 900, true, 5, 6, 5), 900, 900, 30, 40, 860, 171, Ruler(900));
    }

    // Monaco: 'EditorLayoutProvider 8 - rounds floats'
    [Fact]
    public void ComputeLayout_Provider8_RoundsFloats()
    {
        AssertLayout(Compute(900, 900, true, 5, 6, 5.05), 900, 900, 30, 40, 860, 169, Ruler(900));
    }

    [Fact]
    public void ComputeLayout_LoomEditorDefaults_UsesFiveMinCharsOfMaxDigitWidth()
    {
        var env = new EditorLayoutInfoComputerEnv(800, 600, 19, 4, 8.4, 8.4, 1, 1);

        EditorLayoutInfo info = EditorLayoutInfoComputer.ComputeLayout(new EditorLayoutOptions { Folding = false }, env); // the numbers below leave out the 16 DIPs of folding

        Assert.Equal(42, info.LineNumbersWidth);
        Assert.Equal(52, info.ContentLeft);
    }

    [Fact]
    public void ComputeLayout_FoldingAndGlyphMargin_WidenTheGutter()
    {
        var options = new EditorLayoutOptions { GlyphMargin = true, Folding = true };
        var env = new EditorLayoutInfoComputerEnv(800, 600, 19, 1, 8, 8, 1, 2);

        EditorLayoutInfo info = EditorLayoutInfoComputer.ComputeLayout(options, env);

        Assert.Equal(38, info.GlyphMarginWidth);
        Assert.Equal(38, info.LineNumbersLeft);
        Assert.Equal(26, info.DecorationsWidth);
    }

    private static EditorLayoutInfo ComputeWrapped(WordWrap wordWrap, int wordWrapColumn = 80, bool indicator = false, int outerWidth = 800)
    {
        var options = new EditorLayoutOptions
        {
            Folding = false,
            Minimap = new() { Enabled = false },
            Wrapping = new EditorWrappingOptions { WordWrap = wordWrap, WordWrapColumn = wordWrapColumn, WordWrapIndicator = indicator },
        };
        var env = new EditorLayoutInfoComputerEnv(outerWidth, 600, 19, 1, 8, 8, 1, 1);
        return EditorLayoutInfoComputer.ComputeLayout(options, env);
    }

    [Fact]
    public void ComputeLayout_WordWrapOff_HasNoWrappingColumn()
    {
        EditorLayoutInfo info = ComputeWrapped(WordWrap.Off);

        Assert.False(info.IsViewportWrapping);
        Assert.Equal(-1, info.WrappingColumn);
    }

    [Fact]
    public void ComputeLayout_WordWrapColumn_UsesTheColumn()
    {
        EditorLayoutInfo info = ComputeWrapped(WordWrap.WordWrapColumn, wordWrapColumn: 40);

        Assert.False(info.IsViewportWrapping);
        Assert.Equal(40, info.WrappingColumn);
    }

    [Fact]
    public void ComputeLayout_WordWrapOn_WrapsAtTheViewportColumn()
    {
        EditorLayoutInfo info = ComputeWrapped(WordWrap.On);

        Assert.True(info.IsViewportWrapping);
        Assert.Equal(info.ViewportColumn, info.WrappingColumn);
    }

    [Fact]
    public void ComputeLayout_WordWrapOnWithIndicator_LeavesTheRightmostColumn()
    {
        EditorLayoutInfo info = ComputeWrapped(WordWrap.On, indicator: true);

        Assert.Equal(info.ViewportColumn - 1, info.WrappingColumn);
    }

    [Fact]
    public void ComputeLayout_Bounded_UsesTheMinimumOfViewportAndColumn()
    {
        EditorLayoutInfo small = ComputeWrapped(WordWrap.Bounded, wordWrapColumn: 20);
        EditorLayoutInfo large = ComputeWrapped(WordWrap.Bounded, wordWrapColumn: 1000);

        Assert.True(small.IsViewportWrapping);
        Assert.Equal(20, small.WrappingColumn);
        Assert.Equal(large.ViewportColumn, large.WrappingColumn);
    }

    [Fact]
    public void ComputeLayout_WordWrapOn_NeverBelowOne()
    {
        EditorLayoutInfo info = ComputeWrapped(WordWrap.On, indicator: true, outerWidth: 10);

        Assert.Equal(1, info.WrappingColumn);
    }

    private static EditorLayoutInfo ComputeMinimap(
        int outerWidth,
        int outerHeight,
        EditorMinimapOptions minimap,
        double pixelRatio,
        int viewLineCount,
        int lineNumbersDigitCount = 1,
        int verticalScrollbarWidth = 0,
        bool glyphMargin = false,
        bool lineNumbers = false,
        int lineHeight = 16,
        int lineNumbersMinChars = 0,
        int lineDecorationsWidth = 10,
        double charWidth = 10)
    {
        var options = new EditorLayoutOptions
        {
            GlyphMargin = glyphMargin,
            LineNumbers = lineNumbers,
            LineNumbersMinChars = lineNumbersMinChars,
            LineDecorationsWidth = lineDecorationsWidth,
            Folding = false,
            VerticalScrollbarSize = verticalScrollbarWidth,
            HorizontalScrollbarSize = 0,
            Minimap = minimap,
        };
        var env = new EditorLayoutInfoComputerEnv(
            outerWidth, outerHeight, lineHeight, lineNumbersDigitCount, charWidth, charWidth, pixelRatio, 1, ViewLineCount: viewLineCount);
        return EditorLayoutInfoComputer.ComputeLayout(options, env);
    }

    private static EditorMinimapOptions Minimap(MinimapSide side = MinimapSide.Right, MinimapSize size = MinimapSize.Proportional, int maxColumn = 150)
        => new() { Enabled = true, Side = side, Size = size, MaxColumn = maxColumn, RenderCharacters = true };

    private static void AssertMinimap(
        EditorLayoutInfo info,
        int left,
        int width,
        bool heightIsEditorHeight,
        bool isSampling,
        double scale,
        double lineHeight,
        int innerWidth,
        int innerHeight,
        int outerWidth,
        int contentLeft,
        int contentWidth,
        int viewportColumn,
        int gutterLeft = 0)
    {
        var expected = new EditorMinimapLayoutInfo(
            RenderMinimap.Text, left, width, heightIsEditorHeight, isSampling, scale, lineHeight, innerWidth, innerHeight, outerWidth, 800);
        Assert.Equal(expected, info.Minimap);
        Assert.Equal(contentLeft, info.ContentLeft);
        Assert.Equal(contentWidth, info.ContentWidth);
        Assert.Equal(viewportColumn, info.ViewportColumn);
        Assert.Equal(gutterLeft, info.GlyphMarginLeft);
        Assert.Equal(gutterLeft, info.LineNumbersLeft);
        Assert.Equal(gutterLeft, info.DecorationsLeft);
    }

    // Monaco: 'EditorLayoutProvider 9 - render minimap'
    [Fact]
    public void ComputeLayout_Provider9_RenderMinimap()
    {
        AssertMinimap(ComputeMinimap(1000, 800, Minimap(), 1, 9), 903, 97, false, false, 1, 2, 97, 800, 97, 10, 893, 89);
    }

    // Monaco: 'EditorLayoutProvider 9 - render minimap with pixelRatio = 2'
    [Fact]
    public void ComputeLayout_Provider9_RenderMinimapPixelRatio2()
    {
        AssertMinimap(ComputeMinimap(1000, 800, Minimap(), 2, 9), 903, 97, false, false, 2, 4, 194, 1600, 97, 10, 893, 89);
    }

    // Monaco: 'EditorLayoutProvider 9 - render minimap with pixelRatio = 4'
    [Fact]
    public void ComputeLayout_Provider9_RenderMinimapPixelRatio4()
    {
        AssertMinimap(ComputeMinimap(1000, 800, Minimap(), 4, 9), 945, 55, false, false, 2, 4, 220, 3200, 55, 10, 935, 93);
    }

    // Monaco: 'EditorLayoutProvider 10 - render minimap to left'
    [Fact]
    public void ComputeLayout_Provider10_RenderMinimapToLeft()
    {
        AssertMinimap(
            ComputeMinimap(1000, 800, Minimap(MinimapSide.Left), 4, 9), 0, 55, false, false, 2, 4, 220, 3200, 55, 65, 935, 93, gutterLeft: 55);
    }

    // Monaco: 'EditorLayoutProvider 11 - minimap mode cover without sampling'
    [Fact]
    public void ComputeLayout_Provider11_MinimapFillWithoutSampling()
    {
        AssertMinimap(
            ComputeMinimap(1000, 800, Minimap(size: MinimapSize.Fill), 2, 120, lineNumbersDigitCount: 3),
            903, 97, true, false, 3, 13, 291, 1560, 97, 10, 893, 89);
    }

    // Monaco: 'EditorLayoutProvider 12 - minimap mode cover with sampling'
    [Fact]
    public void ComputeLayout_Provider12_MinimapFillWithSampling()
    {
        AssertMinimap(
            ComputeMinimap(1000, 800, Minimap(size: MinimapSize.Fill), 2, 2500, lineNumbersDigitCount: 4),
            945, 55, true, true, 1, 1, 110, 1600, 55, 10, 935, 93);
    }

    // Monaco: 'EditorLayoutProvider 13 - minimap mode contain without sampling'
    [Fact]
    public void ComputeLayout_Provider13_MinimapFitWithoutSampling()
    {
        AssertMinimap(
            ComputeMinimap(1000, 800, Minimap(size: MinimapSize.Fit), 2, 120, lineNumbersDigitCount: 3),
            903, 97, false, false, 2, 4, 194, 1600, 97, 10, 893, 89);
    }

    // Monaco: 'EditorLayoutProvider 14 - minimap mode contain with sampling'
    [Fact]
    public void ComputeLayout_Provider14_MinimapFitWithSampling()
    {
        AssertMinimap(
            ComputeMinimap(1000, 800, Minimap(size: MinimapSize.Fit), 2, 2500, lineNumbersDigitCount: 4),
            945, 55, true, true, 1, 1, 110, 1600, 55, 10, 935, 93);
    }

    // Monaco: 'issue #31312: When wrapping, leave 2px for the cursor'
    [Fact]
    public void ComputeLayout_Issue31312_LeavesTwoPixelsForTheCursor()
    {
        EditorLayoutInfo info = ComputeMinimap(
            1201, 422, Minimap(maxColumn: 120), 2, 999, verticalScrollbarWidth: 14, glyphMargin: true, lineNumbers: true,
            lineHeight: 30, lineNumbersMinChars: 3, lineDecorationsWidth: 26, charWidth: 12.04296875);

        Assert.Equal(new EditorMinimapLayoutInfo(RenderMinimap.Text, 1096, 91, false, false, 2, 4, 182, 844, 91, 422), info.Minimap);
        Assert.Equal(30, info.GlyphMarginWidth);
        Assert.Equal(36, info.LineNumbersWidth);
        Assert.Equal(66, info.DecorationsLeft);
        Assert.Equal(92, info.ContentLeft);
        Assert.Equal(1018, info.ContentWidth);
        Assert.Equal(83, info.ViewportColumn);
        Assert.Equal(new OverviewRulerPosition(Width: 14, Height: 422, Top: 0, Right: 0), info.OverviewRuler);
    }

    // Monaco: 'leaves the rightmost column for the word wrap indicator when wrapping to the viewport'
    [Fact]
    public void ComputeLayout_WordWrapIndicator_LeavesTheRightmostColumn()
    {
        static (int ViewportColumn, int WrappingColumn) WrappingOf(WordWrap wordWrap, bool indicator, int wordWrapColumn = 40)
        {
            var options = new EditorLayoutOptions
            {
                Folding = false,
                LineNumbers = false,
                LineNumbersMinChars = 0,
                VerticalScrollbarSize = 0,
                HorizontalScrollbarSize = 0,
                Minimap = new() { Enabled = false },
                Wrapping = new EditorWrappingOptions { WordWrap = wordWrap, WordWrapColumn = wordWrapColumn, WordWrapIndicator = indicator },
            };
            EditorLayoutInfo info = EditorLayoutInfoComputer.ComputeLayout(options, new EditorLayoutInfoComputerEnv(1000, 800, 16, 1, 10, 10, 1, 1, 9));
            return (info.ViewportColumn, info.WrappingColumn);
        }

        Assert.Equal((98, -1), WrappingOf(WordWrap.Off, true));
        Assert.Equal((98, 98), WrappingOf(WordWrap.On, false));
        Assert.Equal((98, 97), WrappingOf(WordWrap.On, true));
        Assert.Equal((98, 40), WrappingOf(WordWrap.Bounded, false));
        Assert.Equal((98, 40), WrappingOf(WordWrap.Bounded, true));
        Assert.Equal((98, 40), WrappingOf(WordWrap.WordWrapColumn, true));
        Assert.Equal((98, 120), WrappingOf(WordWrap.WordWrapColumn, true, 120));
    }

    [Fact]
    public void ComputeLayout_MinimapEnabledChangesContentWidth()
    {
        EditorLayoutInfo off = ComputeMinimap(1000, 800, new EditorMinimapOptions { Enabled = false }, 1, 9);
        EditorLayoutInfo on = ComputeMinimap(1000, 800, Minimap(), 1, 9);

        Assert.Equal(RenderMinimap.None, off.Minimap.RenderMinimap);
        Assert.Equal(990, off.ContentWidth);
        Assert.Equal(990 - on.Minimap.MinimapWidth, on.ContentWidth);
    }
}
