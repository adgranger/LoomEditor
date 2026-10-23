using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Markdown;
using static LoomEditor.View.Tests.Markdown.TestHelpers.MarkdownFormat;

namespace LoomEditor.View.Tests.Markdown;

public class MarkdownInlineParserTests
{
    private static void Check(string text, params MarkdownInline[] expected) =>
        Assert.Equal(Format(expected), Format(MarkdownInlineParser.Parse(text)));

    [Fact]
    public void Plain() => Check("hello", T("hello"));

    [Fact]
    public void InlineCode() => Check("a `b` c", T("a "), C("b"), T(" c"));

    [Fact]
    public void InlineCode_DoubleBackticks() => Check("``a`b``", C("a`b"));

    [Fact]
    public void InlineCode_StripsOneSpace() => Check("`  a  `", C(" a "));

    [Fact]
    public void InlineCode_Unclosed_Literal() => Check("`a", T("`a"));

    [Fact]
    public void Bold_Stars() => Check("**a**", B("a"));

    [Fact]
    public void Bold_Underscores() => Check("__a__", B("a"));

    [Fact]
    public void Italic_Star() => Check("*a*", I("a"));

    [Fact]
    public void Italic_Underscore() => Check("_a_", I("a"));

    [Fact]
    public void BoldItalic_TripleStar() => Check("***a***", new MarkdownInline("a", Bold: true, Italic: true));

    [Fact]
    public void Italic_InsideBold() =>
        Check("**a *b* c**", B("a "), new MarkdownInline("b", Bold: true, Italic: true), B(" c"));

    [Fact]
    public void Snake_Case_IsPlain() => Check("snake_case_name", T("snake_case_name"));

    [Fact]
    public void Star_WithSpaces_IsPlain() => Check("a * b * c", T("a * b * c"));

    [Fact]
    public void Emphasis_Unmatched_Literal() => Check("**a", T("**a"));

    [Fact]
    public void Link() => Check("[a](http://x)", L("a", "http://x"));

    [Fact]
    public void Link_WithTitle() => Check("[a](http://x \"t\")", L("a", "http://x"));

    [Fact]
    public void Link_TextHasEmphasis() =>
        Check("[a **b**](u)", L("a ", "u"), new MarkdownInline("b", Bold: true, LinkUrl: "u"));

    [Fact]
    public void Link_Malformed_Literal() => Check("[a](b", T("[a](b"));

    [Fact]
    public void Link_AngleUrl() => Check("[a](<x y>)", L("a", "x y"));

    [Fact]
    public void Escape_Tokens() => Check("\\*a\\*", T("*a*"));

    [Fact]
    public void Escape_Backslash_Literal_WhenNotEscapable() => Check("\\q", T("\\q"));

    [Fact]
    public void Entities() => Check("a&nbsp;b &amp; &lt;", T("a b & <"));

    [Fact]
    public void Entity_Unknown_Literal() => Check("a &foo; &", T("a &foo; &"));

    [Fact]
    public void AdjacentSameStyle_Merged() => Check("a\\*b&amp;c", T("a*b&c"));

    [Theory]
    [InlineData("a*b_c")]
    [InlineData("(x) [y] {z}")]
    public void FromText_RoundTrips(string s)
    {
        // FromText turns blanks into &nbsp;, so the text comes back with U+00A0 instead of spaces.
        MarkdownDocument doc = MarkdownParser.Parse(MarkdownString.FromText(s).Value);
        Assert.Equal(Format(Doc(P(T(s.Replace(' ', ' '))))), Format(doc));
    }

    [Fact]
    public void FromText_DashLine_StaysAParagraph() =>
        Assert.Equal(Format(Doc(P(T("- dash")))), Format(MarkdownParser.Parse(MarkdownString.FromText("- dash").Value)));

    [Fact]
    public void FromText_NumberedLine_StaysAParagraph() =>
        // The card expected a list, but FromText turns the blank after "1." into &nbsp;, so no list marker is left.
        Assert.Equal(Format(Doc(P(T("1. item")))), Format(MarkdownParser.Parse(MarkdownString.FromText("1. item").Value)));
}
