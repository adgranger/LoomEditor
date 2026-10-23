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

    [Fact]
    public void SquigglyClasses_HaveUnderlineRows()
    {
        Assert.Equal(new DecorationStyle(Underline: UnderlineStyle.Squiggle, UnderlineColorId: "editorError.foreground"), DecorationStyles.Get("squiggly-error"));
        Assert.Equal("editorWarning.foreground", DecorationStyles.Get("squiggly-warning")?.UnderlineColorId);
        Assert.Equal("editorInfo.foreground", DecorationStyles.Get("squiggly-info")?.UnderlineColorId);
        Assert.Equal(new DecorationStyle(Underline: UnderlineStyle.Dots, UnderlineColorId: "editorHint.foreground"), DecorationStyles.Get("squiggly-hint"));
    }

    [Theory]
    [InlineData("squiggly-unnecessary")]
    [InlineData("squiggly-inline-unnecessary")]
    [InlineData("squiggly-inline-deprecated")]
    public void UnnecessaryClasses_HaveNoRow(string className) => Assert.Null(DecorationStyles.Get(className));

    [Fact]
    public void ColorIds_IncludeUnderlineColors() => Assert.Contains("editorHint.foreground", DecorationStyles.ColorIds);
}
