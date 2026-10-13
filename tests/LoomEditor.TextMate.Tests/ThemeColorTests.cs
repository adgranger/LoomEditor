using LoomEditor.Themes;

namespace LoomEditor.TextMate.Tests;

// LoomEditor.Themes is small enough to share this test project with TextMate (both are phase 4).
public class ThemeColorTests
{
    [Theory]
    [InlineData("#1F1F1F", 255, 0x1F, 0x1F, 0x1F)]
    [InlineData("#abc", 255, 0xAA, 0xBB, 0xCC)]
    [InlineData("#11223380", 0x80, 0x11, 0x22, 0x33)]
    public void Parse_ValidColors(string text, int a, int r, int g, int b)
    {
        Assert.Equal(new ThemeColor((byte)a, (byte)r, (byte)g, (byte)b), ThemeColor.Parse(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void Parse_InvalidColors_Throws(string text)
    {
        Assert.Throws<FormatException>(() => ThemeColor.Parse(text));
    }

    [Fact]
    public void ToString_RoundTrips()
    {
        var color = new ThemeColor(0x80, 0x11, 0x22, 0x33);

        Assert.Equal(color, ThemeColor.Parse(color.ToString()));
    }
}
