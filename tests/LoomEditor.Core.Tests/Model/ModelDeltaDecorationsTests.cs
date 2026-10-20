// Ported from microsoft/vscode: src/vs/editor/test/common/model/modelDecorations.test.ts (suite 'deltaDecorations')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

public class ModelDeltaDecorationsTests
{
    private sealed record LightWeightDecoration(string Id, Range Range);

    private static LightWeightDecoration Decoration(string id, int startLine, int startColumn, int endLine, int endColumn) =>
        new(id, new Range(startLine, startColumn, endLine, endColumn));

    private static ModelDeltaDecoration ToModelDeltaDecoration(LightWeightDecoration dec) =>
        new(dec.Range, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "test", ClassName = dec.Id }));

    private static List<LightWeightDecoration> ReadModelDecorations(TextModel model, IEnumerable<string> ids) =>
        [.. ids.Select(id => new LightWeightDecoration(model.GetDecorationOptions(id)!.ClassName!, model.GetDecorationRange(id)!.Value))];

    private static void TestDeltaDecorations(string[] text, LightWeightDecoration[] decorations, LightWeightDecoration[] newDecorations)
    {
        TextModel model = TestTextModels.Create(string.Join("\n", text));

        // Add initial decorations & assert they are added
        string[] initialIds = model.DeltaDecorations(0, [], [.. decorations.Select(ToModelDeltaDecoration)]);
        List<LightWeightDecoration> actualDecorations = ReadModelDecorations(model, initialIds);

        Assert.Equal(decorations.Length, initialIds.Length);
        Assert.Equal(initialIds.Length, model.GetAllDecorations().Count);
        actualDecorations.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        Array.Sort(decorations, (a, b) => string.CompareOrdinal(a.Id, b.Id));
        Assert.Equal(decorations, actualDecorations);

        string[] newIds = model.DeltaDecorations(0, initialIds, [.. newDecorations.Select(ToModelDeltaDecoration)]);

        Assert.Equal(newDecorations.Length, newIds.Length);
        Assert.Equal(newIds.Length, model.GetAllDecorations().Count);
        // Monaco's final assertion compares the initial decorations again (a copy-paste in the test); kept as is.
        Assert.Equal(decorations, actualDecorations);
    }

    private static readonly string[] Text =
    [
        "This is a text",
        "That has multiple lines",
        "And is very friendly",
        "Towards testing",
    ];

    [Fact]
    public void ResultRespectsInput()
    {
        // Monaco: 'result respects input'
        TextModel model = TestTextModels.Create("Hello world,\nHow are you?");

        string[] ids = model.DeltaDecorations(0, [], [ToModelDeltaDecoration(Decoration("a", 1, 1, 1, 12)), ToModelDeltaDecoration(Decoration("b", 2, 1, 2, 13))]);

        Assert.Equal(new Range(1, 1, 1, 12), model.GetDecorationRange(ids[0]));
        Assert.Equal(new Range(2, 1, 2, 13), model.GetDecorationRange(ids[1]));
    }

    [Fact]
    public void DeltaDecorations1()
    {
        // Monaco: 'deltaDecorations 1'
        TestDeltaDecorations(
            Text,
            [
                Decoration("a", 1, 1, 1, 2),
                Decoration("b", 1, 1, 1, 15),
                Decoration("c", 1, 1, 2, 1),
                Decoration("d", 1, 1, 2, 24),
                Decoration("e", 2, 1, 2, 24),
                Decoration("f", 2, 1, 4, 16),
            ],
            [
                Decoration("x", 1, 1, 1, 2),
                Decoration("b", 1, 1, 1, 15),
                Decoration("c", 1, 1, 2, 1),
                Decoration("d", 1, 1, 2, 24),
                Decoration("e", 2, 1, 2, 21),
                Decoration("f", 2, 17, 4, 16),
            ]);
    }

    [Fact]
    public void DeltaDecorations2()
    {
        // Monaco: 'deltaDecorations 2'
        TestDeltaDecorations(
            Text,
            [
                Decoration("a", 1, 1, 1, 2),
                Decoration("b", 1, 2, 1, 3),
                Decoration("c", 1, 3, 1, 4),
                Decoration("d", 1, 4, 1, 5),
                Decoration("e", 1, 5, 1, 6),
            ],
            [
                Decoration("a", 1, 2, 1, 3),
                Decoration("b", 1, 3, 1, 4),
                Decoration("c", 1, 4, 1, 5),
                Decoration("d", 1, 5, 1, 6),
            ]);
    }

    [Fact]
    public void DeltaDecorations3()
    {
        // Monaco: 'deltaDecorations 3'
        TestDeltaDecorations(
            Text,
            [
                Decoration("a", 1, 1, 1, 2),
                Decoration("b", 1, 2, 1, 3),
                Decoration("c", 1, 3, 1, 4),
                Decoration("d", 1, 4, 1, 5),
                Decoration("e", 1, 5, 1, 6),
            ],
            []);
    }

    [Fact]
    public void ModelDoesNotGetConfusedWithIndividualTrackedRanges()
    {
        // Monaco: 'model doesn't get confused with individual tracked ranges'
        TextModel model = TestTextModels.Create("Hello world,\nHow are you?");

        string trackedRangeId = model.ChangeDecorations(0, accessor => accessor.AddDecoration(
            new Range(1, 1, 1, 1),
            ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "test", Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges })));
        model.ChangeDecorations(0, accessor => accessor.RemoveDecoration(trackedRangeId));

        string[] ids = model.DeltaDecorations(0, [], [ToModelDeltaDecoration(Decoration("a", 1, 1, 1, 12)), ToModelDeltaDecoration(Decoration("b", 2, 1, 2, 13))]);

        Assert.Equal(new Range(1, 1, 1, 12), model.GetDecorationRange(ids[0]));
        Assert.Equal(new Range(2, 1, 2, 13), model.GetDecorationRange(ids[1]));

        ids = model.DeltaDecorations(0, ids, [ToModelDeltaDecoration(Decoration("a", 1, 1, 1, 12)), ToModelDeltaDecoration(Decoration("b", 2, 1, 2, 13))]);

        Assert.Equal(new Range(1, 1, 1, 12), model.GetDecorationRange(ids[0]));
        Assert.Equal(new Range(2, 1, 2, 13), model.GetDecorationRange(ids[1]));
    }

    [Fact]
    public void Issue16922_ClickingOnLinkDoesntSeemToDoAnything()
    {
        // Monaco: 'issue #16922: Clicking on link doesn't seem to do anything'
        TextModel model = TestTextModels.Create("Hello world,\nHow are you?\nFine.\nGood.");

        model.DeltaDecorations(0, [], [.. new[]
        {
            Decoration("1", 1, 1, 1, 1),
            Decoration("2", 1, 13, 1, 13),
            Decoration("3", 2, 1, 2, 1),
            Decoration("4", 2, 1, 2, 4),
            Decoration("5", 2, 8, 2, 13),
            Decoration("6", 3, 1, 4, 6),
            Decoration("x1", 1, 1, 3, 6),
            Decoration("x2", 2, 5, 2, 8),
            Decoration("x3", 1, 1, 2, 8),
            Decoration("x4", 2, 5, 3, 1),
        }.Select(ToModelDeltaDecoration)]);

        var inRangeClassNames = model.GetDecorationsInRange(new Range(2, 6, 2, 6)).Select(d => d.Options.ClassName).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(["x1", "x2", "x3", "x4"], inRangeClassNames);
    }

    [Fact]
    public void Issue41492_UrlHighlightingPersistsAfterPastingOverUrl()
    {
        // Monaco: 'issue #41492: URL highlighting persists after pasting over url'
        TextModel model = TestTextModels.Create("My First Line");

        string id = model.DeltaDecorations(0, [], [new ModelDeltaDecoration(
            new Range(1, 2, 1, 14),
            ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
            {
                Description = "test",
                Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
                CollapseOnReplaceEdit = true,
            }))])[0];
        model.ApplyEdits([TextEdit.Replace(new Range(1, 1, 1, 14), "Some new text that is longer than the previous one")]);

        Assert.Equal(new Range(1, 1, 1, 1), model.GetDecorationRange(id));
    }
}
