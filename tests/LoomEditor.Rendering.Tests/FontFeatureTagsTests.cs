
namespace LoomEditor.Rendering.Tests;

// Not ported from Monaco: DirectWrite specific.
public class FontFeatureTagsTests
{
    [Fact]
    public void Pack_Liga_EqualsStandardLigatures()
    {
        Assert.Equal(0x6167696Cu, FontFeatureTags.Pack("liga"));
    }

    [Fact]
    public void Pack_Calt_EqualsContextualAlternates()
    {
        Assert.Equal(0x746C6163u, FontFeatureTags.Pack("calt"));
    }

    [Fact]
    public void Pack_Ss01_EqualsStylisticSet1()
    {
        Assert.Equal(0x31307373u, FontFeatureTags.Pack("ss01"));
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
