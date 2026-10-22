using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model.Events;

namespace LoomEditor.Core.Tests.Model.Events;

public class LineInjectedTextTests
{
    private static ModelDecorationInjectedTextOptions Opts(string content, double? widthInEm = null) =>
        ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = content, WidthInEm = widthInEm });

    private static LineInjectedText Inj(int column, string content, int order = 0, double? widthInEm = null, int line = 1) =>
        new(0, line, column, Opts(content, widthInEm), order);

    private static ModelDecoration Deco(Range range, InjectedTextOptions? before, InjectedTextOptions? after) =>
        new("id", 0, range, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            Before = before,
            After = after,
        }));

    [Fact]
    public void ApplyInjectedText_NullOrEmpty_ReturnsTheLine()
    {
        Assert.Equal("abc", LineInjectedText.ApplyInjectedText("abc", null));
        Assert.Equal("abc", LineInjectedText.ApplyInjectedText("abc", []));
    }

    [Fact]
    public void ApplyInjectedText_TwoInjections_InsertsAtColumns()
    {
        Assert.Equal("abXcdYef", LineInjectedText.ApplyInjectedText("abcdef", [Inj(3, "X"), Inj(5, "Y")]));
    }

    [Fact]
    public void ApplyInjectedText_TwoInjectionsAtTheSameColumn_KeepsTheOrder()
    {
        Assert.Equal("abXYcdef", LineInjectedText.ApplyInjectedText("abcdef", [Inj(3, "X"), Inj(3, "Y", 1)]));
    }

    [Fact]
    public void FromDecorations_BeforeAndAfter_UseStartAndEndOfTheRange()
    {
        var result = LineInjectedText.FromDecorations(
            [Deco(new Range(1, 2, 1, 4), new InjectedTextOptions { Content = "b" }, new InjectedTextOptions { Content = "a" })]);

        Assert.Equal([2, 4], result.Select(r => r.Column));
        Assert.Equal([0, 1], result.Select(r => r.Order));
    }

    [Fact]
    public void FromDecorations_EmptyContentWithoutWidth_IsIgnored()
    {
        Assert.Empty(LineInjectedText.FromDecorations([Deco(new Range(1, 2, 1, 4), new InjectedTextOptions { Content = "" }, null)]));
    }

    [Fact]
    public void FromDecorations_EmptyContentWithWidth_IsKept()
    {
        var result = LineInjectedText.FromDecorations([Deco(new Range(1, 2, 1, 4), new InjectedTextOptions { Content = "", WidthInEm = 1 }, null)]);

        Assert.Single(result);
    }

    [Fact]
    public void FromDecorations_IsSortedByLineColumnThenOrder()
    {
        var result = LineInjectedText.FromDecorations(
        [
            Deco(new Range(2, 1, 2, 5), new InjectedTextOptions { Content = "a" }, null),
            Deco(new Range(1, 3, 1, 3), null, new InjectedTextOptions { Content = "b" }),
            Deco(new Range(1, 3, 1, 3), new InjectedTextOptions { Content = "c" }, null),
            Deco(new Range(1, 1, 1, 1), new InjectedTextOptions { Content = "d" }, null),
        ]);

        Assert.Equal(["d", "c", "b", "a"], result.Select(r => r.Options.Content));
    }

    [Fact]
    public void GetFixedWidthInjectedTextRanges_ComputesOffsetsInTheInjectedLine()
    {
        var result = LineInjectedText.GetFixedWidthInjectedTextRanges([Inj(2, "abc"), Inj(4, "de", widthInEm: 2.5)]);

        Assert.Equal([new FixedWidthInjectedTextRange(6, 8, 2.5)], result);
    }

    [Fact]
    public void GetFixedWidthInjectedTextRanges_WidthOnly_IsAnEmptyRange()
    {
        var result = LineInjectedText.GetFixedWidthInjectedTextRanges([Inj(3, "", widthInEm: 1)]);

        Assert.Equal([new FixedWidthInjectedTextRange(2, 2, 1)], result);
    }

    [Fact]
    public void WithText_ReplacesTheContentOnly()
    {
        var original = Inj(3, "abc", 1, 2, 4);

        var copy = original.WithText("z");

        Assert.Equal("z", copy.Options.Content);
        Assert.Equal("abc", original.Options.Content);
        Assert.Equal((0, 4, 3, 1), (copy.OwnerId, copy.LineNumber, copy.Column, copy.Order));
        Assert.Equal(2, copy.Options.WidthInEm);
    }
}
