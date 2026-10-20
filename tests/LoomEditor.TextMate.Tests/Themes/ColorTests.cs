using LoomEditor.Themes;

namespace LoomEditor.TextMate.Tests.Themes;

// Ported from microsoft/vscode: src/vs/base/test/common/color.test.ts (suites 'Color' and 'HSLA').
// Not ported: blending, toString, toNumber32Bit, HSVA, Format, ensureContrastRatio (not needed).
public class ColorTests
{
    private static Color FromHex(string hex) => Color.FromThemeColor(ThemeColor.Parse(hex));

    private static readonly Color Color1 = new(new Hsla(60, 1, 0.5, 1));
    private static readonly Color Color2 = new(new Hsla(0, 0, 0.753, 1));

    [Fact]
    public void IsLighterColor()
    {
        // Monaco: 'isLighterColor'
        Assert.True(Color1.IsLighterThan(Color2));

        // Abyss theme
        Assert.True(FromHex("#770811").IsLighterThan(FromHex("#000c18")));
    }

    [Fact]
    public void GetLighterColor()
    {
        // Monaco: 'getLighterColor'
        Assert.Equal(Color1.Hsla, Color.GetLighterColor(Color1, Color2).Hsla);
        Assert.Equal(new Hsla(0, 0, 0.916, 1), Color.GetLighterColor(Color2, Color1).Hsla);
        Assert.Equal(new Hsla(0, 0, 0.851, 1), Color.GetLighterColor(Color2, Color1, 0.3).Hsla);
        Assert.Equal(new Hsla(0, 0, 0.981, 1), Color.GetLighterColor(Color2, Color1, 0.7).Hsla);
        Assert.Equal(new Hsla(0, 0, 1, 1), Color.GetLighterColor(Color2, Color1, 1).Hsla);
    }

    [Fact]
    public void IsDarkerColor()
    {
        // Monaco: 'isDarkerColor'
        Assert.True(Color2.IsDarkerThan(Color1));
    }

