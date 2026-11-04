using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

public class FontVariationSettingsTests
{
    [Theory]
    [InlineData("'wght' 600", 600)]
    [InlineData("\"wdth\" 90, \"wght\" 450.7", 450)]
    public void TryGetWght_ValidSetting_ReturnsValue(string settings, int expected)
    {
        Assert.True(FontVariationSettings.TryGetWght(settings, out int value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("'wdth' 90")]
    public void TryGetWght_NoWght_ReturnsFalse(string settings)
    {
        Assert.False(FontVariationSettings.TryGetWght(settings, out int value));
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("'wght'")]
    [InlineData("'wght' abc")]
    [InlineData("'wghtX 600")]
    public void TryGetWght_Malformed_ReturnsFalse(string settings) => Assert.False(FontVariationSettings.TryGetWght(settings, out _));

    [Theory]
    [InlineData("normal", "", 400)]
    [InlineData("bold", "", 700)]
    [InlineData("300", "", 300)]
    [InlineData("1500", "", 999)]
    [InlineData("junk", "", 400)]
    [InlineData("normal", "'wght' 0", 1)]
    [InlineData("bold", "'wght' 560", 560)]
    public void ResolveBaseWeight_Setting_ReturnsClampedWeight(string fontWeight, string variationSettings, int expected)
    {
        BareFontInfo bare = new(1, "Consolas", fontWeight, 14, 19, 0, "", variationSettings);

        Assert.Equal(expected, FontVariationSettings.ResolveBaseWeight(bare));
    }
}
