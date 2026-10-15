using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Cursors;

// Monaco has no test for CursorConfiguration: all tests are new.
public class CursorConfigurationTests
{
    private static CursorConfiguration Create(int height = 600, int lineHeight = 19, TextModelOptions? modelOptions = null, EditorCursorOptions? options = null)
    {
        var font = new FontInfo(1, "Consolas", "normal", 14, lineHeight, 0, true, 8, 16, true, 8, 8, 8, 8);
        var layout = new EditorLayoutInfo(
            Width: 652,
            Height: height,
            GlyphMarginLeft: 0,
            GlyphMarginWidth: 0,
            GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0,
            LineNumbersWidth: 42,
            DecorationsLeft: 42,
            DecorationsWidth: 10,
            ContentLeft: 52,
            ContentWidth: 600,
            ViewportColumn: 70,
            VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12,
            OverviewRuler: new OverviewRulerPosition(14, height, 0, 0));
        return new CursorConfiguration(modelOptions ?? TextModelOptions.Default, options ?? EditorCursorOptions.Default, font, layout);
    }

    [Fact]
    public void Constructor_Defaults_MatchMonaco()
    {
        CursorConfiguration config = Create();

        Assert.Equal(29, config.PageSize);
        Assert.Equal(WordHelper.UsualWordSeparators, config.WordSeparators);
        Assert.Equal(10000, config.MultiCursorLimit);
        Assert.Equal(InputMode.Insert, config.InputMode);
        Assert.Equal(4, config.TabSize);
        Assert.Equal(4, config.IndentSize);
        Assert.True(config.InsertSpaces);
        Assert.False(config.ReadOnly);
        Assert.True(config.UseTabStops);
        Assert.True(config.EmptySelectionClipboard);
        Assert.Equal(MultiCursorPasteMode.Spread, config.MultiCursorPaste);
        Assert.Equal(EditorAutoIndentStrategy.Full, config.AutoIndent);
        Assert.Equal(EditorAutoClosingStrategy.LanguageDefined, config.AutoClosingBrackets);
        Assert.Equal(EditorAutoClosingEditStrategy.Auto, config.AutoClosingDelete);
        Assert.Empty(config.WordSegmenterLocales);
    }

    [Fact]
    public void Constructor_SmallViewport_PageSizeIsAtLeastOne()
    {
        Assert.Equal(1, Create(height: 10).PageSize);
    }

    [Fact]
    public void Constructor_ModelOptions_ProvideTabAndIndentSettings()
    {
        CursorConfiguration config = Create(modelOptions: new TextModelOptions(tabSize: 2, indentSize: 3, insertSpaces: false));

        Assert.Equal(2, config.TabSize);
        Assert.Equal(3, config.IndentSize);
        Assert.False(config.InsertSpaces);
    }

    [Fact]
    public void ColumnFromVisibleColumn_ClampsToTheLine()
    {
        var config = Create();
        var model = new TextModelCursorSimpleModel(new TextModel("\tab"));

        Assert.Equal(2, config.ColumnFromVisibleColumn(model, 1, 4));
        Assert.Equal(4, config.ColumnFromVisibleColumn(model, 1, 100));
    }

    [Fact]
    public void VisibleColumnFromColumn_CountsTheTabStop()
    {
        var config = Create();
        var model = new TextModelCursorSimpleModel(new TextModel("\tab"));

        Assert.Equal(5, config.VisibleColumnFromColumn(model, new Position(1, 3)));
    }

    [Fact]
    public void NormalizeIndentation_InsertSpaces_ConvertsTabsToSpaces()
    {
        var config = Create();

        Assert.Equal("     x", config.NormalizeIndentation("\t x"));
    }
}
