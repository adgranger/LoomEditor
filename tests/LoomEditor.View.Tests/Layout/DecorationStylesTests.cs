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

    // registrations.contribution.ts 14–96, style.css
    [Theory]
    [InlineData("line-insert", "diffEditor.insertedLineBackground")]
    [InlineData("line-delete", "diffEditor.removedLineBackground")]
    [InlineData("char-insert", "diffEditor.insertedTextBackground")]
    [InlineData("char-delete", "diffEditor.removedTextBackground")]
    [InlineData("inline-deleted-text", "diffEditor.removedTextBackground")]
    public void DecorationStyles_DiffClasses_HaveRows(string className, string background)
    {
        Assert.Equal(background, DecorationStyles.Get(className)?.BackgroundColorId);
        Assert.True(DecorationStyles.Get("inline-deleted-text")!.StrikeThrough);
        Assert.Equal(GutterIcon.Plus, DecorationStyles.Get("insert-sign")?.GutterIcon);
        Assert.Equal(GutterIcon.Minus, DecorationStyles.Get("delete-sign")?.GutterIcon);
    }

    [Fact]
    public void DecorationStyles_EmptyRange_HasItsOwnRowWithALeftBorder()
    {
        DecorationStyle style = DecorationStyles.Get("char-insert diff-range-empty")!;

        Assert.Equal(3, style.LeftBorderWidth);
        Assert.Equal("diffEditor.insertedTextBackground", style.BorderColorId);
        Assert.Equal("diffEditor.removedTextBackground", DecorationStyles.Get("char-delete diff-range-empty")?.BorderColorId);
    }

    [Fact]
    public void DecorationStyles_GutterInsert_FallsBackToLineBackground()
    {
        DecorationStyle style = DecorationStyles.Get("gutter-insert")!;

        Assert.Equal("diffEditorGutter.insertedLineBackground", style.BackgroundColorId);
        Assert.Equal("diffEditor.insertedLineBackground", style.BackgroundFallbackColorId);
    }

    [Fact]
    public void ColorIds_IncludeUnderlineColors() => Assert.Contains("editorHint.foreground", DecorationStyles.ColorIds);
}
