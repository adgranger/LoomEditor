// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/hiddenRangeModel.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.Features.Folding.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

public class HiddenRangeModelTests
{
    private static (TextModel Text, FoldingModel Folding, HiddenRangeModel Hidden) Create(params string[] lines)
    {
        var textModel = new TextModel(string.Join('\n', lines));
        var foldingModel = new FoldingModel(textModel, new TestDecorationProvider(textModel));
        var hiddenRangeModel = new HiddenRangeModel(foldingModel);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));
        return (textModel, foldingModel, hiddenRangeModel);
    }

    private static void AssertRanges(IReadOnlyList<Range> actual, params (int Start, int End)[] expected) =>
        Assert.Equal(expected, actual.Select(r => (r.StartLineNumber, r.EndLineNumber)));

    private static void AssertHidden(HiddenRangeModel model, params int[] hiddenLines)
    {
        for (int line = 1; line <= 10; line++)
        {
            Assert.Equal(hiddenLines.Contains(line), model.IsHidden(line));
        }
    }

    // Monaco: 'Hidden Range Model' › 'hasRanges'
    [Fact]
    public void HasRanges()
    {
        var textModel = new TextModel(string.Join('\n',
            "/**", " * Comment", " */", "class A {", "  void foo() {", "    if (true) {", "      //hello", "    }", "  }", "}"));
        var foldingModel = new FoldingModel(textModel, new TestDecorationProvider(textModel));
        var hiddenRangeModel = new HiddenRangeModel(foldingModel);

        Assert.False(hiddenRangeModel.HasRanges());

        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!, foldingModel.GetRegionAtLine(6)!]);
        AssertRanges(hiddenRangeModel.HiddenRanges, (2, 3), (7, 7));
        Assert.True(hiddenRangeModel.HasRanges());
        AssertHidden(hiddenRangeModel, 2, 3, 7);

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(4)!]);
        AssertRanges(hiddenRangeModel.HiddenRanges, (2, 3), (5, 9));
        Assert.True(hiddenRangeModel.HasRanges());
        AssertHidden(hiddenRangeModel, 2, 3, 5, 6, 7, 8, 9);

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!, foldingModel.GetRegionAtLine(6)!, foldingModel.GetRegionAtLine(4)!]);
        AssertRanges(hiddenRangeModel.HiddenRanges);
        Assert.False(hiddenRangeModel.HasRanges());
        AssertHidden(hiddenRangeModel);
    }

    [Fact]
    public void AdjustSelections_SelectionInHiddenRange_MovesToTheEndOfTheHeaderLine()
    {
        (_, FoldingModel foldingModel, HiddenRangeModel hidden) = Create("a", "  b", "  c", "d");
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);
        Selection[] selections = [new Selection(3, 1, 3, 2), new Selection(4, 1, 4, 1)];

        bool changed = hidden.AdjustSelections(selections);

        Assert.True(changed);
        Assert.Equal(new Selection(1, 2, 1, 2), selections[0]);
        Assert.Equal(new Selection(4, 1, 4, 1), selections[1]);
    }

    [Fact]
    public void AdjustSelections_NothingHidden_ChangesNothing()
    {
        (_, _, HiddenRangeModel hidden) = Create("a", "  b", "  c", "d");
        Selection[] selections = [new Selection(3, 1, 3, 2)];

        Assert.False(hidden.AdjustSelections(selections));
        Assert.Equal(new Selection(3, 1, 3, 2), selections[0]);
    }

    [Fact]
    public void NotifyChangeModelContent_LineInserted_RaisesChangedAgain()
    {
        (TextModel textModel, FoldingModel foldingModel, HiddenRangeModel hidden) = Create("a", "  b", "  c", "d");
        textModel.ContentChanged += (_, e) => hidden.NotifyChangeModelContent(e);
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);
        int raised = 0;
        hidden.Changed += (_, _) => raised++;

        // The same regions again: nothing changes, so nothing is raised.
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));
        Assert.Equal(0, raised);

        // A line is inserted below: the ranges are the same, but the view must be told.
        textModel.ApplyEdits([TextEdit.Insert(new Position(4, 1), "x\n")]);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        Assert.Equal(1, raised);
        AssertRanges(hidden.HiddenRanges, (2, 3));
    }
}
