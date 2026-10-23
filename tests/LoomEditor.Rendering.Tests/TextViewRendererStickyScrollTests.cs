using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.StickyScroll;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the sticky scroll widget (it renders HTML): all tests are new.
public class TextViewRendererStickyScrollTests
{
    private const string Text = "L1\nL2\nL3\nL4\nL5\nL6\nL7\nL8";

    private static void SetSticky(ViewHarness harness, int lastLineRelativePosition, params int[] lineNumbers) =>
        harness.ViewModel.StickyScroll = new StickyScrollRenderData(lineNumbers, lastLineRelativePosition);

    private static bool HasText(RecordingRenderSurface surface, string text, float top) =>
        surface.Calls.OfType<DrawLineLayoutCall>().Any(call => call.Text == text && call.LineTop == top);

    private static bool HasFill(RecordingRenderSurface surface, float top, ThemeColor color) =>
        surface.Calls.OfType<FillRectangleCall>().Any(call => call.Y == top && call.Color == color && call.Height == 20);

    [Fact]
    public void StickyScroll_Null_DrawsNothing()
    {
        using var harness = new ViewHarness(Text);

        RecordingRenderSurface surface = harness.Render();

        Assert.False(harness.Renderer.IsInStickyScroll(5));
        Assert.Equal(-1, harness.Renderer.GetStickyLineAt(5));
        Assert.Equal(1, surface.Calls.OfType<DrawLineLayoutCall>().Count(call => call.Text == "L3"));
    }

    [Fact]
    public void StickyScroll_TwoLines_DrawsBackgroundsNumbersAndTexts()
    {
        using var harness = new ViewHarness(Text);
        SetSticky(harness, 0, 3, 4);

        RecordingRenderSurface surface = harness.Render();

        ThemeColor background = harness.Renderer.Theme.Background;
        Assert.True(HasFill(surface, 0, background));
        Assert.True(HasFill(surface, 20, background));
        Assert.True(HasText(surface, "L3", 0));
        Assert.True(HasText(surface, "L4", 20));
        Assert.True(HasText(surface, "3", 0));
        Assert.True(HasText(surface, "4", 20));
        Assert.True(harness.Renderer.IsInStickyScroll(39));
        Assert.False(harness.Renderer.IsInStickyScroll(40));
        // The shadow sits under the widget.
        Assert.Contains(surface.Calls.OfType<FillRectangleCall>(), call => call.Y == 40 && call.Height == 2);
    }

    [Fact]
    public void StickyScroll_LastLinePushedUp_IsDrawnHigher()
    {
        using var harness = new ViewHarness(Text);
        SetSticky(harness, -10, 3, 4);

        RecordingRenderSurface surface = harness.Render();

        Assert.True(HasText(surface, "L3", 0));
        Assert.True(HasText(surface, "L4", 10));
        Assert.Contains(surface.Calls.OfType<FillRectangleCall>(), call => call.Y == 30 && call.Height == 2);
    }

    [Fact]
    public void StickyScroll_CollapsedScope_DrawsTheChevron()
    {
        using var harness = new ViewHarness(Text);
        var foldingModel = new FoldingModel(harness.Model, new FoldingDecorationProvider(harness.ViewModel));
        foldingModel.Update(FoldingRegions.FromFoldRanges((FoldRange[])
            [new FoldRange { StartLineNumber = 2, EndLineNumber = 4, Source = FoldSource.Provider }]));
        foldingModel.ToggleCollapseState(new[] { foldingModel.GetRegionAtLine(2)! });
        SetSticky(harness, 0, 2);

        RecordingRenderSurface surface = harness.Render();

        // A collapsed last line is drawn one DIP lower; the chevron is centered in it.
        ThemeColor color = harness.Renderer.Theme.FoldingControlForeground!.Value;
        float cx = (harness.LayoutInfo.DecorationsLeft + 2 + harness.LayoutInfo.DecorationsLeft + harness.LayoutInfo.DecorationsWidth) / 2f;
        Assert.Contains(new DrawLineCall(cx - 2, 7, cx + 2, 11, color, 1.5f), surface.Calls.OfType<DrawLineCall>());
    }

    [Fact]
    public void StickyScroll_Hover_UsesTheHoverBackground()
    {
        using var harness = new ViewHarness(Text);
        SetSticky(harness, 0, 3, 4);
        harness.Renderer.HoveredStickyLineIndex = 1;

        RecordingRenderSurface surface = harness.Render();

        ThemeColor hover = harness.Renderer.Theme.GetColor("editorStickyScrollHover.background")!.Value;
        Assert.True(HasFill(surface, 20, hover));
        Assert.False(HasFill(surface, 0, hover));
    }

    [Fact]
    public void GetStickyLineAt_ReturnsTheIndex()
    {
        using var harness = new ViewHarness(Text);
        SetSticky(harness, -10, 3, 4);
        harness.Render();

        Assert.Equal(0, harness.Renderer.GetStickyLineAt(5));
        Assert.Equal(0, harness.Renderer.GetStickyLineAt(15));
        Assert.Equal(1, harness.Renderer.GetStickyLineAt(25));
        Assert.Equal(-1, harness.Renderer.GetStickyLineAt(35));
        Assert.False(harness.Renderer.IsInStickyScroll(30));
    }
}
