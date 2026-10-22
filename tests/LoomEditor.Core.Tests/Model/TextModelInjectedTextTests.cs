using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

public class TextModelInjectedTextTests
{
    private static ModelDeltaDecoration Injected(Range range, string? before, string? after = null, bool showIfCollapsed = false) =>
        new(range, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "test",
            ShowIfCollapsed = showIfCollapsed,
            Before = before is null ? null : new InjectedTextOptions { Content = before },
            After = after is null ? null : new InjectedTextOptions { Content = after },
        }));

    [Fact]
    public void GetLineInjectedText_NoDecorations_IsEmpty()
    {
        TextModel model = TestTextModels.Create("abc\ndef");

        Assert.Empty(model.GetLineInjectedText(1));
    }

    [Fact]
    public void GetLineInjectedText_AfterDecoration_IsAtTheEndColumn()
    {
        TextModel model = TestTextModels.Create("abcdef");
        model.DeltaDecorations(0, [], [Injected(new Range(1, 2, 1, 4), null, "X")]);

        LineInjectedText_Assert(model.GetLineInjectedText(1), (4, "X"));
    }

    [Fact]
    public void GetLineInjectedText_EmptyRangeWithoutShowIfCollapsed_IsIgnored()
    {
        TextModel model = TestTextModels.Create("abcdef");
        model.DeltaDecorations(0, [], [Injected(new Range(1, 3, 1, 3), "X")]);

        Assert.Empty(model.GetLineInjectedText(1));
    }

    [Fact]
    public void GetLineInjectedText_EmptyRangeWithShowIfCollapsed_IsKept()
    {
        TextModel model = TestTextModels.Create("abcdef");
        model.DeltaDecorations(0, [], [Injected(new Range(1, 3, 1, 3), "X", showIfCollapsed: true)]);

        LineInjectedText_Assert(model.GetLineInjectedText(1), (3, "X"));
    }

    [Fact]
    public void GetLineInjectedText_FiltersByOwner()
    {
        TextModel model = TestTextModels.Create("abcdef");
        model.DeltaDecorations(1, [], [Injected(new Range(1, 1, 1, 2), "A")]);
        model.DeltaDecorations(2, [], [Injected(new Range(1, 3, 1, 4), "B")]);

        Assert.Equal(2, model.GetLineInjectedText(1).Count);
        LineInjectedText_Assert(model.GetLineInjectedText(1, 1), (1, "A"));
        LineInjectedText_Assert(model.GetLineInjectedText(1, 2), (3, "B"));
    }

    [Fact]
    public void GetLineInjectedText_MultiLineDecoration_ReportsBeforeAndAfterOnTheirLines()
    {
        TextModel model = TestTextModels.Create("abc\ndef");
        model.DeltaDecorations(0, [], [Injected(new Range(1, 2, 2, 3), "B", "A")]);

        LineInjectedText_Assert(model.GetLineInjectedText(1), (2, "B"));
        LineInjectedText_Assert(model.GetLineInjectedText(2), (3, "A"));
    }

    [Fact]
    public void GetLineInjectedText_IgnoresDecorationsWithoutInjectedText()
    {
        TextModel model = TestTextModels.Create("abcdef");
        model.DeltaDecorations(0, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "plain" }))]);

        Assert.Empty(model.GetLineInjectedText(1));
    }

    [Fact]
    public void GetInjectedTextDecorations_ReturnsOnlyInjectedText()
    {
        TextModel model = TestTextModels.Create("abc\ndef");
        model.DeltaDecorations(
            0,
            [],
            [
                new ModelDeltaDecoration(new Range(1, 1, 1, 3), ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "plain" })),
                Injected(new Range(2, 1, 2, 3), "X"),
            ]);

        ModelDecoration only = Assert.Single(model.GetInjectedTextDecorations());

        Assert.Equal(new Range(2, 1, 2, 3), only.Range);
    }

    private static void LineInjectedText_Assert(IReadOnlyList<LoomEditor.Core.Model.Events.LineInjectedText> actual, params (int Column, string Content)[] expected) =>
        Assert.Equal(expected, actual.Select(t => (t.Column, t.Options.Content)));
}
