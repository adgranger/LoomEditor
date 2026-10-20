using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Decorations;

// New tests: Monaco has none for ModelDecorationOptions in isolation.
public class ModelDecorationOptionsTests
{
    [Fact]
    public void Register_Defaults_AreMonacosDefaults()
    {
        ModelDecorationOptions options = ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "d" });

        Assert.Equal(TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges, options.Stickiness);
        Assert.Equal(0, options.ZIndex);
        Assert.False(options.IsWholeLine);
        Assert.False(options.ShowIfCollapsed);
        Assert.False(options.CollapseOnReplaceEdit);
        Assert.False(options.ShouldFillLineOnLineBreak);
        Assert.False(options.InlineClassNameAffectsLetterSpacing);
        Assert.False(options.HideInCommentTokens);
        Assert.False(options.HideInStringTokens);
        Assert.Null(options.ClassName);
        Assert.Null(options.GlyphMarginClassName);
        Assert.Null(options.LinesDecorationsClassName);
        Assert.Null(options.FirstLineDecorationClassName);
        Assert.Null(options.MarginClassName);
        Assert.Null(options.InlineClassName);
        Assert.Null(options.BeforeContentClassName);
        Assert.Null(options.AfterContentClassName);
        Assert.False(options.AffectsFont);
    }

    [Fact]
    public void Register_ClassNames_AreCleaned()
    {
        ModelDecorationOptions options = ModelDecorationOptions.Register(new ModelDecorationOptionsData
        {
            Description = "d",
            ClassName = "a.b c#d",
            GlyphMarginClassName = "a.b c#d",
            LinesDecorationsClassName = "a.b c#d",
            FirstLineDecorationClassName = "a.b c#d",
            MarginClassName = "a.b c#d",
            InlineClassName = "a.b c#d",
            BeforeContentClassName = "a.b c#d",
            AfterContentClassName = "a.b c#d",
        });

        string?[] all =
        [
            options.ClassName, options.GlyphMarginClassName, options.LinesDecorationsClassName,
            options.FirstLineDecorationClassName, options.MarginClassName, options.InlineClassName,
            options.BeforeContentClassName, options.AfterContentClassName,
        ];
        Assert.All(all, name => Assert.Equal("a b c d", name));
    }

    [Fact]
    public void CleanClassName_KeepsLettersDigitsDashAndUnderscore()
    {
        Assert.Equal("Find-Match_2", ModelDecorationOptions.CleanClassName("Find-Match_2"));
        Assert.Equal(" ", ModelDecorationOptions.CleanClassName("é"));
    }

    [Fact]
    public void Empty_HasTheDescriptionEmpty()
    {
        Assert.Equal("empty", ModelDecorationOptions.Empty.Description);
    }

    [Fact]
    public void CreateDynamic_ReturnsANewInstanceEachTime()
    {
        var data = new ModelDecorationOptionsData { Description = "d" };

        Assert.NotSame(ModelDecorationOptions.CreateDynamic(data), ModelDecorationOptions.CreateDynamic(data));
    }
}
