using LoomEditor.Rendering.Win2D;
using Microsoft.Graphics.Canvas.Text;

namespace LoomEditor.Rendering.Tests;

// Not ported from Monaco: DirectWrite specific.
public class FontFeatureTagsTests
{
    [Fact]
    public void Pack_Liga_EqualsStandardLigatures()
    {
        Assert.Equal((uint)CanvasTypographyFeatureName.StandardLigatures, FontFeatureTags.Pack("liga"));
    }

    [Fact]
    public void Pack_Calt_EqualsContextualAlternates()
    {
        Assert.Equal((uint)CanvasTypographyFeatureName.ContextualAlternates, FontFeatureTags.Pack("calt"));
    }

    [Fact]
    public void Pack_Ss01_EqualsStylisticSet1()
    {
        Assert.Equal((uint)CanvasTypographyFeatureName.StylisticSet1, FontFeatureTags.Pack("ss01"));
    }

    [Theory]
    [InlineData("lig")]
    [InlineData("ligas")]
    [InlineData("")]
    public void Pack_NotFourCharacters_Throws(string tag)
    {
        Assert.Throws<ArgumentException>(() => FontFeatureTags.Pack(tag));
    }

    [Fact]
    public void Pack_NonAscii_Throws()
    {
        Assert.Throws<ArgumentException>(() => FontFeatureTags.Pack("ligé"));
    }
}
