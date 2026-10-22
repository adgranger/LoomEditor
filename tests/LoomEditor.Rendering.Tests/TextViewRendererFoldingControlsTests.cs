using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the folding controls (CSS codicons): all tests are new.
public class TextViewRendererFoldingControlsTests
{
    private const string Text = "L1\nL2\nL3\nL4\nL5\nL6";

    private static FoldingModel AddRegion(ViewHarness harness, ShowFoldingControls controls, bool collapse)
    {
        var provider = new FoldingDecorationProvider(harness.ViewModel) { ShowFoldingControls = controls };
        var foldingModel = new FoldingModel(harness.Model, provider);
        foldingModel.Update(FoldingRegions.FromFoldRanges((FoldRange[])
            [new FoldRange { StartLineNumber = 2, EndLineNumber = 4, Source = FoldSource.Provider }]));
        if (collapse)
        {
            foldingModel.ToggleCollapseState(new[] { foldingModel.GetRegionAtLine(2)! });
        }

        return foldingModel;
    }

    private static (float Cx, float Cy) ChevronCenter(ViewHarness harness, int viewLine)
    {
        EditorLayoutInfo layout = harness.LayoutInfo;
        float cx = (layout.DecorationsLeft + 2 + layout.DecorationsLeft + layout.DecorationsWidth) / 2f;
        return (cx, (viewLine - 1) * 20 + 10);
    }

    [Fact]
    public void Layout_FoldingOn_AddsSixteenDipsToTheDecorationsWidth()
    {
        using var harness = new ViewHarness(Text);

        Assert.Equal(26, harness.LayoutInfo.DecorationsWidth);
    }

    [Fact]
    public void ExpandedRegion_MarginNotHovered_DrawsNoChevron()
    {
        using var harness = new ViewHarness(Text);
        AddRegion(harness, ShowFoldingControls.Mouseover, collapse: false);

        Assert.Empty(harness.Render().Calls.OfType<DrawLineCall>());
    }

    [Fact]
    public void ExpandedRegion_MarginHovered_DrawsADownChevron()
    {
        using var harness = new ViewHarness(Text);
        AddRegion(harness, ShowFoldingControls.Mouseover, collapse: false);
        harness.Renderer.IsMarginHovered = true;

        List<DrawLineCall> lines = [.. harness.Render().Calls.OfType<DrawLineCall>()];

        (float cx, float cy) = ChevronCenter(harness, 2);
        Assert.Equal(
            [new DrawLineCall(cx - 4, cy - 2, cx, cy + 2, harness.Renderer.Theme.FoldingControlForeground!.Value, 1.5f),
             new DrawLineCall(cx, cy + 2, cx + 4, cy - 2, harness.Renderer.Theme.FoldingControlForeground!.Value, 1.5f)],
            lines);
    }

    [Fact]
    public void CollapsedRegion_AlwaysDrawsARightChevron()
    {
        using var harness = new ViewHarness(Text);
        AddRegion(harness, ShowFoldingControls.Mouseover, collapse: true);

        List<DrawLineCall> lines = [.. harness.Render().Calls.OfType<DrawLineCall>()];

        (float cx, float cy) = ChevronCenter(harness, 2);
        Assert.Equal(
            [new DrawLineCall(cx - 2, cy - 4, cx + 2, cy, harness.Renderer.Theme.FoldingControlForeground!.Value, 1.5f),
             new DrawLineCall(cx + 2, cy, cx - 2, cy + 4, harness.Renderer.Theme.FoldingControlForeground!.Value, 1.5f)],
            lines);
    }

    [Fact]
    public void ShowFoldingControlsAlways_ExpandedRegion_DrawsWithoutHover()
    {
        using var harness = new ViewHarness(Text);
        AddRegion(harness, ShowFoldingControls.Always, collapse: false);

        Assert.Equal(2, harness.Render().Calls.OfType<DrawLineCall>().Count());
    }

    [Fact]
    public void ShowFoldingControlsNever_DrawsNothingAndNoRoom()
    {
        using var harness = new ViewHarness(Text);
        harness.ViewModel.SetOptions(
            harness.ViewModel.LayoutOptions with { ShowFoldingDecoration = false }, harness.ViewModel.ViewLayoutOptions);
        AddRegion(harness, ShowFoldingControls.Never, collapse: false);
        harness.Renderer.IsMarginHovered = true;

        Assert.Equal(10, harness.LayoutInfo.DecorationsWidth);
        Assert.Empty(harness.Render().Calls.OfType<DrawLineCall>());
    }

    [Fact]
    public void RegionOnHiddenLine_DrawsNothing()
    {
        using var harness = new ViewHarness(Text);
        AddRegion(harness, ShowFoldingControls.Always, collapse: false);
        harness.ViewModel.SetHiddenAreas(new[] { new Range(2, 1, 4, 1) });

        Assert.Empty(harness.Render().Calls.OfType<DrawLineCall>());
    }
}
