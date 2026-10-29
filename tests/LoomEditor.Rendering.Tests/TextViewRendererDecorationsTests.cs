using LoomEditor.Core.Decorations;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Tokens;
using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the decorations overlay (it renders HTML): all tests are new.
public class TextViewRendererDecorationsTests
{
    private const float CharacterWidth = (float)FakeTextMeasurer.CharacterWidth;
    private const float LineHeight = FakeTextMeasurer.LineHeight;

    private const string ThemeJson = """
        {
          "colors": {
            "editor.rangeHighlightBackground": "#110000",
            "editor.rangeHighlightBorder": "#220000",
            "loom.commentThreadRangeBackground": "#443300",
            "editor.wordHighlightBackground": "#003300",
            "editor.findMatchHighlightBackground": "#000044",
            "editor.selectionBackground": "#555555",
            "editor.findMatchForeground": "#ff00ff",
            "editor.foldPlaceholderForeground": "#00ffff",
            "editor.foldBackground": "#123456",
            "editorBracketMatch.background": "#654321",
            "foreground": "#abcdef",
            "diffEditor.insertedTextBackground": "#00aa00",
            "diffEditor.removedTextBackground": "#aa0000",
            "diffEditor.insertedLineBackground": "#00bb00"
          }
        }
        """;

    private static ViewHarness CreateHarness(string text, int width = 500)
    {
        var harness = new ViewHarness(text, width: width);
        harness.Renderer.Theme = EditorTheme.Load(ThemeJson, "vs-dark", "Test");
        return harness;
    }

    private static void AddDecoration(ViewHarness harness, Range range, string className, bool isWholeLine = false, bool showIfCollapsed = false, int? zIndex = null) =>
        AddDecoration(harness, range, new ModelDecorationOptionsData
        {
            Description = "test",
            ClassName = className,
            IsWholeLine = isWholeLine,
            ShowIfCollapsed = showIfCollapsed,
            ZIndex = zIndex,
        });

    private static void AddDecoration(ViewHarness harness, Range range, ModelDecorationOptionsData data)
    {
        ModelDecorationOptions options = ModelDecorationOptions.CreateDynamic(data);
        harness.Model.DeltaDecorations(harness.ViewModel.EditorId, [], new[] { new ModelDeltaDecoration(range, options) });
    }

    private static ThemeColor Color(ViewHarness harness, string id) => harness.Renderer.Theme.GetColor(id)!.Value;

