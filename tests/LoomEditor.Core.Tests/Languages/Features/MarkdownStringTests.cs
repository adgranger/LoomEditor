using LoomEditor.Core.Languages.Features;

namespace LoomEditor.Core.Tests.Languages.Features;

public class MarkdownStringTests
{
    // Monaco: MarkdownString.appendText
    [Fact]
    public void FromText_PlainText_Unchanged() => Assert.Equal("abc", MarkdownString.FromText("abc").Value);

    // Monaco: MarkdownString.appendText (escapeMarkdownSyntaxTokens)
    [Fact]
    public void FromText_EscapesSyntaxTokens() => Assert.Equal("a\\*b\\_c", MarkdownString.FromText("a*b_c").Value);

    // Monaco: MarkdownString.appendText
    [Fact]
    public void FromText_SpacesBecomeNbsp()
    {
        Assert.Equal("a&nbsp;&nbsp;b", MarkdownString.FromText("a  b").Value);
        Assert.Equal("a&nbsp;b", MarkdownString.FromText("a\tb").Value);
    }

    // Monaco: MarkdownString.appendText
    [Fact]
    public void FromText_GreaterThanEscaped() => Assert.Equal("\\>x", MarkdownString.FromText(">x").Value);

    // Monaco: MarkdownString.appendText (Paragraph newline style)
    [Fact]
    public void FromText_NewlineBecomesParagraph() => Assert.Equal("a\n\nb", MarkdownString.FromText("a\nb").Value);

    // Monaco: escapeMarkdownSyntaxTokens
    [Fact]
    public void FromText_LeadingDashEscaped()
    {
        Assert.Equal("\\-&nbsp;x", MarkdownString.FromText("- x").Value);
        Assert.Equal("a-b", MarkdownString.FromText("a-b").Value);
    }

    // Monaco: isEmptyMarkdownString
    [Fact]
    public void IsNullOrEmpty_One()
    {
        Assert.True(MarkdownString.IsNullOrEmpty((MarkdownString?)null));
        Assert.True(MarkdownString.IsNullOrEmpty(new MarkdownString("")));
        Assert.False(MarkdownString.IsNullOrEmpty(new MarkdownString("x")));
    }

    // Monaco: isEmptyMarkdownString
    [Fact]
    public void IsNullOrEmpty_List_EveryEmpty()
    {
        Assert.True(MarkdownString.IsNullOrEmpty((IReadOnlyList<MarkdownString>?)null));
        Assert.True(MarkdownString.IsNullOrEmpty(Array.Empty<MarkdownString>()));
        Assert.True(MarkdownString.IsNullOrEmpty([new MarkdownString("")]));
        Assert.True(MarkdownString.IsNullOrEmpty([new MarkdownString(""), new MarkdownString("")]));
        Assert.False(MarkdownString.IsNullOrEmpty([new MarkdownString(""), new MarkdownString("x")]));
    }
}
