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
}