    private static List<FillRectangleCall> Fills(ViewHarness harness, RecordingRenderSurface surface, string colorId)
    {
        ThemeColor color = Color(harness, colorId);
        return [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == color)];
    }

    private static int IndexOf(RecordingRenderSurface surface, DrawCall call) => surface.Calls.IndexOf(call);

    [Fact]
    public void DecorationStyles_EveryColorId_IsAKnownThemeColor() =>
        Assert.All(DecorationStyles.ColorIds, id => Assert.True(EditorColorDefaults.IsKnown(id), id));

    [Fact]
    public void Render_RangeDecoration_FillsItsBackgroundUnderTheText()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "rangeHighlight");

        RecordingRenderSurface surface = harness.Render();

        FillRectangleCall fill = Assert.Single(Fills(harness, surface, "editor.rangeHighlightBackground"));
        Assert.Equal(new FillRectangleCall(harness.LayoutInfo.ContentLeft, 0, 5 * CharacterWidth, LineHeight, fill.Color), fill);
        int text = surface.Calls.FindIndex(call => call is DrawLineLayoutCall { Text: "hello world" });
        Assert.True(IndexOf(surface, fill) < text, "the decoration must be drawn under the text");
    }

    [Fact]
    public void Render_RangeDecoration_DrawsItsBorderInside()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "rangeHighlight");

        RecordingRenderSurface surface = harness.Render();

        ThemeColor border = Color(harness, "editor.rangeHighlightBorder");
        DrawRectangleCall call = Assert.Single(surface.Calls.OfType<DrawRectangleCall>(), c => c.Color == border);
        Assert.Equal(new DrawRectangleCall(harness.LayoutInfo.ContentLeft + 0.5f, 0.5f, 5 * CharacterWidth - 1, LineHeight - 1, border, 1), call);
        Assert.True(IndexOf(surface, Fills(harness, surface, "editor.rangeHighlightBackground")[0]) < IndexOf(surface, call));
    }

    [Fact]
    public void Render_WholeLineDecoration_FillsTheLine()
    {
        using ViewHarness harness = CreateHarness("a\nb\nc");
        AddDecoration(harness, new Range(2, 1, 2, 1), "wordHighlight", isWholeLine: true);

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editor.wordHighlightBackground"));

        int contentLeft = harness.LayoutInfo.ContentLeft;
        Assert.Equal(new FillRectangleCall(contentLeft, LineHeight, harness.LayoutInfo.Width - contentLeft, LineHeight, fill.Color), fill);
    }

    [Fact]
    public void Render_MultiLineDecoration_ContinuesToTheLineEnd()
    {
        using ViewHarness harness = CreateHarness("ab\ncdef");
        AddDecoration(harness, new Range(1, 2, 2, 3), "wordHighlight");

        List<FillRectangleCall> fills = Fills(harness, harness.Render(), "editor.wordHighlightBackground");

        int contentLeft = harness.LayoutInfo.ContentLeft;
        Assert.Equal(2, fills.Count);
        Assert.Equal((contentLeft + CharacterWidth, 0f, CharacterWidth), (fills[0].X, fills[0].Y, fills[0].Width));
        Assert.Equal((contentLeft, LineHeight, 2 * CharacterWidth), (fills[1].X, fills[1].Y, fills[1].Width));
    }

    [Fact]
    public void Render_CommentThreadText_OneLine_FillsOnlyTheText()
    {
        using ViewHarness harness = CreateHarness("abcdef");
        AddDecoration(harness, new Range(1, 2, 1, 5), "comment-thread-text");

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "loom.commentThreadRangeBackground"));

        Assert.Equal((harness.LayoutInfo.ContentLeft + CharacterWidth, 3 * CharacterWidth), (fill.X, fill.Width));
    }

    [Fact]
    public void Render_CommentThreadText_ThreeLines_PartialFirstAndLastLines()
    {
        using ViewHarness harness = CreateHarness("abcd\nefgh\nijkl");
        AddDecoration(harness, new Range(1, 3, 3, 2), "comment-thread-text");

        List<FillRectangleCall> fills = Fills(harness, harness.Render(), "loom.commentThreadRangeBackground");

        int contentLeft = harness.LayoutInfo.ContentLeft;
        Assert.Equal(3, fills.Count);
        Assert.Equal(contentLeft + 2 * CharacterWidth, fills[0].X);
        Assert.Equal(contentLeft, fills[1].X);
        Assert.Equal((contentLeft, CharacterWidth), (fills[2].X, fills[2].Width));
    }

    [Fact]
    public void Render_FindMatch_IncludesTheNewLine()
    {
        using ViewHarness harness = CreateHarness("ab\ncdef");
        AddDecoration(harness, new Range(1, 2, 2, 3), "findMatch");

        List<FillRectangleCall> fills = Fills(harness, harness.Render(), "editor.findMatchHighlightBackground");

        float halfwidth = (float)harness.Measurer.FontInfo.TypicalHalfwidthCharacterWidth;
        Assert.Equal(CharacterWidth + halfwidth, fills[0].Width);
        Assert.Equal(2 * CharacterWidth, fills[1].Width);
    }

    [Fact]
    public void Render_TouchingDecorationsOfTheSameClass_AreMerged()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 3), "wordHighlight");
        AddDecoration(harness, new Range(1, 3, 1, 5), "wordHighlight");

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editor.wordHighlightBackground"));

        Assert.Equal(4 * CharacterWidth, fill.Width);
    }

    [Fact]
    public void Render_ShowIfCollapsed_EmptyRange_IsWidened()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 3, 1, 3), "wordHighlight", showIfCollapsed: true);

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editor.wordHighlightBackground"));

        double halfwidth = harness.Measurer.FontInfo.TypicalHalfwidthCharacterWidth;
        Assert.Equal((float)halfwidth, fill.Width);
        Assert.Equal((float)(harness.LayoutInfo.ContentLeft + Math.Round(2 * CharacterWidth - halfwidth / 2)), fill.X);
    }

    [Fact]
    public void Render_UnknownClassName_DrawsNothing()
    {
        using ViewHarness harness = CreateHarness("hello world");
        int expected = harness.Render().Calls.Count;
        AddDecoration(harness, new Range(1, 1, 1, 6), "no-such-class");

        Assert.Equal(expected, harness.Render().Calls.Count);
    }

    [Fact]
    public void Render_ZIndex_DrawsHigherLast()
    {
        using ViewHarness harness = CreateHarness("hello world");
        // By class name alone, rangeHighlight would come first.
        AddDecoration(harness, new Range(1, 1, 1, 6), "rangeHighlight", zIndex: 10);
        AddDecoration(harness, new Range(1, 1, 1, 6), "wordHighlight");

        RecordingRenderSurface surface = harness.Render();

        int word = IndexOf(surface, Assert.Single(Fills(harness, surface, "editor.wordHighlightBackground")));
        int range = IndexOf(surface, Assert.Single(Fills(harness, surface, "editor.rangeHighlightBackground")));
        Assert.True(word < range, "the decoration with the higher z-index must be drawn last");
    }

    [Fact]
    public void Render_DecorationAboveTheSelection()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "wordHighlight");

        RecordingRenderSurface surface = harness.Render(new Selection(1, 1, 1, 6));

        int selection = surface.Calls.FindIndex(call => call is FillRectangleCall fill && fill.Color == harness.Renderer.Theme.SelectionBackground);
        int decoration = IndexOf(surface, Assert.Single(Fills(harness, surface, "editor.wordHighlightBackground")));
        Assert.InRange(selection, 0, decoration - 1);
    }

    [Fact]
    public void Render_HorizontalScroll_ShiftsDecorations()
    {
        using ViewHarness harness = CreateHarness(new string('x', 100));
        AddDecoration(harness, new Range(1, 1, 1, 6), "wordHighlight");
        harness.Render(); // reports the width of the line, so that there is room to scroll
        harness.ScrollTo(scrollLeft: 30);

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editor.wordHighlightBackground"));

        Assert.Equal(harness.LayoutInfo.ContentLeft - 30, fill.X);
    }

    [Fact]
    public void Render_InlineClassNameWithForeground_ColorsTheText()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "findMatchInline" });

        harness.Render();

        FakeLineLayout layout = harness.Measurer.Layouts.Last(l => l.Text == "hello world");
        uint magenta = LineStyle.ToArgb(Color(harness, "editor.findMatchForeground"));
        Assert.Equal(new StyleRun(0, 5, magenta, FontStyle.None), layout.Style.Runs[0]);
    }

    [Fact]
    public void Render_BracketHighlightingClass_UsesTheBracketColorNotTheTokenColor()
    {
        using ViewHarness harness = CreateHarness("(hello)");
        AddDecoration(harness, new Range(1, 1, 1, 2), new ModelDecorationOptionsData { Description = "test", InlineClassName = "bracket-highlighting-1" });

        harness.Render();

        FakeLineLayout layout = harness.Measurer.Layouts.Last(l => l.Text == "(hello)");
        ThemeColor expected = BracketPairColorizationColors.GetLevelColor(harness.Renderer.Theme, 1)!.Value;
        Assert.Equal(new StyleRun(0, 1, LineStyle.ToArgb(expected), FontStyle.None), layout.Style.Runs[0]);
    }

    [Fact]
    public void Render_InlineClassNameWithoutRow_KeepsTokenColors()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "no-such-class" });

        harness.Render();

        Assert.Same(LineStyle.Plain, harness.Measurer.Layouts.Last(l => l.Text == "hello world").Style);
    }

    [Fact]
    public void Render_AfterContent_DrawnAfterTheLineEnd()
    {
        using ViewHarness harness = CreateHarness("abc\ndef");
        AddDecoration(harness, new Range(1, 1, 1, 4), new ModelDecorationOptionsData { Description = "test", AfterContentClassName = "inline-folded", IsWholeLine = true });

        RecordingRenderSurface surface = harness.Render();

        DrawLineLayoutCall call = Assert.Single(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "⋯");
        float x = (float)(harness.LayoutInfo.ContentLeft + 3 * CharacterWidth + 0.2 * harness.Measurer.FontInfo.FontSize);
        Assert.Equal(new DrawLineLayoutCall("⋯", x, 0, Color(harness, "editor.foldPlaceholderForeground")), call);
        int text = surface.Calls.FindIndex(c => c is DrawLineLayoutCall { Text: "abc" });
        Assert.True(text < surface.Calls.IndexOf(call), "the after content must be drawn after the text");
    }

    [Fact]
    public void Render_EmptyDiffRange_DrawsA3DipLeftBar()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 3, 1, 6), "char-insert diff-range-empty");

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "diffEditor.insertedTextBackground"));

        Assert.Equal(new FillRectangleCall(harness.LayoutInfo.ContentLeft + 2 * CharacterWidth, 0, 3, LineHeight, fill.Color), fill);
    }

    [Fact]
    public void Render_SelectionAnchor_DrawsA2DipLeftBar()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 3, 1, 3), new ModelDecorationOptionsData { Description = "selection-anchor", ClassName = "selection-anchor" });

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "loom.selectionAnchor"));

        Assert.Equal(new FillRectangleCall(harness.LayoutInfo.ContentLeft + 2 * CharacterWidth, 0, 2, LineHeight, fill.Color), fill);
    }

    [Fact]
    public void Render_DndTarget_DrawsA2DipRightBar()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 3, 1, 3), new ModelDecorationOptionsData { Description = "dnd-target", ClassName = "dnd-target" });

        // The caret is drawn with the same color: the bar is the fill at the end of the (empty) range.
        Assert.Contains(Fills(harness, harness.Render(), "editorCursor.foreground"), fill => fill is { Width: 2, Y: 0 } && fill.X == harness.LayoutInfo.ContentLeft + 2 * CharacterWidth && fill.Height == LineHeight);
    }

    [Fact]
    public void Render_SnippetPlaceholder_DrawsItsBackground()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "snippet-placeholder");

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editor.snippetTabstopHighlightBackground"));

        Assert.Equal(new FillRectangleCall(harness.LayoutInfo.ContentLeft, 0, 5 * CharacterWidth, LineHeight, fill.Color), fill);
    }

    [Fact]
    public void Render_FinishSnippetPlaceholder_DrawsA1DipBorder()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "finish-snippet-placeholder");

        RecordingRenderSurface surface = harness.Render();

        ThemeColor border = Color(harness, "editor.snippetFinalTabstopHighlightBorder");
        DrawRectangleCall call = Assert.Single(surface.Calls.OfType<DrawRectangleCall>(), c => c.Color == border);
        Assert.Equal(new DrawRectangleCall(harness.LayoutInfo.ContentLeft + 0.5f, 0.5f, 5 * CharacterWidth - 1, LineHeight - 1, border, 1), call);
    }

    [Fact]
    public void Render_InlineDeletedText_DrawsAStrikeThroughOverItsBackground()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "inline-deleted-text");

        RecordingRenderSurface surface = harness.Render();

        FillRectangleCall background = Assert.Single(Fills(harness, surface, "diffEditor.removedTextBackground"));
        Assert.Equal(new FillRectangleCall(harness.LayoutInfo.ContentLeft, 0, 5 * CharacterWidth, LineHeight, background.Color), background);
        var strike = new FillRectangleCall(harness.LayoutInfo.ContentLeft, LineHeight / 2, 5 * CharacterWidth, 1, harness.Renderer.Theme.Foreground);
        Assert.Contains(strike, surface.Calls.OfType<FillRectangleCall>());
    }

    [Fact]
    public void Render_GutterInsert_FallsBackToTheLineBackground()
    {
        using ViewHarness harness = CreateHarness("a\nb");
        AddDecoration(harness, new Range(1, 1, 1, 1), new ModelDecorationOptionsData { Description = "test", MarginClassName = "gutter-insert" });

        Assert.Single(Fills(harness, harness.Render(), "diffEditor.insertedLineBackground"));
    }

    [Theory]
    [InlineData("insert-sign", 2)]
    [InlineData("delete-sign", 1)]
    public void Render_DiffSigns_DrawAPlusOrAMinusInTheForeground(string className, int strokes)
    {
        using ViewHarness harness = CreateHarness("a\nb");
        AddDecoration(harness, new Range(1, 1, 1, 1), new ModelDecorationOptionsData { Description = "test", LinesDecorationsClassName = className });

        ThemeColor foreground = Color(harness, "foreground");
        List<DrawLineCall> lines = [.. harness.Render().Calls.OfType<DrawLineCall>().Where(c => c.Color == foreground)];

        Assert.Equal(strokes, lines.Count);
    }

    [Fact]
    public void Render_MarginClassName_FillsTheMargin()
    {
        using ViewHarness harness = CreateHarness("a\nb\nc");
        AddDecoration(harness, new Range(2, 1, 2, 1), new ModelDecorationOptionsData { Description = "test", MarginClassName = "folded-background" });

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editor.foldBackground"));

        Assert.Equal(new FillRectangleCall(0, LineHeight, harness.LayoutInfo.ContentLeft, LineHeight, fill.Color), fill);
    }

    [Fact]
    public void Render_LinesDecorationsClassName_FillsTheDecorationsColumn()
    {
        using ViewHarness harness = CreateHarness("a\nb\nc");
        AddDecoration(harness, new Range(1, 1, 1, 1), new ModelDecorationOptionsData { Description = "test", LinesDecorationsClassName = "bracket-match" });

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editorBracketMatch.background"));

        EditorLayoutInfo layout = harness.LayoutInfo;
        Assert.True(layout.DecorationsWidth > 0);
        Assert.Equal(new FillRectangleCall(layout.DecorationsLeft, 0, layout.DecorationsWidth, LineHeight, fill.Color), fill);
    }

    [Fact]
    public void Render_FirstLineDecorationClassName_OnlyOnTheFirstLine()
    {
        using ViewHarness harness = CreateHarness("a\nb\nc");
        AddDecoration(harness, new Range(2, 1, 3, 1), new ModelDecorationOptionsData { Description = "test", FirstLineDecorationClassName = "bracket-match" });

        FillRectangleCall fill = Assert.Single(Fills(harness, harness.Render(), "editorBracketMatch.background"));

        Assert.Equal(LineHeight, fill.Y);
    }

    [Fact]
    public void Render_SameGutterClassTwiceOnALine_DrawnOnce()
    {
        using ViewHarness harness = CreateHarness("a\nb\nc");
        AddDecoration(harness, new Range(1, 1, 2, 1), new ModelDecorationOptionsData { Description = "test", MarginClassName = "folded-background" });
        AddDecoration(harness, new Range(2, 1, 2, 1), new ModelDecorationOptionsData { Description = "test", MarginClassName = "folded-background" });

        List<FillRectangleCall> fills = Fills(harness, harness.Render(), "editor.foldBackground");

        Assert.Equal(new[] { 0f, LineHeight }, fills.Select(f => f.Y));
    }

    private static List<DrawLineCall> Lines(ViewHarness harness, RecordingRenderSurface surface, string colorId)
    {
        ThemeColor color = Color(harness, colorId);
        return [.. surface.Calls.OfType<DrawLineCall>().Where(call => call.Color == color)];
    }

    [Fact]
    public void SquigglyError_DrawsZigzagLines()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 2, 1, 5), "squiggly-error");

        List<DrawLineCall> lines = Lines(harness, harness.Render(), "editorError.foreground");

        Assert.NotEmpty(lines);
        Assert.Equal(harness.LayoutInfo.ContentLeft + CharacterWidth, lines[0].X1);
        Assert.All(lines, l => Assert.InRange(l.Y1, LineHeight - 3, LineHeight));
    }

    [Fact]
    public void SquigglyHint_DrawsThreeDots()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 2, 1, 4), "squiggly-hint");

        Assert.Equal(3, Fills(harness, harness.Render(), "editorHint.foreground").Count);
    }

    [Fact]
    public void Squiggly_Warning_DrawnAboveError_WhenHigherZIndex()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 1, 1, 6), "squiggly-error");
        AddDecoration(harness, new Range(1, 1, 1, 6), "squiggly-warning", zIndex: 10);

        RecordingRenderSurface surface = harness.Render();

        Assert.True(IndexOf(surface, Lines(harness, surface, "editorError.foreground")[0]) < IndexOf(surface, Lines(harness, surface, "editorWarning.foreground")[0]));
    }

    [Fact]
    public void Squiggly_EmptyDecoration_ShowIfCollapsed_DrawsSomething()
    {
        using ViewHarness harness = CreateHarness("hello world");
        AddDecoration(harness, new Range(1, 3, 1, 3), "squiggly-error", showIfCollapsed: true);

        List<DrawLineCall> lines = Lines(harness, harness.Render(), "editorError.foreground");

        float halfwidth = (float)harness.Measurer.FontInfo.TypicalHalfwidthCharacterWidth;
        Assert.Equal(halfwidth, lines[^1].X2 - lines[0].X1, 0.01f);
    }

    [Fact]
    public void MarkerDecorations_DrawASquiggle()
    {
        using ViewHarness harness = CreateHarness("hello world");
        var service = new MarkerService();
        using var decorations = new MarkerDecorations(harness.Model, service, harness.ViewModel.EditorId);
        service.Change("t", new[] { new MarkerData(MarkerSeverity.Error, "x", 1, 1, 1, 4) });

        Assert.NotEmpty(Lines(harness, harness.Render(), "editorError.foreground"));
    }
}
