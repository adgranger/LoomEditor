using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;

namespace LoomEditor.View.Tests.Comments;

public class MarkdownToolbarInlineTests
{
    // Input and output mark the selection with '|' (two bars = range, a single bar = caret).
    private static string Run(MarkdownToolbarAction action, string marked)
    {
        int first = marked.IndexOf('|');
        int second = marked.IndexOf('|', first + 1);
        string text;
        ToolbarSelection sel;
        if (second < 0)
        {
            text = marked.Remove(first, 1);
            sel = new ToolbarSelection(first, first);
        }
        else
        {
            text = marked.Remove(second, 1).Remove(first, 1);
            sel = new ToolbarSelection(first, second - 1);
        }

        ToolbarEdit edit = MarkdownToolbar.Apply(action, text, sel);
        string o = edit.Text;
        if (edit.Selection.IsEmpty)
        {
            return o.Insert(edit.Selection.Start, "|");
        }

        return o.Insert(edit.Selection.End, "|").Insert(edit.Selection.Start, "|");
    }

    private static string Bold(string marked) => Run(MarkdownToolbarAction.Bold, marked);

    [Fact]
    public void Bold_Selection_Wraps() => Assert.Equal("x |**a**| y", Bold("x |a| y"));

    [Fact]
    public void Bold_Selection_WrappedOutside_Unwraps() => Assert.Equal("|a|", Bold("**|a|**"));

    [Fact]
    public void Bold_Selection_IncludesMarkers_Unwraps() => Assert.Equal("|a|", Bold("|**a**|"));

    [Fact]
    public void Bold_EmptySelection_InsertsPairWithCaretBetween() => Assert.Equal("x**|**y", Bold("x|y"));

    [Fact]
    public void Bold_EmptyPair_Removes() => Assert.Equal("x|y", Bold("x**|**y"));

    [Fact]
    public void Bold_EdgeSpaces_StayOutside() => Assert.Equal(" |**a**| ", Bold("| a |"));

    [Fact]
    public void Bold_OnlyBlanks_ActsAsEmpty() => Assert.Equal("x   **|**y", Bold("x |  |y"));

    [Fact]
    public void Bold_MultiLine_WrapsEachNonBlankLine() =>
        Assert.Equal("|**a**\n\n **b**| ", Bold("|a\n\n b |"));

    [Fact]
    public void Bold_MultiLine_AllWrapped_UnwrapsAll() => Assert.Equal("|a\n\nb|", Bold("|**a**\n\n**b**|"));

    [Fact]
    public void Italic_UsesUnderscore() => Assert.Equal("|_a_|", Run(MarkdownToolbarAction.Italic, "|a|"));

    [Fact]
    public void Underline_UsesUTags() => Assert.Equal("|<u>a</u>|", Run(MarkdownToolbarAction.Underline, "|a|"));

    [Fact]
    public void Strikethrough_UsesTildes() => Assert.Equal("|~~a~~|", Run(MarkdownToolbarAction.Strikethrough, "|a|"));

    [Fact]
    public void InlineCode_UsesBacktick() => Assert.Equal("|`a`|", Run(MarkdownToolbarAction.InlineCode, "|a|"));

    [Fact]
    public void Link_Selection_SelectsUrlPlaceholder() =>
        Assert.Equal("[abc](|url|)", Run(MarkdownToolbarAction.Link, "|abc|"));

    [Fact]
    public void Link_UrlSelection_PutsItInParens() =>
        Assert.Equal("[|](https://x.io)", Run(MarkdownToolbarAction.Link, "|https://x.io|"));

    [Fact]
    public void Link_Empty_CaretInBrackets() => Assert.Equal("a[|](url)b", Run(MarkdownToolbarAction.Link, "a|b"));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(2, 1)]
    [InlineData(0, 5)]
    public void Apply_InvalidSelection_Throws(int start, int end) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MarkdownToolbar.Apply(MarkdownToolbarAction.Bold, "abc", new ToolbarSelection(start, end)));

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void Apply_NeverProducesCarriageReturn(int actionId) =>
        Assert.DoesNotContain('\r', MarkdownToolbar.Apply((MarkdownToolbarAction)actionId, "a\n b \n\nc", new ToolbarSelection(0, 8)).Text);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RoundTrip_EveryInlineAction_ParsesToItsFlag(int actionId)
    {
        var action = (MarkdownToolbarAction)actionId;
        string text = MarkdownToolbar.Apply(action, "word", new ToolbarSelection(0, 4)).Text;
        MarkdownBlock block = Assert.Single(MarkdownParser.Parse(text, MarkdownParserOptions.Extended).Blocks);
        MarkdownInline inline = Assert.Single(Assert.IsType<MarkdownParagraph>(block).Inlines);
        Assert.Equal("word", inline.Text);
        Assert.True(action switch
        {
            MarkdownToolbarAction.Bold => inline.Bold,
            MarkdownToolbarAction.Italic => inline.Italic,
            MarkdownToolbarAction.Underline => inline.Underline,
            MarkdownToolbarAction.Strikethrough => inline.Strikethrough,
            _ => inline.Code,
        });
    }
}
