using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Monaco has no test for CursorConfiguration: all tests are new.
public class CursorConfigurationTests
{
    private static CursorConfiguration Create(
        int height = 600,
        int lineHeight = 19,
        TextModelOptions? modelOptions = null,
        EditorCursorOptions? options = null,
        string languageId = "plaintext",
        LanguageConfigurationRegistry? registry = null)
    {
        var font = new FontInfo(1, "Consolas", "normal", 14, lineHeight, 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, 8, 16, true, 8, 8, 8, 8);
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
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.None, 0, 0, false, false, 1, 1, 0, 0, 0, 0),
            ViewportColumn: 70,
            IsViewportWrapping: false,
            WrappingColumn: -1,
            VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12,
            OverviewRuler: new OverviewRulerPosition(14, height, 0, 0));
        return new CursorConfiguration(languageId, modelOptions ?? TextModelOptions.Default, options ?? EditorCursorOptions.Default, font, layout, registry ?? new LanguageConfigurationRegistry());
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

    private static CursorConfiguration CreateForLanguage(LanguageConfiguration configuration, EditorCursorOptions? options = null)
    {
        var registry = new LanguageConfigurationRegistry();
        registry.Register("test", configuration);
        return Create(options: options, languageId: "test", registry: registry);
    }

    [Fact]
    public void Constructor_LanguageWithAutoClosingPairs_ExposesThem()
    {
        CursorConfiguration config = CreateForLanguage(new LanguageConfiguration { AutoClosingPairs = [new AutoClosingPairConditional("(", ")")] });

        Assert.True(config.AutoClosingPairs.AutoClosingPairsOpenByEnd.ContainsKey("("));
    }

    [Fact]
    public void Constructor_NoSurroundingPairs_UsesAutoClosingPairs()
    {
        CursorConfiguration config = CreateForLanguage(new LanguageConfiguration { AutoClosingPairs = [new AutoClosingPairConditional("(", ")")] });

        Assert.Equal(")", config.SurroundingPairs["("]);
    }

    [Fact]
    public void Constructor_SurroundingPairs_AreOpenToClose()
    {
        CursorConfiguration config = CreateForLanguage(new LanguageConfiguration { SurroundingPairs = [new AutoClosingPair("<", ">"), new AutoClosingPair("<", "/>")] });

        Assert.Equal("/>", config.SurroundingPairs["<"]);
    }

    [Fact]
    public void ShouldAutoCloseBefore_LanguageDefined_UsesAutoCloseBeforeSet()
    {
        CursorConfiguration config = CreateForLanguage(
            new LanguageConfiguration { AutoCloseBefore = ";" },
            new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.LanguageDefined });

        Assert.True(config.ShouldAutoCloseBefore.Bracket(';'));
        Assert.False(config.ShouldAutoCloseBefore.Bracket('a'));
    }

    [Fact]
    public void ShouldAutoCloseBefore_BeforeWhitespace_OnlySpaceAndTab()
    {
        CursorConfiguration config = CreateForLanguage(
            new LanguageConfiguration(),
            new EditorCursorOptions { AutoClosingQuotes = EditorAutoClosingStrategy.BeforeWhitespace });

        Assert.True(config.ShouldAutoCloseBefore.Quote(' '));
        Assert.True(config.ShouldAutoCloseBefore.Quote((char)9));
        Assert.False(config.ShouldAutoCloseBefore.Quote(')'));
    }

    [Fact]
    public void ShouldAutoCloseBefore_Always_True()
    {
        CursorConfiguration config = CreateForLanguage(
            new LanguageConfiguration(),
            new EditorCursorOptions { AutoClosingComments = EditorAutoClosingStrategy.Always });

        Assert.True(config.ShouldAutoCloseBefore.Comment('x'));
    }

    [Fact]
    public void ShouldAutoCloseBefore_Never_False()
    {
        CursorConfiguration config = CreateForLanguage(
            new LanguageConfiguration(),
            new EditorCursorOptions { AutoClosingBrackets = EditorAutoClosingStrategy.Never });

        Assert.False(config.ShouldAutoCloseBefore.Bracket(' '));
    }

    [Fact]
    public void ElectricChars_LanguageWithBrackets_ContainsLastCharacterOfEachClose()
    {
        CursorConfiguration config = CreateForLanguage(new LanguageConfiguration { Brackets = [new("{", "}"), new("(", ")")] });

        Assert.Contains("}", config.ElectricChars);
        Assert.Contains(")", config.ElectricChars);
        Assert.DoesNotContain("{", config.ElectricChars);
    }

    [Fact]
    public void BlockCommentStartToken_LanguageWithBlockComment_IsOpenToken()
    {
        CursorConfiguration config = CreateForLanguage(new LanguageConfiguration { Comments = new CommentRule(BlockComment: new CharacterPair("/*", "*/")) });

        Assert.Equal("/*", config.BlockCommentStartToken);
    }

    [Fact]
    public void ViewModel_LanguageChanged_RecreatesCursorConfig()
    {
        var registry = new LanguageConfigurationRegistry();
        registry.Register("withBlockComment", new LanguageConfiguration { Comments = new CommentRule(BlockComment: new CharacterPair("/*", "*/")) });
        var codec = new LanguageIdCodec();
        codec.Register("withBlockComment");
        var model = new TextModel("text", DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null, registry);
        TestCodeEditor.WithTestCodeEditor(model, editor =>
        {
            Assert.Null(editor.ViewModel.CursorConfig.BlockCommentStartToken);

            model.Tokenization.SetLanguageId("withBlockComment");

            Assert.Equal("/*", editor.ViewModel.CursorConfig.BlockCommentStartToken);
        });
    }

    [Fact]
    public void ViewModel_LanguageConfigurationRegistered_RecreatesCursorConfig()
    {
        var registry = new LanguageConfigurationRegistry();
        var codec = new LanguageIdCodec();
        codec.Register("late");
        var model = new TextModel("text", DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null, registry);
        model.Tokenization.SetLanguageId("late");
        TestCodeEditor.WithTestCodeEditor(model, editor =>
        {
            Assert.Null(editor.ViewModel.CursorConfig.BlockCommentStartToken);

            registry.Register("late", new LanguageConfiguration { Comments = new CommentRule(BlockComment: new CharacterPair("/*", "*/")) });

            Assert.Equal("/*", editor.ViewModel.CursorConfig.BlockCommentStartToken);
        });
    }
}
