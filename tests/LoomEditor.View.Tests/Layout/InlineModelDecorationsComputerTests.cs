// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/inlineDecorations.test.ts (suite 'InlineModelDecorationsComputer')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Not ported: 'decoration with affectsFont sets hasVariableFonts' (font decorations) and the
// 'InjectedTextInlineDecorationsComputer' suite (injected text: ADR 0009, decision 4).
public class InlineModelDecorationsComputerTests
{
    private static ModelDecoration CreateModelDecoration(string id, Range range, ModelDecorationOptionsData options) =>
        new(id, 0, range, ModelDecorationOptions.CreateDynamic(options));

    private static InlineModelDecorationsComputer CreateComputer(TextModel model, params ModelDecoration[] decorations) =>
        new(_ => decorations, model, new ViewModelLinesFromProjectedModel(0, model, model.Options.TabSize).CreateCoordinatesConverter());

    [Fact]
    public void NoDecorations()
    {
        // Monaco: 'no decorations'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(model);

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Empty(result.Decorations);
        Assert.Single(result.InlineDecorations);
        Assert.Empty(result.InlineDecorations[0]);
        Assert.Equal([false], result.HasVariableFonts);
    }

    [Fact]
    public void InlineClassNameDecorationOnASingleLine()
    {
        // Monaco: 'inline class name decoration on a single line'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "test-class" }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Single(result.Decorations);
        Assert.Equal([new InlineDecoration(new Range(1, 1, 1, 6), "test-class", InlineDecorationType.Regular)], result.InlineDecorations[0]);
        Assert.Equal([false], result.HasVariableFonts);
    }

    [Fact]
    public void InlineClassNameWithAffectsLetterSpacing()
    {
        // Monaco: 'inlineClassName with affectsLetterSpacing'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model,
            CreateModelDecoration("dec1", new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "test-class", InlineClassNameAffectsLetterSpacing = true }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Equal([new InlineDecoration(new Range(1, 1, 1, 6), "test-class", InlineDecorationType.RegularAffectingLetterSpacing)], result.InlineDecorations[0]);
    }

    [Fact]
    public void BeforeContentClassNameDecoration()
    {
        // Monaco: 'beforeContentClassName decoration'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 3, 1, 8), new ModelDecorationOptionsData { Description = "test", BeforeContentClassName = "before-class" }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Equal([new InlineDecoration(new Range(1, 3, 1, 3), "before-class", InlineDecorationType.Before)], result.InlineDecorations[0]);
    }

    [Fact]
    public void AfterContentClassNameDecoration()
    {
        // Monaco: 'afterContentClassName decoration'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 3, 1, 8), new ModelDecorationOptionsData { Description = "test", AfterContentClassName = "after-class" }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Equal([new InlineDecoration(new Range(1, 8, 1, 8), "after-class", InlineDecorationType.After)], result.InlineDecorations[0]);
    }

    [Fact]
    public void AllDecorationTypesCombined()
    {
        // Monaco: 'all decoration types combined'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model,
            CreateModelDecoration(
                "dec1",
                new Range(1, 2, 1, 6),
                new ModelDecorationOptionsData
                {
                    Description = "test",
                    InlineClassName = "inline-class",
                    BeforeContentClassName = "before-class",
                    AfterContentClassName = "after-class",
                }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Equal(
            [
                new InlineDecoration(new Range(1, 2, 1, 6), "inline-class", InlineDecorationType.Regular),
                new InlineDecoration(new Range(1, 2, 1, 2), "before-class", InlineDecorationType.Before),
                new InlineDecoration(new Range(1, 6, 1, 6), "after-class", InlineDecorationType.After),
            ],
            result.InlineDecorations[0]);
    }

    [Fact]
    public void DecorationSpanningMultipleLines()
    {
        // Monaco: 'decoration spanning multiple lines'
        var model = new TextModel("line one\nline two\nline three");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 3, 3, 5), new ModelDecorationOptionsData { Description = "test", InlineClassName = "multi-line" }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 3, 11));

        var expected = new InlineDecoration(new Range(1, 3, 3, 5), "multi-line", InlineDecorationType.Regular);
        Assert.Equal(3, result.InlineDecorations.Count);
        Assert.All(result.InlineDecorations, line => Assert.Equal([expected], line));
    }

    [Fact]
    public void MultipleDecorationsOnDifferentLines()
    {
        // Monaco: 'multiple decorations on different lines'
        var model = new TextModel("line one\nline two");
        InlineModelDecorationsComputer computer = CreateComputer(
            model,
            CreateModelDecoration("dec1", new Range(1, 1, 1, 5), new ModelDecorationOptionsData { Description = "test", InlineClassName = "class-a" }),
            CreateModelDecoration("dec2", new Range(2, 1, 2, 5), new ModelDecorationOptionsData { Description = "test", InlineClassName = "class-b" }));

        ViewDecorationsCollection result = computer.GetDecorations(new Range(1, 1, 2, 9));

        Assert.Equal([new InlineDecoration(new Range(1, 1, 1, 5), "class-a", InlineDecorationType.Regular)], result.InlineDecorations[0]);
        Assert.Equal([new InlineDecoration(new Range(2, 1, 2, 5), "class-b", InlineDecorationType.Regular)], result.InlineDecorations[1]);
    }

    [Fact]
    public void DecorationCacheIsUsedForSameDecorationId()
    {
        // Monaco: 'decoration cache is used for same decoration id'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "test-class" }));

        ViewDecorationsCollection result1 = computer.GetDecorations(new Range(1, 1, 1, 12));
        ViewDecorationsCollection result2 = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.Same(result1.Decorations[0], result2.Decorations[0]);
    }

    [Fact]
    public void ResetClearsDecorationCache()
    {
        // Monaco: 'reset clears decoration cache'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "test-class" }));

        ViewDecorationsCollection result1 = computer.GetDecorations(new Range(1, 1, 1, 12));
        computer.Reset();
        ViewDecorationsCollection result2 = computer.GetDecorations(new Range(1, 1, 1, 12));

        Assert.NotSame(result1.Decorations[0], result2.Decorations[0]);
    }

    [Fact]
    public void GetInlineDecorationsReturnsInlineDecorationsForAModelLine()
    {
        // Monaco: 'getInlineDecorations returns inline decorations for a model line'
        var model = new TextModel("hello world");
        InlineModelDecorationsComputer computer = CreateComputer(
            model, CreateModelDecoration("dec1", new Range(1, 1, 1, 6), new ModelDecorationOptionsData { Description = "test", InlineClassName = "test-class" }));

        IReadOnlyList<IReadOnlyList<InlineDecoration>> result = computer.GetInlineDecorations(1);

        Assert.Single(result);
        Assert.Equal([new InlineDecoration(new Range(1, 1, 1, 6), "test-class", InlineDecorationType.Regular)], result[0]);
    }
}
