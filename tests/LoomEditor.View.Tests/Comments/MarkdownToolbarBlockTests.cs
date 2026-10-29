using LoomEditor.View.Comments;
using LoomEditor.View.Markdown;

namespace LoomEditor.View.Tests.Comments;

public class MarkdownToolbarBlockTests
{
    // Input and output mark the selection with '|' (two bars = range, a single bar = caret).
    private static string Run(MarkdownToolbarAction action, string marked, string? seed = null)
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

        ToolbarEdit edit = MarkdownToolbar.Apply(action, text, sel, seed);
        string o = edit.Text;
        return edit.Selection.IsEmpty
            ? o.Insert(edit.Selection.Start, "|")
            : o.Insert(edit.Selection.End, "|").Insert(edit.Selection.Start, "|");
    }

    private static string Heading(string m) => Run(MarkdownToolbarAction.Heading, m);

    private static string Bullet(string m) => Run(MarkdownToolbarAction.BulletList, m);

    private static string Numbered(string m) => Run(MarkdownToolbarAction.NumberedList, m);

    private static string Quote(string m) => Run(MarkdownToolbarAction.Quote, m);

    [Fact]
    public void Heading_AddsPrefix() => Assert.Equal("|### a b|", Heading("|a b|"));

    [Fact]
    public void Heading_SamePrefix_Removes() => Assert.Equal("|a|", Heading("|### a|"));

    [Fact]
    public void Heading_OtherLevel_Replaces() => Assert.Equal("|### a|", Heading("|# a|"));

    [Fact]
    public void Bullet_MultiLine_PrefixesNonBlankLines() => Assert.Equal("|- a\n\n- b|", Bullet("|a\n\nb|"));

    [Fact]
    public void Bullet_AllHavePrefix_Removes() => Assert.Equal("|a\n\nb|", Bullet("|- a\n\n- b|"));

    [Fact]
    public void Bullet_SomeHavePrefix_AddsToTheOthers() => Assert.Equal("|- a\n- b|", Bullet("|- a\nb|"));

    [Fact]
    public void Bullet_SelectionEndsAtLineStart_ExcludesLastLine() => Assert.Equal("|- a|\nb", Bullet("|a\n|b"));

    [Fact]
    public void Numbered_CountsNonBlankLines() => Assert.Equal("|1. a\n\n2. b|", Numbered("|a\n\nb|"));

    [Fact]
    public void Numbered_ReplacesBulletPrefix() => Assert.Equal("|1. a\n2. b\n3. c|", Numbered("|- a\n5. b\nc|"));

    [Fact]
    public void Numbered_AllNumbered_Removes() => Assert.Equal("|a\nb|", Numbered("|1. a\n2. b|"));

    [Fact]
    public void Quote_AddsAndRemoves()
    {
        Assert.Equal("|> a\n> b|", Quote("|a\nb|"));
        Assert.Equal("|a\nb|", Quote("|> a\n> b|"));
    }

    [Fact]
    public void LinePrefix_EmptySelection_CaretKeepsRelativePosition()
    {
        Assert.Equal("x\n- a|b", Bullet("x\na|b"));
        Assert.Equal("x\na|b", Bullet("x\n- a|b"));
        Assert.Equal("|a", Bullet("- |a"));
    }

    [Fact]
    public void CodeBlock_Selection_WrapsInFences() =>
        Assert.Equal("```\n|a\nb|\n```", Run(MarkdownToolbarAction.CodeBlock, "|a\nb|"));

    [Fact]
    public void CodeBlock_Empty_CaretOnMiddleLine() =>
        Assert.Equal("```\n|\n```", Run(MarkdownToolbarAction.CodeBlock, "|"));

    [Fact]
    public void CodeBlock_NotAtLineStart_AddsNewlineBefore() =>
        Assert.Equal("x\n```\n|y|\n```\nz", Run(MarkdownToolbarAction.CodeBlock, "x|y|z"));

    [Fact]
    public void CodeBlock_Fenced_Removes() =>
        Assert.Equal("|a|", Run(MarkdownToolbarAction.CodeBlock, "|```\na\n```|"));

    [Fact]
    public void Suggestion_InsertsBlockWithSeedSelected() =>
        Assert.Equal("```suggestion\n|int x;|\n```", Run(MarkdownToolbarAction.Suggestion, "|", "int x;"));

    [Fact]
    public void Suggestion_NoSeed_EmptyBlock() =>
        Assert.Equal("```suggestion\n|\n```", Run(MarkdownToolbarAction.Suggestion, "|"));

    // Phase 12: a range seed has several lines.
    [Fact]
    public void Suggestion_MultiLineSeed_InsertsAllLinesSelected() =>
        Assert.Equal("```suggestion\n|a\nb|\n```", Run(MarkdownToolbarAction.Suggestion, "|", "a\nb"));

    [Fact]
    public void Suggestion_SecondSeedLineHasFence_UsesLongerFence() =>
        Assert.Equal("````suggestion\n|a\n```x|\n````", Run(MarkdownToolbarAction.Suggestion, "|", "a\n```x"));

    [Fact]
    public void Suggestion_SeedWithFiveBackticks_UsesSix() =>
        Assert.Equal("``````suggestion\n|`````x|\n``````", Run(MarkdownToolbarAction.Suggestion, "|", "`````x"));

    [Fact]
    public void Suggestion_SeedWithFence_UsesLongerFence() =>
        Assert.Equal("````suggestion\n|```x|\n````", Run(MarkdownToolbarAction.Suggestion, "|", "```x"));

    [Fact]
    public void Suggestion_MidLine_StartsOnNewLine() =>
        Assert.Equal("ab\n```suggestion\n|s|\n```\ncd", Run(MarkdownToolbarAction.Suggestion, "ab|cd", "s"));

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(11)]
    public void RoundTrip_BlockAction_ParsesToItsBlock(int actionId)
    {
        var action = (MarkdownToolbarAction)actionId;
        string text = MarkdownToolbar.Apply(action, "word", new ToolbarSelection(0, 4), "word").Text;
        MarkdownBlock block = Assert.Single(MarkdownParser.Parse(text, MarkdownParserOptions.Extended).Blocks);
        switch (action)
        {
            case MarkdownToolbarAction.BulletList:
                Assert.False(Assert.IsType<MarkdownList>(block).Ordered);
                break;
            case MarkdownToolbarAction.NumberedList:
                Assert.True(Assert.IsType<MarkdownList>(block).Ordered);
                break;
            case MarkdownToolbarAction.Quote:
                Assert.IsType<MarkdownQuote>(block);
                break;
            case MarkdownToolbarAction.Heading:
                Assert.Equal(3, Assert.IsType<MarkdownHeading>(block).Level);
                break;
            case MarkdownToolbarAction.CodeBlock:
                Assert.Equal("word", Assert.IsType<MarkdownCodeBlock>(block).Code);
                break;
            default:
                var code = Assert.IsType<MarkdownCodeBlock>(block);
                Assert.Equal("suggestion", code.Language);
                Assert.Equal("word", code.Code);
                break;
        }
    }
}
