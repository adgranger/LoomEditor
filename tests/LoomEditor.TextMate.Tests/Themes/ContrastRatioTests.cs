using LoomEditor.Themes;
using LoomEditor.Themes.HighContrast;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests.
public class ContrastRatioTests
{
    private static double Ratio(string fg, string bg) => ContrastRatio.Between(ThemeColor.Parse(fg), ThemeColor.Parse(bg));

    [Fact]
    public void Between_BlackOnWhite_Is21() => Assert.Equal(21, Ratio("#000000", "#FFFFFF"), precision: 1);

    [Fact]
    public void Between_WhiteOnBlack_IsSymmetric() => Assert.Equal(Ratio("#000000", "#FFFFFF"), Ratio("#FFFFFF", "#000000"));

    [Fact]
    public void Between_SameColor_IsOne() => Assert.Equal(1, Ratio("#336699", "#336699"), precision: 6);

    [Fact]
    public void Between_TranslucentForeground_IsFlattenedOverTheBackground() =>
        Assert.InRange(Ratio("#FFFFFF80", "#000000"), Ratio("#808080", "#000000") - 0.05, Ratio("#808080", "#000000") + 0.05);

    [Fact]
    public void Between_BackgroundAlphaIsIgnored() => Assert.Equal(Ratio("#000000", "#FFFFFF"), Ratio("#000000", "#FFFFFF10"));

    [Fact]
    public void Between_KnownPair() => Assert.InRange(Ratio("#767676", "#FFFFFF"), 4.54 - 0.05, 4.54 + 0.05);

    [Fact]
    public void Constants_HaveTheWcagValues()
    {
        Assert.Equal(4.5, ContrastRatio.AaNormalText);
        Assert.Equal(7.0, ContrastRatio.AaaNormalText);
        Assert.Equal(3.0, ContrastRatio.NonText);
    }
}
