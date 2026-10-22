using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

// New tests: the FoldingAction classes of folding.ts have no upstream test.
public class FoldingCommandsTests
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMilliseconds(300);

    private const string Nested = "class A {\n  void Foo() {\n    if (x) {\n      y();\n    }\n  }\n}";
    private const string Siblings = "a\n  b\nc\n  d\ne\n  f";

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, FoldingFeature Feature) Create(string text, string? languageId = null)
    {
        var context = new EditorContext(_scheduler);
        context.SetText(text);
        if (languageId is not null)
        {
            context.Highlighter.SetLanguage(languageId);
        }

        var feature = new FoldingFeature();
        feature.Attach(context);
        _scheduler.AdvanceBy(FirstDelay);
        return (context, feature);
    }

    private static void Run(EditorContext context, string id) => Assert.True(context.Commands.Execute(id), id);

    private static List<(int Start, int End)> Collapsed(FoldingFeature feature)
    {
        FoldingRegions regions = feature.FoldingModel!.Regions;
        var result = new List<(int, int)>();
        for (int i = 0; i < regions.Length; i++)
        {
            if (regions.IsCollapsed(i))
            {
                result.Add((regions.GetStartLineNumber(i), regions.GetEndLineNumber(i)));
            }
        }

        return result;
    }

    [Fact]
    public void Fold_FoldsTheRegionAtTheCursor()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);
        context.Selection = new Selection(3, 1, 3, 1);

        Run(context, FoldingCommands.Fold);

        Assert.Equal([(3, 4)], Collapsed(feature));
    }

    [Fact]
    public void Unfold_UnfoldsIt()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);
        context.Selection = new Selection(3, 1, 3, 1);
        Run(context, FoldingCommands.Fold);

        Run(context, FoldingCommands.Unfold);

        Assert.Empty(Collapsed(feature));
    }

    [Fact]
    public void ToggleFold_Twice_RestoresTheState()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);
        context.Selection = new Selection(2, 1, 2, 1);

        Run(context, FoldingCommands.ToggleFold);
        Assert.Equal([(2, 5)], Collapsed(feature));

        Run(context, FoldingCommands.ToggleFold);
        Assert.Empty(Collapsed(feature));
    }

    [Fact]
    public void FoldRecursively_FoldsTheRegionAndItsChildren()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);
        context.Selection = new Selection(2, 1, 2, 1);

        Run(context, FoldingCommands.FoldRecursively);

        Assert.Equal([(2, 5), (3, 4)], Collapsed(feature));
    }

    [Fact]
    public void UnfoldRecursively_UnfoldsAll()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);
        Run(context, FoldingCommands.FoldAll);
        Assert.Equal(3, Collapsed(feature).Count);

        Run(context, FoldingCommands.UnfoldRecursively);

        Assert.Empty(Collapsed(feature));
    }

    [Fact]
    public void FoldAll_ThenUnfoldAll()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);

        Run(context, FoldingCommands.FoldAll);
        Assert.Equal([(1, 6), (2, 5), (3, 4)], Collapsed(feature));
        Assert.Equal(2, context.ViewModel.GetLineCount());

        Run(context, FoldingCommands.UnfoldAll);
        Assert.Empty(Collapsed(feature));
    }

    [Fact]
    public void FoldLevel2_FoldsOnlySecondLevelRegions()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);

        Run(context, FoldingCommands.FoldLevel(2));

        Assert.Equal([(2, 5)], Collapsed(feature));
    }

    [Fact]
    public void FoldAllExcept_KeepsTheCursorRegionsOpen()
    {
        (EditorContext context, FoldingFeature feature) = Create(Siblings);
        context.Selection = new Selection(2, 1, 2, 1);

        Run(context, FoldingCommands.FoldAllExcept);

        Assert.Equal([(3, 4), (5, 6)], Collapsed(feature));
    }

    [Fact]
    public void FoldAllMarkerRegions_FoldsRegionMarkers()
    {
        (EditorContext context, FoldingFeature feature) = Create("#region R\nclass A {\n  int x;\n}\n#endregion", "csharp");

        Run(context, FoldingCommands.FoldAllMarkerRegions);
        Assert.Equal([(1, 5)], Collapsed(feature));

        Run(context, FoldingCommands.UnfoldAllMarkerRegions);
        Assert.Empty(Collapsed(feature));
    }

    [Fact]
    public void FoldAllBlockComments_FoldsBlockComments()
    {
        (EditorContext context, FoldingFeature feature) = Create("/* c\n * d\n */\n/**\n * doc\n */\nclass A {\n  int x;\n}", "csharp");

        Run(context, FoldingCommands.FoldAllBlockComments);

        Assert.Equal([(1, 3), (4, 6)], Collapsed(feature));
    }

    [Fact]
    public void GotoParentFold_MovesToTheStartOfTheFold()
    {
        (EditorContext context, _) = Create(Siblings);
        context.Selection = new Selection(4, 1, 4, 1);

        Run(context, FoldingCommands.GotoParentFold);

        Assert.Equal(new Selection(3, 1, 3, 1), context.Selection);
    }

    [Fact]
    public void GotoNextFold_MovesToTheNextSibling()
    {
        (EditorContext context, _) = Create(Siblings);
        context.Selection = new Selection(1, 1, 1, 1);

        Run(context, FoldingCommands.GotoNextFold);

        Assert.Equal(new Selection(3, 1, 3, 1), context.Selection);
    }

    [Fact]
    public void GotoPreviousFold_MovesToThePreviousSibling()
    {
        (EditorContext context, _) = Create(Siblings);
        context.Selection = new Selection(5, 1, 5, 1);

        Run(context, FoldingCommands.GotoPreviousFold);

        Assert.Equal(new Selection(3, 1, 3, 1), context.Selection);
    }

    [Fact]
    public void CreateFoldingRangeFromSelection_FoldsTheSelectedLines()
    {
        (EditorContext context, FoldingFeature feature) = Create("a\nb\nc\nd\ne");
        context.Selection = new Selection(2, 1, 4, 3);

        Run(context, FoldingCommands.CreateFoldingRangeFromSelection);

        Assert.Equal([(2, 4)], Collapsed(feature));
        Assert.Equal(FoldSource.UserDefined, feature.FoldingModel!.Regions.GetSource(0));
        Assert.Equal(new Selection(2, 1, 2, 1), context.Selection);
    }

    [Fact]
    public void RemoveManualFoldingRanges_RemovesIt()
    {
        (EditorContext context, FoldingFeature feature) = Create("a\nb\nc\nd\ne");
        context.Selection = new Selection(2, 1, 4, 3);
        Run(context, FoldingCommands.CreateFoldingRangeFromSelection);
        Assert.Equal(1, feature.FoldingModel!.Regions.Length);

        Run(context, FoldingCommands.RemoveManualFoldingRanges);

        Assert.Equal(0, feature.FoldingModel!.Regions.Length);
        Assert.Equal(5, context.ViewModel.GetLineCount());
    }

    [Fact]
    public void FoldingDisabled_CommandsDoNothing()
    {
        (EditorContext context, FoldingFeature feature) = Create(Nested);
        feature.SetOptions(EditorFoldingOptions.Default with { Folding = false });

        Run(context, FoldingCommands.FoldAll);

        Assert.Null(feature.FoldingModel);
        Assert.Empty(context.ViewModel.GetHiddenAreas());
    }
}