    [Fact]
    public void GetDarkerColor()
    {
        // Monaco: 'getDarkerColor'
        Assert.Equal(Color2.Hsla, Color.GetDarkerColor(Color2, Color1).Hsla);
        Assert.Equal(new Hsla(60, 1, 0.392, 1), Color.GetDarkerColor(Color1, Color2).Hsla);
        Assert.Equal(new Hsla(60, 1, 0.435, 1), Color.GetDarkerColor(Color1, Color2, 0.3).Hsla);
        Assert.Equal(new Hsla(60, 1, 0.349, 1), Color.GetDarkerColor(Color1, Color2, 0.7).Hsla);
        Assert.Equal(new Hsla(60, 1, 0.284, 1), Color.GetDarkerColor(Color1, Color2, 1).Hsla);

        // Abyss theme
        Assert.Equal(new Hsla(355, 0.874, 0.157, 1), Color.GetDarkerColor(FromHex("#770811"), FromHex("#000c18"), 0.4).Hsla);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 1)]
    [InlineData(255, 0, 0, 0.2126)]
    [InlineData(0, 255, 0, 0.7152)]
    [InlineData(0, 0, 255, 0.0722)]
    [InlineData(255, 255, 0, 0.9278)]
    [InlineData(0, 255, 255, 0.7874)]
    [InlineData(255, 0, 255, 0.2848)]
    [InlineData(192, 192, 192, 0.5271)]
    [InlineData(128, 128, 128, 0.2159)]
    [InlineData(128, 0, 0, 0.0459)]
    [InlineData(128, 128, 0, 0.2003)]
    [InlineData(0, 128, 0, 0.1544)]
    [InlineData(128, 0, 128, 0.0615)]
    [InlineData(0, 128, 128, 0.17)]
    [InlineData(0, 0, 128, 0.0156)]
    public void Luminance(int r, int g, int b, double expected)
    {
        // Monaco: 'luminance'
        Assert.Equal(expected, new Color(new Rgba(r, g, b, 1)).GetRelativeLuminance());
    }

    [Fact]
    public void Hsla_ToRgba()
    {
        // Monaco: 'HSLA.toRGBA'
        Assert.Equal(new Rgba(0, 0, 0, 0), Hsla.ToRgba(new Hsla(0, 0, 0, 0)));
        Assert.Equal(new Rgba(0, 0, 0, 1), Hsla.ToRgba(new Hsla(0, 0, 0, 1)));
        Assert.Equal(new Rgba(255, 255, 255, 1), Hsla.ToRgba(new Hsla(0, 0, 1, 1)));

        Assert.Equal(new Rgba(255, 0, 0, 1), Hsla.ToRgba(new Hsla(0, 1, 0.5, 1)));
        Assert.Equal(new Rgba(0, 255, 0, 1), Hsla.ToRgba(new Hsla(120, 1, 0.5, 1)));
        Assert.Equal(new Rgba(0, 0, 255, 1), Hsla.ToRgba(new Hsla(240, 1, 0.5, 1)));

        Assert.Equal(new Rgba(255, 255, 0, 1), Hsla.ToRgba(new Hsla(60, 1, 0.5, 1)));
        Assert.Equal(new Rgba(0, 255, 255, 1), Hsla.ToRgba(new Hsla(180, 1, 0.5, 1)));
        Assert.Equal(new Rgba(255, 0, 255, 1), Hsla.ToRgba(new Hsla(300, 1, 0.5, 1)));

        Assert.Equal(new Rgba(192, 192, 192, 1), Hsla.ToRgba(new Hsla(0, 0, 0.753, 1)));

        Assert.Equal(new Rgba(128, 128, 128, 1), Hsla.ToRgba(new Hsla(0, 0, 0.502, 1)));
        Assert.Equal(new Rgba(128, 0, 0, 1), Hsla.ToRgba(new Hsla(0, 1, 0.251, 1)));
        Assert.Equal(new Rgba(128, 128, 0, 1), Hsla.ToRgba(new Hsla(60, 1, 0.251, 1)));
        Assert.Equal(new Rgba(0, 128, 0, 1), Hsla.ToRgba(new Hsla(120, 1, 0.251, 1)));
        Assert.Equal(new Rgba(128, 0, 128, 1), Hsla.ToRgba(new Hsla(300, 1, 0.251, 1)));
        Assert.Equal(new Rgba(0, 128, 128, 1), Hsla.ToRgba(new Hsla(180, 1, 0.251, 1)));
        Assert.Equal(new Rgba(0, 0, 128, 1), Hsla.ToRgba(new Hsla(240, 1, 0.251, 1)));
    }

    [Fact]
    public void Hsla_FromRgba()
    {
        // Monaco: 'HSLA.fromRGBA'
        Assert.Equal(new Hsla(0, 0, 0, 0), Hsla.FromRgba(new Rgba(0, 0, 0, 0)));
        Assert.Equal(new Hsla(0, 0, 0, 1), Hsla.FromRgba(new Rgba(0, 0, 0, 1)));
        Assert.Equal(new Hsla(0, 0, 1, 1), Hsla.FromRgba(new Rgba(255, 255, 255, 1)));

        Assert.Equal(new Hsla(0, 1, 0.5, 1), Hsla.FromRgba(new Rgba(255, 0, 0, 1)));
        Assert.Equal(new Hsla(120, 1, 0.5, 1), Hsla.FromRgba(new Rgba(0, 255, 0, 1)));
        Assert.Equal(new Hsla(240, 1, 0.5, 1), Hsla.FromRgba(new Rgba(0, 0, 255, 1)));

        Assert.Equal(new Hsla(60, 1, 0.5, 1), Hsla.FromRgba(new Rgba(255, 255, 0, 1)));
        Assert.Equal(new Hsla(180, 1, 0.5, 1), Hsla.FromRgba(new Rgba(0, 255, 255, 1)));
        Assert.Equal(new Hsla(300, 1, 0.5, 1), Hsla.FromRgba(new Rgba(255, 0, 255, 1)));

        Assert.Equal(new Hsla(0, 0, 0.753, 1), Hsla.FromRgba(new Rgba(192, 192, 192, 1)));

        Assert.Equal(new Hsla(0, 0, 0.502, 1), Hsla.FromRgba(new Rgba(128, 128, 128, 1)));
        Assert.Equal(new Hsla(0, 1, 0.251, 1), Hsla.FromRgba(new Rgba(128, 0, 0, 1)));
        Assert.Equal(new Hsla(60, 1, 0.251, 1), Hsla.FromRgba(new Rgba(128, 128, 0, 1)));
        Assert.Equal(new Hsla(120, 1, 0.251, 1), Hsla.FromRgba(new Rgba(0, 128, 0, 1)));
        Assert.Equal(new Hsla(300, 1, 0.251, 1), Hsla.FromRgba(new Rgba(128, 0, 128, 1)));
        Assert.Equal(new Hsla(180, 1, 0.251, 1), Hsla.FromRgba(new Rgba(0, 128, 128, 1)));
        Assert.Equal(new Hsla(240, 1, 0.251, 1), Hsla.FromRgba(new Rgba(0, 0, 128, 1)));
    }
}
