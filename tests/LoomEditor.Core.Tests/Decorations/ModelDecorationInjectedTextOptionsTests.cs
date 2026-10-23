using LoomEditor.Core.Decorations;

namespace LoomEditor.Core.Tests.Decorations;

public class ModelDecorationInjectedTextOptionsTests
{
    [Fact]
    public void From_NullContent_BecomesEmpty()
    {
        var options = ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = null! });

        Assert.Equal("", options.Content);
    }

    [Fact]
    public void From_EmptyInlineClassName_BecomesNull()
    {
        Assert.Null(ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = "x", InlineClassName = "" }).InlineClassName);
        Assert.Equal("a b!", ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = "x", InlineClassName = "a b!" }).InlineClassName);
    }

    [Fact]
    public void From_KeepsAttachedDataAndCursorStops()
    {
        var data = new object();

        var options = ModelDecorationInjectedTextOptions.From(new InjectedTextOptions
        {
            Content = "x",
            AttachedData = data,
            CursorStops = InjectedTextCursorStops.Left,
            InlineClassNameAffectsLetterSpacing = true,
        });

        Assert.Same(data, options.AttachedData);
        Assert.Equal(InjectedTextCursorStops.Left, options.CursorStops);
        Assert.True(options.InlineClassNameAffectsLetterSpacing);
    }

    [Fact]
    public void Register_WithBeforeAndAfter_ExposesNormalizedOptions()
    {
        var options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            Before = new InjectedTextOptions { Content = "b" },
            After = new InjectedTextOptions { Content = "a", WidthInEm = 2 },
        });

        Assert.Equal("b", options.Before!.Content);
        Assert.Equal("a", options.After!.Content);
        Assert.Equal(2, options.After.WidthInEm);
    }

    [Fact]
    public void Register_WithoutInjectedText_HasNullBeforeAndAfter()
    {
        var options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "test" });

        Assert.Null(options.Before);
        Assert.Null(options.After);
    }
}
