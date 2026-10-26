using LoomEditor.View.Markdown;
using static LoomEditor.View.Tests.Markdown.TestHelpers.MarkdownFormat;

namespace LoomEditor.View.Tests.Markdown;

public class MarkdownParserTests
{
    private static void Check(MarkdownDocument expected, string? markdown) =>
        Assert.Equal(Format(expected), Format(MarkdownParser.Parse(markdown)));

    [Fact]
    public void Empty_NoBlocks() => Check(Doc(), "");

    [Fact]
    public void Null_NoBlocks() => Check(Doc(), null);

    [Fact]
    public void SingleParagraph() => Check(Doc(P(T("hello"))), "hello");

    [Fact]
    public void TwoParagraphs_BlankLine() => Check(Doc(P(T("a")), P(T("b"))), "a\n\nb");

    [Fact]
    public void SoftBreak_BecomesSpace() => Check(Doc(P(T("a b"))), "a\nb");

    [Fact]
    public void HardBreak_TwoSpaces() => Check(Doc(P(T("a"), Br(), T("b"))), "a  \nb");

    [Fact]
    public void HardBreak_Backslash() => Check(Doc(P(T("a"), Br(), T("b"))), "a\\\nb");

    [Fact]
    public void Crlf_Normalized() => Check(Doc(P(T("a")), P(T("b c"))), "a\r\n\r\nb\r\nc");

    [Fact]
    public void FencedCode_WithLanguage() => Check(Doc(new MarkdownCodeBlock("csharp", "int x;")), "```csharp\nint x;\n```");

    [Fact]
    public void FencedCode_NoLanguage_NullLanguage() => Check(Doc(new MarkdownCodeBlock(null, "x")), "```\nx\n```");

    [Fact]
    public void FencedCode_Unclosed_RunsToEnd() => Check(Doc(new MarkdownCodeBlock("cs", "a\nb")), "```cs\na\nb");

    [Fact]
    public void FencedCode_KeepsIndentationAndBlankLines() =>
        Check(Doc(new MarkdownCodeBlock(null, "  a\n\n\tb")), "```\n  a\n\n\tb\n```");

    [Fact]
    public void FencedCode_TildeFence() => Check(Doc(new MarkdownCodeBlock("x", "a")), "~~~x\na\n~~~");

    [Fact]
    public void FencedCode_LongerClosingFenceCloses() =>
        Check(Doc(new MarkdownCodeBlock(null, "a"), P(T("b"))), "```\na\n`````\nb");

    [Fact]
    public void FencedCode_ShorterInnerFenceDoesNotClose() =>
        Check(Doc(new MarkdownCodeBlock(null, "a\n```\nb")), "````\na\n```\nb\n````");

    [Fact]
    public void Heading_Levels()
    {
        for (int level = 1; level <= 6; level++)
        {
            Check(Doc(new MarkdownHeading(level, [T("t")])), new string('#', level) + " t");
        }

        Check(Doc(P(T("####### t"))), "####### t");
    }

    [Fact]
    public void Heading_InlineParsed() => Check(Doc(new MarkdownHeading(2, [B("b"), T(" x")])), "## **b** x");

    [Fact]
    public void Rule_ThreeDashes() => Check(Doc(new MarkdownRule()), "---");

    [Fact]
    public void Rule_BeforeList() => Check(Doc(P(T("a")), new MarkdownRule()), "a\n---");

    [Fact]
    public void List_Unordered() => Check(Doc(new MarkdownList(false, [[T("a")], [T("b")]])), "- a\n* b");

    [Fact]
    public void List_Ordered() => Check(Doc(new MarkdownList(true, [[T("a")], [T("b")]])), "1. a\n2) b");

    [Fact]
    public void List_ContinuationLine() => Check(Doc(new MarkdownList(false, [[T("a b")]])), "- a\n  b");

    [Fact]
    public void List_EndsAtBlankLine() =>
        Check(Doc(new MarkdownList(false, [[T("a")]]), P(T("b"))), "- a\n\nb");

    [Fact]
    public void List_MixedMarkers_StayOneList() =>
        Check(Doc(new MarkdownList(true, [[T("a")], [T("b")]])), "1. a\n- b");

    [Fact]
    public void ParagraphThenCode_NoBlankLine() =>
        Check(Doc(P(T("a")), new MarkdownCodeBlock(null, "x")), "a\n```\nx\n```");

    [Fact]
    public void Blockquote_IsAParagraph() => Check(Doc(P(T("> x"))), "> x");

    [Fact]
    public void Html_IsLiteralText() => Check(Doc(P(T("<b>x</b>"))), "<b>x</b>");

    [Fact]
    public void ToPlainText_Paragraphs() =>
        Assert.Equal("a b\n\nc", MarkdownParser.Parse("a\nb\n\nc").ToPlainText());

    [Fact]
    public void ToPlainText_CodeBlockAndList() =>
        Assert.Equal("code\n\n1. a\n2. b\n\n\n\nh", MarkdownParser.Parse("```\ncode\n```\n1. a\n2. b\n---\n# h").ToPlainText());

    [Fact]
    public void ToPlainText_LineBreak() => Assert.Equal("a\nb", MarkdownParser.Parse("a\\\nb").ToPlainText());

    [Fact]
    public void ToPlainText_Empty() => Assert.Equal("", MarkdownParser.Parse("").ToPlainText());
}
