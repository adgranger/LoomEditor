using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no common test for fontInfo.ts: all tests are new.
public class FontInfoTests
{
    [Theory]
    [InlineData(14, 0, 19)]
    [InlineData(14, 1.5, 21)]
    [InlineData(14, 22, 22)]
    [InlineData(4, 0, 8)]
    public void Create_LineHeightRule_ReturnsExpectedPixels(double fontSize, double lineHeight, int expected)
    {
        BareFontInfo info = BareFontInfo.Create("Consolas", "normal", fontSize, lineHeight, 0, 1);

        Assert.Equal(expected, info.LineHeight);
    }

    [Fact]
    public void Create_KeepsRequestedValues()
    {
        BareFontInfo info = BareFontInfo.Create("Consolas", "bold", 14, 0, 0.5, 2);

        Assert.Equal("Consolas", info.FontFamily);
        Assert.Equal("bold", info.FontWeight);
        Assert.Equal(14, info.FontSize);
        Assert.Equal(0.5, info.LetterSpacing);
        Assert.Equal(2, info.PixelRatio);
    }

    [Fact]
    public void Constants_UseWindowsValues()
    {
        Assert.Equal(1.35, BareFontInfo.GoldenLineHeightRatio);
        Assert.Equal(8, BareFontInfo.MinimumLineHeight);
        Assert.Equal("Consolas, 'Courier New', monospace", BareFontInfo.DefaultWindowsFontFamily);
    }
}
