// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (EditorMinimap defaults and validate)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class EditorMinimapOptionsTests
{
    [Fact]
    public void Defaults_MatchMonaco()
    {
        EditorMinimapOptions o = new();

        Assert.True(o.Enabled);
        Assert.Equal(MinimapSize.Proportional, o.Size);
        Assert.Equal(MinimapSide.Right, o.Side);
        Assert.Equal(MinimapShowSlider.MouseOver, o.ShowSlider);
        Assert.Equal(MinimapAutohide.None, o.Autohide);
        Assert.True(o.RenderCharacters);
        Assert.Equal(120, o.MaxColumn);
        Assert.Equal(1, o.Scale);
    }

    [Fact]
    public void Validated_ClampsMaxColumnAndScale()
    {
        Assert.Equal(1, new EditorMinimapOptions { MaxColumn = 0 }.Validated().MaxColumn);
        Assert.Equal(10000, new EditorMinimapOptions { MaxColumn = 99999 }.Validated().MaxColumn);
        Assert.Equal(80, new EditorMinimapOptions { MaxColumn = 80 }.Validated().MaxColumn);
        Assert.Equal(1, new EditorMinimapOptions { Scale = -4 }.Validated().Scale);
        Assert.Equal(3, new EditorMinimapOptions { Scale = 9 }.Validated().Scale);
        Assert.Equal(2, new EditorMinimapOptions { Scale = 2 }.Validated().Scale);
    }

    [Fact]
    public void EditorLayoutOptions_MinimapDisabledUntilDrawn()
    {
        Assert.False(new EditorLayoutOptions().Minimap.Enabled);
    }
}
