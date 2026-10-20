using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no such table (it uses CSS): all tests are new.
public class DecorationStylesTests
{
    [Fact]
    public void Get_KnownClass_ReturnsItsRow()
    {
        DecorationStyle? style = DecorationStyles.Get("currentFindMatch");

        Assert.Equal(new DecorationStyle("editor.findMatchBackground", "editor.findMatchBorder", BorderWidth: 2), style);
    }

    [Fact]
    public void Get_ClassList_ReturnsTheFirstKnownClass()
    {
        DecorationStyle? style = DecorationStyles.Get("unknown  wordHighlight findMatch");

        Assert.Equal("editor.wordHighlightBackground", style?.BackgroundColorId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("codicon codicon-unknown")]
    public void Get_UnknownOrNull_ReturnsNull(string? className) => Assert.Null(DecorationStyles.Get(className));
}
