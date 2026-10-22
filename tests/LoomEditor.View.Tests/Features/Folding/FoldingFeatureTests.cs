using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

// New tests: folding.ts (the controller) has no upstream test.
public class FoldingFeatureTests
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(200);

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, FoldingFeature Feature) Create(string text)
    {
        var context = new EditorContext(_scheduler);
        context.SetText(text);
        var feature = new FoldingFeature();
        feature.Attach(context);
        return (context, feature);
    }

    private (EditorContext Context, FoldingFeature Feature) CreateComputed(string text)
    {
        (EditorContext context, FoldingFeature feature) = Create(text);
        _scheduler.AdvanceBy(FirstDelay);
        return (context, feature);
    }

    private static int FoldingDecorationCount(EditorContext context) =>
        context.Model.GetAllDecorations().Count(d => d.OwnerId == context.Id);

    [Fact]
    public void Attach_ComputesRangesAfterTheFirstDelay()
    {
        (_, FoldingFeature feature) = Create("a\n  b\n  c\nd");

        _scheduler.AdvanceBy(FirstDelay - TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, feature.FoldingModel!.Regions.Length);

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, feature.FoldingModel!.Regions.Length);
    }

    [Fact]
    public void Edit_RecomputesRangesAfter200Ms()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("a\n  b\n  c\nd");

        context.Model.ApplyEdits([TextEdit.Insert(new Position(4, 1), "e\n  f\n  g\n")]);

        _scheduler.AdvanceBy(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, feature.FoldingModel!.Regions.Length);

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, feature.FoldingModel!.Regions.Length);
    }

    [Fact]
    public void FoldRegion_HidesItsLinesInTheViewModel()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("a\n  b\n  c\nd");
        Assert.Equal(4, context.ViewModel.GetLineCount());

        FoldingModel foldingModel = feature.FoldingModel!;
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);

        Assert.Equal(2, context.ViewModel.GetLineCount());
        Range hidden = Assert.Single(context.ViewModel.GetHiddenAreas());
        Assert.Equal((2, 3), (hidden.StartLineNumber, hidden.EndLineNumber));
    }

    [Fact]
    public void FoldRegion_CursorInside_MovesToTheHeaderLine()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("a\n  b\n  c\nd");
        context.Selection = new Selection(3, 2, 3, 2);

        FoldingModel foldingModel = feature.FoldingModel!;
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);

        Assert.Equal(new Selection(1, 2, 1, 2), context.Selection);
    }

    [Fact]
    public void CursorMovedIntoHiddenLines_UnfoldsAfter200Ms()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("a\n  b\n  c\nd");
        FoldingModel foldingModel = feature.FoldingModel!;
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);

        context.Selection = new Selection(3, 1, 3, 1);

        _scheduler.AdvanceBy(Delay - TimeSpan.FromMilliseconds(1));
        Assert.True(foldingModel.Regions.IsCollapsed(0));

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.False(foldingModel.Regions.IsCollapsed(0));
        Assert.Equal(4, context.ViewModel.GetLineCount());
    }

    [Fact]
    public void TypingAboveAFold_KeepsItFolded()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("x\na\n  b\n  c\nd");
        FoldingModel foldingModel = feature.FoldingModel!;
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(2)!]);

        context.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), "y\n")]);
        _scheduler.AdvanceBy(Delay);

        FoldingRegions regions = feature.FoldingModel!.Regions;
        Assert.Equal(1, regions.Length);
        Assert.Equal((3, 5, true), (regions.GetStartLineNumber(0), regions.GetEndLineNumber(0), regions.IsCollapsed(0)));
        Assert.Equal(4, context.ViewModel.GetLineCount());
    }

    [Fact]
    public void FoldingDisabled_NoRangesNoHiddenAreas()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("a\n  b\n  c\nd");
        FoldingModel foldingModel = feature.FoldingModel!;
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);
        Assert.Equal(2, context.ViewModel.GetLineCount());

        feature.SetOptions(EditorFoldingOptions.Default with { Folding = false });

        Assert.Null(feature.FoldingModel);
        Assert.Empty(context.ViewModel.GetHiddenAreas());
        Assert.Equal(4, context.ViewModel.GetLineCount());
        Assert.Equal(0, FoldingDecorationCount(context));
    }

    [Fact]
    public void MoreRangesThanTheLimit_ReportsTheLimit()
    {
        string text = string.Concat(Enumerable.Repeat("a\n  b\n", 12));
        (_, FoldingFeature feature) = Create(text);
        feature.SetOptions(EditorFoldingOptions.Default with { FoldingMaximumRegions = 10 });

        _scheduler.AdvanceBy(FirstDelay);

        Assert.Equal(10, feature.LimitReporter.Limited);
        Assert.Equal(12, feature.LimitReporter.Computed);
        Assert.Equal(10, feature.FoldingModel!.Regions.Length);
    }

    [Fact]
    public void Dispose_RemovesHiddenAreasAndDecorations()
    {
        (EditorContext context, FoldingFeature feature) = CreateComputed("a\n  b\n  c\nd");
        FoldingModel foldingModel = feature.FoldingModel!;
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);
        Assert.True(FoldingDecorationCount(context) > 0);

        feature.Dispose();

        Assert.Empty(context.ViewModel.GetHiddenAreas());
        Assert.Equal(4, context.ViewModel.GetLineCount());
        Assert.Equal(0, FoldingDecorationCount(context));
        Assert.Equal(0, _scheduler.PendingCount);
    }
}
