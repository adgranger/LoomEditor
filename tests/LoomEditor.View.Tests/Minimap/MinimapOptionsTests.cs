using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

public class MinimapOptionsTests
{
    private static MinimapOptions Create(EditorTheme theme, int height = 600, double lineHeight = 2)
    {
        var layout = new EditorLayoutInfo(
            Width: 800, Height: height, GlyphMarginLeft: 0, GlyphMarginWidth: 0, GlyphMarginDecorationLaneCount: 1,
            LineNumbersLeft: 0, LineNumbersWidth: 42, DecorationsLeft: 42, DecorationsWidth: 10, ContentLeft: 52, ContentWidth: 634,
            Minimap: new EditorMinimapLayoutInfo(RenderMinimap.Text, 686, 100, false, false, 1, lineHeight, 100, height, 100, height),
            ViewportColumn: 90, IsViewportWrapping: false, WrappingColumn: -1, VerticalScrollbarWidth: 14,
            HorizontalScrollbarHeight: 12, OverviewRuler: new OverviewRulerPosition(14, height, 0, 0));
        return new MinimapOptions(
            layout, new EditorMinimapOptions(), FontInfo.Default, false, theme, new MinimapTokensColorTracker(theme),
            (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
    }

    [Fact]
    public void MinimapOptions_NoMinimapBackground_UsesTheDefaultBackground()
    {
        MinimapOptions options = Create(EditorTheme.Dark);

        Assert.Equal(options.DefaultBackgroundColor, options.BackgroundColor);
        Assert.NotEqual(Rgba8.Empty, options.BackgroundColor);
    }

    [Fact]
    public void MinimapOptions_ForegroundOpacity_FromTheTheme()
    {
        // minimap.foregroundOpacity defaults to #000f: fully opaque.
        Assert.Equal(255, Create(EditorTheme.Dark).ForegroundAlpha);
    }

    [Fact]
    public void MinimapOptions_EqualsOptions_DetectsAChange()
    {
        MinimapOptions a = Create(EditorTheme.Dark);

        Assert.True(a.EqualsOptions(Create(EditorTheme.Dark)));
        Assert.False(a.EqualsOptions(Create(EditorTheme.Dark, height: 601)));
        Assert.False(a.EqualsOptions(Create(EditorTheme.Light)));
    }

    [Fact]
    public void TokensColorTracker_BackgroundIsLight_ForTheLightTheme()
    {
        Assert.True(new MinimapTokensColorTracker(EditorTheme.Light).BackgroundIsLight());
        Assert.False(new MinimapTokensColorTracker(EditorTheme.Dark).BackgroundIsLight());
    }
}
