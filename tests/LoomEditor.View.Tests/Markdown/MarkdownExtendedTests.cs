using LoomEditor.View.Markdown;
using static LoomEditor.View.Tests.Markdown.TestHelpers.MarkdownFormat;

namespace LoomEditor.View.Tests.Markdown;

public class MarkdownExtendedTests
{
    private static string Ext(string md) => Format(MarkdownParser.Parse(md, MarkdownParserOptions.Extended));

    private static string Def(string md) => Format(MarkdownParser.Parse(md));

    [Fact]
    public void Quote_SingleLine_IsQuoteWithParagraph() => Assert.Equal("Q[P[{:x}]]", Ext("> x"));

    [Fact]
    public void Quote_MultiLine_JoinsLines() => Assert.Equal("Q[P[{:a b}]]", Ext("> a\n> b"));

    [Fact]
    public void Quote_WithList_ParsesListInside() => Assert.Equal("Q[UL[{:a} | {:b}]]", Ext("> - a\n> - b"));

    [Fact]
    public void Quote_Nested_UpToDepthThree() =>
        Assert.Equal("Q[Q[Q[P[{:> x}]]]]", Ext("> > > > x"));

    [Fact]
    public void Quote_InsideFence_IsCode() => Assert.Equal("CODE(null)<> x>", Ext("```\n> x\n```"));

    [Fact]
    public void Quote_EndsAtBlankLine() => Assert.Equal("Q[P[{:a}]]\nQ[P[{:b}]]", Ext("> a\n\n> b"));

    [Fact]
    public void Quote_NotExtended_IsParagraph() => Assert.Equal("P[{:> x}]", Def("> x"));

    [Fact]
    public void Strikethrough_Simple() => Assert.Equal("P[{:a } {s:b} {: c}]", Ext("a ~~b~~ c"));

    [Fact]
    public void Strikethrough_InsideBold_InheritsFlags() => Assert.Equal("P[{bs:x}]", Ext("**~~x~~**"));

    [Fact]
    public void Strikethrough_Unclosed_IsLiteral() => Assert.Equal("P[{:~~x}]", Ext("~~x"));

    [Fact]
    public void Strikethrough_NotExtended_IsLiteral() => Assert.Equal("P[{:~~x~~}]", Def("~~x~~"));

    [Fact]
    public void Underline_U_And_Ins() => Assert.Equal("P[{u:a} {: } {u:b}]", Ext("<u>a</u> <ins>b</ins>"));

    [Fact]
    public void Underline_CaseInsensitive() => Assert.Equal("P[{u:a}]", Ext("<U>a</U>"));

    [Fact]
    public void Underline_Unmatched_IsLiteral() => Assert.Equal("P[{:<u>a}]", Ext("<u>a"));

    [Fact]
    public void Underline_OtherHtml_IsLiteral() => Assert.Equal("P[{:<b>a</b>}]", Ext("<b>a</b>"));

    [Fact]
    public void Extended_SuggestionFence_ParsedAsCodeBlockWithLanguage()
    {
        MarkdownBlock block = Assert.Single(MarkdownParser.Parse("```suggestion\nx\n```", MarkdownParserOptions.Extended).Blocks);
        Assert.Equal("suggestion", Assert.IsType<MarkdownCodeBlock>(block).Language);
    }

    [Theory]
    [InlineData("> x")]
    [InlineData("~~x~~ and <u>y</u>")]
    [InlineData("# h\n\n- a\n- b\n\n```cs\nz\n```")]
    public void Default_SameOutputAsBefore(string md) =>
        Assert.Equal(Format(MarkdownParser.Parse(md)), Format(MarkdownParser.Parse(md, MarkdownParserOptions.Default)));

    [Fact]
    public void ToPlainText_IncludesQuotedText() =>
        Assert.Equal("a\n\nb", MarkdownParser.Parse("> a\n\nb", MarkdownParserOptions.Extended).ToPlainText());
}
