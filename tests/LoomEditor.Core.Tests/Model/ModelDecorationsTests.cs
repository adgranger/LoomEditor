// Ported from microsoft/vscode: src/vs/editor/test/common/model/modelDecorations.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

public class ModelDecorationsTests
{
    private const string Line1 = "My First Line";
    private const string Line2 = "\t\tMy Second Line";
    private const string Line3 = "    Third Line";
    private const string Line4 = "";
    private const string Line5 = "1";

    private readonly TextModel _model = TestTextModels.Create(Line1 + "\r\n" + Line2 + "\n" + Line3 + "\n" + Line4 + "\r\n" + Line5);

    // --------- utils

    private static ModelDecorationOptions Options(string? className) =>
        ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "test", ClassName = className });

    private static void ModelHasDecorations(TextModel model, params (Range Range, string? ClassName)[] expected)
    {
        var actual = model.GetAllDecorations()
            .Select(d => (d.Range, d.Options.ClassName))
            .OrderBy(d => d.Range, Comparer<Range>.Create(Range.CompareUsingStarts))
            .ToList();
        Assert.Equal(expected, actual);
    }

    private static void ModelHasDecoration(TextModel model, int startLine, int startColumn, int endLine, int endColumn, string className) =>
        ModelHasDecorations(model, (new Range(startLine, startColumn, endLine, endColumn), className));

    private static void ModelHasNoDecorations(TextModel model) => Assert.Empty(model.GetAllDecorations());

    private static string AddDecoration(TextModel model, int startLine, int startColumn, int endLine, int endColumn, string className) =>
        model.ChangeDecorations(0, accessor => accessor.AddDecoration(new Range(startLine, startColumn, endLine, endColumn), Options(className)));

    private static void LineHasDecorations(TextModel model, int lineNumber, params (int Start, int End, string ClassName)[] expected)
    {
        var actual = model.GetLineDecorations(lineNumber).Select(d => (d.Range.StartColumn, d.Range.EndColumn, d.Options.ClassName!)).ToList();
        Assert.Equal(expected, actual);
    }

    private static void LineHasNoDecorations(TextModel model, int lineNumber) => LineHasDecorations(model, lineNumber);

    private static void LineHasDecoration(TextModel model, int lineNumber, int start, int end, string className) =>
        LineHasDecorations(model, lineNumber, (start, end, className));

    // --------- Model Decorations

    [Fact]
    public void SingleCharacterDecoration()
    {
        // Monaco: 'single character decoration'
        AddDecoration(_model, 1, 1, 1, 2, "myType");
        LineHasDecoration(_model, 1, 1, 2, "myType");
        LineHasNoDecorations(_model, 2);
        LineHasNoDecorations(_model, 3);
        LineHasNoDecorations(_model, 4);
        LineHasNoDecorations(_model, 5);
    }

    [Fact]
    public void LineDecoration()
    {
        // Monaco: 'line decoration'
        AddDecoration(_model, 1, 1, 1, 14, "myType");
        LineHasDecoration(_model, 1, 1, 14, "myType");
        LineHasNoDecorations(_model, 2);
        LineHasNoDecorations(_model, 3);
        LineHasNoDecorations(_model, 4);
        LineHasNoDecorations(_model, 5);
    }

    [Fact]
    public void FullLineDecoration()
    {
        // Monaco: 'full line decoration'
        AddDecoration(_model, 1, 1, 2, 1, "myType");

        IReadOnlyList<ModelDecoration> line1Decorations = _model.GetLineDecorations(1);
        Assert.Single(line1Decorations);
        Assert.Equal("myType", line1Decorations[0].Options.ClassName);

        IReadOnlyList<ModelDecoration> line2Decorations = _model.GetLineDecorations(1);
        Assert.Single(line2Decorations);
        Assert.Equal("myType", line2Decorations[0].Options.ClassName);

        LineHasNoDecorations(_model, 3);
        LineHasNoDecorations(_model, 4);
        LineHasNoDecorations(_model, 5);
    }

    [Fact]
    public void MultipleLineDecoration()
    {
        // Monaco: 'multiple line decoration'
        AddDecoration(_model, 1, 2, 3, 2, "myType");

        for (int i = 0; i < 3; i++)
        {
            // Monaco asks for line 1 three times
            IReadOnlyList<ModelDecoration> decorations = _model.GetLineDecorations(1);
            Assert.Single(decorations);
            Assert.Equal("myType", decorations[0].Options.ClassName);
        }

        LineHasNoDecorations(_model, 4);
        LineHasNoDecorations(_model, 5);
    }

    // --------- removing, changing decorations

    [Fact]
    public void DecorationGetsRemoved()
    {
        // Monaco: 'decoration gets removed'
        string decId = AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        _model.ChangeDecorations(0, accessor => accessor.RemoveDecoration(decId));
        ModelHasNoDecorations(_model);
    }

    [Fact]
    public void DecorationsGetRemoved()
    {
        // Monaco: 'decorations get removed'
        string decId1 = AddDecoration(_model, 1, 2, 3, 2, "myType1");
        string decId2 = AddDecoration(_model, 1, 2, 3, 1, "myType2");
        ModelHasDecorations(
            _model,
            (new Range(1, 2, 3, 1), "myType2"),
            (new Range(1, 2, 3, 2), "myType1"));
        _model.ChangeDecorations(0, accessor => accessor.RemoveDecoration(decId1));
        ModelHasDecorations(_model, (new Range(1, 2, 3, 1), "myType2"));
        _model.ChangeDecorations(0, accessor => accessor.RemoveDecoration(decId2));
        ModelHasNoDecorations(_model);
    }

    [Fact]
    public void DecorationRangeCanBeChanged()
    {
        // Monaco: 'decoration range can be changed'
        string decId = AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        _model.ChangeDecorations(0, accessor => accessor.ChangeDecoration(decId, new Range(1, 1, 1, 2)));
        ModelHasDecoration(_model, 1, 1, 1, 2, "myType");
    }

    // --------- editing text & effects on decorations

    private void Insert(int line, int column, string text) => _model.ApplyEdits([TextEdit.Insert(new Position(line, column), text)]);

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingOneLineTextBeforeIt()
    {
        // Monaco: 'decorations are updated when inserting one line text before it'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        Insert(1, 1, "Hallo ");
        ModelHasDecoration(_model, 1, 8, 3, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingOneLineTextBeforeIt2()
    {
        // Monaco: 'decorations are updated when inserting one line text before it 2'
        AddDecoration(_model, 1, 1, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 1, 3, 2, "myType");
        _model.ApplyEdits([TextEdit.Replace(new Range(1, 1, 1, 1), "Hallo ")]);
        ModelHasDecoration(_model, 1, 1, 3, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingMultipleLinesTextBeforeIt()
    {
        // Monaco: 'decorations are updated when inserting multiple lines text before it'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        Insert(1, 1, "Hallo\nI'm inserting multiple\nlines");
        ModelHasDecoration(_model, 3, 7, 5, 2, "myType");
    }

    [Fact]
    public void DecorationsChangeWhenInsertingTextAfterThem()
    {
        // Monaco: 'decorations change when inserting text after them'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        Insert(3, 2, "Hallo");
        ModelHasDecoration(_model, 1, 2, 3, 7, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingTextInside()
    {
        // Monaco: 'decorations are updated when inserting text inside'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        Insert(1, 3, "Hallo ");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingTextInside2()
    {
        // Monaco: 'decorations are updated when inserting text inside 2'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        Insert(3, 1, "Hallo ");
        ModelHasDecoration(_model, 1, 2, 3, 8, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingTextInside3()
    {
        // Monaco: 'decorations are updated when inserting text inside 3'
        AddDecoration(_model, 1, 1, 2, 16, "myType");
        ModelHasDecoration(_model, 1, 1, 2, 16, "myType");
        Insert(2, 2, "\n");
        ModelHasDecoration(_model, 1, 1, 3, 15, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenInsertingMultipleLinesTextInside()
    {
        // Monaco: 'decorations are updated when inserting multiple lines text inside'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        Insert(1, 3, "Hallo\nI'm inserting multiple\nlines");
        ModelHasDecoration(_model, 1, 2, 5, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenDeletingOneLineTextBeforeIt()
    {
        // Monaco: 'decorations are updated when deleting one line text before it'
        AddDecoration(_model, 1, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 1, 2, 3, 2, "myType");
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 2))]);
        ModelHasDecoration(_model, 1, 1, 3, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenDeletingMultipleLinesTextBeforeIt()
    {
        // Monaco: 'decorations are updated when deleting multiple lines text before it'
        AddDecoration(_model, 2, 2, 3, 2, "myType");
        ModelHasDecoration(_model, 2, 2, 3, 2, "myType");
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 2, 1))]);
        ModelHasDecoration(_model, 1, 2, 2, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenDeletingMultipleLinesTextBeforeIt2()
    {
        // Monaco: 'decorations are updated when deleting multiple lines text before it 2'
        AddDecoration(_model, 2, 3, 3, 2, "myType");
        ModelHasDecoration(_model, 2, 3, 3, 2, "myType");
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 2, 2))]);
        ModelHasDecoration(_model, 1, 2, 2, 2, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenDeletingTextInside()
    {
        // Monaco: 'decorations are updated when deleting text inside'
        AddDecoration(_model, 1, 2, 4, 1, "myType");
        ModelHasDecoration(_model, 1, 2, 4, 1, "myType");
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 3, 2, 1))]);
        ModelHasDecoration(_model, 1, 2, 3, 1, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenDeletingTextInside2()
    {
        // Monaco: 'decorations are updated when deleting text inside 2'
        AddDecoration(_model, 1, 2, 4, 1, "myType");
        ModelHasDecoration(_model, 1, 2, 4, 1, "myType");
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 2)), TextEdit.Delete(new Range(4, 1, 4, 1))]);
        ModelHasDecoration(_model, 1, 1, 4, 1, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenDeletingMultipleLinesText()
    {
        // Monaco: 'decorations are updated when deleting multiple lines text'
        AddDecoration(_model, 1, 2, 4, 1, "myType");
        ModelHasDecoration(_model, 1, 2, 4, 1, "myType");
        _model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 3, 1))]);
        ModelHasDecoration(_model, 1, 1, 2, 1, "myType");
    }

    [Fact]
    public void DecorationsAreUpdatedWhenChangingEol()
    {
        // Monaco: 'decorations are updated when changing EOL'
        for (int i = 1; i <= 9; i++)
        {
            AddDecoration(_model, 1, i + 1, 4, 1, "myType" + i);
        }

        Insert(1, 1, "x");
        _model.SetEol(EndOfLineSequence.CRLF);
        Insert(1, 1, "x");

        ModelHasDecorations(
            _model,
            [.. Enumerable.Range(1, 9).Select(i => (new Range(1, i + 3, 4, 1), (string?)("myType" + i)))]);
    }

    [Fact]
    public void AnApparentlySimpleEdit()
    {
        // Monaco: 'an apparently simple edit'
        AddDecoration(_model, 1, 2, 4, 1, "myType1");
        _model.ApplyEdits([TextEdit.Replace(new Range(1, 14, 2, 1), "x")]);
        ModelHasDecorations(_model, (new Range(1, 2, 3, 1), "myType1"));
    }

    [Fact]
    public void RemoveAllDecorationsWithOwnerIdWorks()
    {
        // Monaco: 'removeAllDecorationsWithOwnerId works'
        _model.DeltaDecorations(1, [], [new ModelDeltaDecoration(new Range(1, 2, 4, 1), Options("myType1"))]);
        _model.RemoveAllDecorationsWithOwnerId(1);
        ModelHasNoDecorations(_model);
    }

    // --------- new tests

    [Fact]
    public void ChangeDecorations_AccessorUsedAfterTheCallback_Throws()
    {
        IModelDecorationsChangeAccessor? kept = null;
        _model.ChangeDecorations(0, accessor => kept = accessor);

        Assert.Throws<InvalidOperationException>(() => kept!.AddDecoration(new Range(1, 1, 1, 2), Options("a")));
        Assert.Throws<InvalidOperationException>(() => kept!.RemoveDecoration("x"));
        Assert.Throws<InvalidOperationException>(() => kept!.DeltaDecorations([], []));
    }

    [Fact]
    public void DeltaDecorations_UnknownOldId_IsSkipped()
    {
        string[] ids = _model.DeltaDecorations(0, ["nope"], [new ModelDeltaDecoration(new Range(1, 1, 1, 2), Options("a"))]);

        Assert.Single(ids);
        Assert.Equal(new Range(1, 1, 1, 2), _model.GetDecorationRange(ids[0]));
        Assert.Single(_model.GetAllDecorations());
    }

    [Fact]
    public void GetDecorationsInRange_OwnerFilter_SeesOwnAndOwnerZeroDecorations()
    {
        var range = new Range(1, 1, 1, 5);
        _model.DeltaDecorations(0, [], [new ModelDeltaDecoration(range, Options("zero"))]);
        _model.DeltaDecorations(1, [], [new ModelDeltaDecoration(range, Options("one"))]);
        _model.DeltaDecorations(2, [], [new ModelDeltaDecoration(range, Options("two"))]);

        var seenByOne = _model.GetDecorationsInRange(range, ownerId: 1).Select(d => d.Options.ClassName).Order().ToList();

        Assert.Equal(["one", "zero"], seenByOne);
    }

    [Fact]
    public void SetValue_RemovesAllDecorations()
    {
        AddDecoration(_model, 1, 1, 1, 2, "myType");

        _model.SetValue("new text");

        ModelHasNoDecorations(_model);
    }
}
