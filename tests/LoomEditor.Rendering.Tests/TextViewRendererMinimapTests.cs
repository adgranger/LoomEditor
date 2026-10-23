using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the minimap drawing (it draws on canvases): all tests are new.
public class TextViewRendererMinimapTests
{
    private static ViewHarness CreateHarness(EditorMinimapOptions? options = null, int lineCount = 200)
    {
        var harness = new ViewHarness(string.Join('\n', Enumerable.Repeat("some text", lineCount)));
        // The harness layout has no minimap by default.
        harness.ViewModel.SetOptions(
            harness.ViewModel.LayoutOptions with { Minimap = options ?? new EditorMinimapOptions() }, harness.ViewModel.ViewLayoutOptions);
        return harness;
    }

    private static ThemeColor SliderColor(string id) => EditorTheme.Dark.GetColor(id)!.Value;

    private static List<FillRectangleCall> Fills(RecordingRenderSurface surface, ThemeColor color) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == color)];

    [Fact]
    public void Minimap_Disabled_DrawsNothing()
    {
        using ViewHarness harness = CreateHarness(new EditorMinimapOptions { Enabled = false });

        RecordingRenderSurface surface = harness.Render();

        Assert.Empty(surface.Calls.OfType<DrawImageCall>());
    }

    [Fact]
    public void Minimap_Enabled_DrawsTwoImagesAtTheLayoutPosition()
    {
        using ViewHarness harness = CreateHarness();

        RecordingRenderSurface surface = harness.Render();

        EditorMinimapLayoutInfo minimap = harness.LayoutInfo.Minimap;
        List<DrawImageCall> images = [.. surface.Calls.OfType<DrawImageCall>()];
        Assert.Equal(2, images.Count);
        Assert.All(images, image =>
        {
            Assert.Equal(minimap.MinimapLeft, image.X);
            Assert.Equal(0, image.Y);
            Assert.Equal((float)minimap.MinimapCanvasOuterWidth, image.Width);
            Assert.Equal((float)minimap.MinimapCanvasOuterHeight, image.Height);
            Assert.Equal(minimap.MinimapCanvasInnerWidth, image.PixelWidth);
            Assert.Equal(0.9f, image.Opacity);
        });
    }

    [Fact]
    public void Minimap_RepaintedOnlyWhenChanged_KeepsTheImageVersion()
    {
        using ViewHarness harness = CreateHarness();

        int first = harness.Render().Calls.OfType<DrawImageCall>().First().Version;
        int second = harness.Render().Calls.OfType<DrawImageCall>().First().Version;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Slider_AlwaysMode_IsDrawn()
    {
        using ViewHarness harness = CreateHarness(new EditorMinimapOptions { ShowSlider = MinimapShowSlider.Always });

        RecordingRenderSurface surface = harness.Render();

        FillRectangleCall slider = Assert.Single(Fills(surface, SliderColor("minimapSlider.background")));
        Assert.Equal(harness.LayoutInfo.Minimap.MinimapLeft, slider.X);
        Assert.Equal(harness.LayoutInfo.Minimap.MinimapWidth, slider.Width);
    }

    [Fact]
    public void Slider_MouseOverMode_HiddenUntilHovered()
    {
        using ViewHarness harness = CreateHarness();
        ThemeColor color = SliderColor("minimapSlider.background");

        Assert.Empty(Fills(harness.Render(), color));

        harness.Renderer.MinimapPresentation.MouseOverMinimap = true;
        Assert.Single(Fills(harness.Render(), color));
    }

    [Fact]
    public void Slider_Hovered_UsesTheHoverColor()
    {
        using ViewHarness harness = CreateHarness(new EditorMinimapOptions { ShowSlider = MinimapShowSlider.Always });
        harness.Renderer.MinimapPresentation.SliderHovered = true;

        RecordingRenderSurface surface = harness.Render();

        Assert.Single(Fills(surface, SliderColor("minimapSlider.hoverBackground")));
    }

    [Fact]
    public void Shadow_DrawnOnlyWhenHorizontallyScrollable()
    {
        using ViewHarness harness = CreateHarness();
        ThemeColor shadow = EditorTheme.Dark.GetColor("scrollbar.shadow")!.Value;

        // The content fits horizontally.
        Assert.Empty(Fills(harness.Render(), shadow));

        // A line wider than the viewport.
        harness.ViewModel.ViewLayout.SetMaxLineWidth(5000);
        FillRectangleCall band = Assert.Single(Fills(harness.Render(), shadow));
        Assert.Equal(harness.LayoutInfo.Minimap.MinimapLeft - 6, band.X);
        Assert.Equal(6, band.Width);
    }

    [Fact]
    public void Autohide_MouseOver_DrawsNothingUntilHovered()
    {
        using ViewHarness harness = CreateHarness(new EditorMinimapOptions { Autohide = MinimapAutohide.MouseOver });

        Assert.Empty(harness.Render().Calls.OfType<DrawImageCall>());

        harness.Renderer.MinimapPresentation.MouseOverMinimap = true;
        Assert.Equal(2, harness.Render().Calls.OfType<DrawImageCall>().Count());
    }

    [Fact]
    public void Autohide_Scroll_ShowsTheMinimapAfterAScroll()
    {
        using ViewHarness harness = CreateHarness(new EditorMinimapOptions { Autohide = MinimapAutohide.Scroll });
        Assert.Empty(harness.Render().Calls.OfType<DrawImageCall>());

        harness.ScrollTo(scrollTop: 100);

        Assert.Equal(2, harness.Render().Calls.OfType<DrawImageCall>().Count());
    }
}
