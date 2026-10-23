using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class EditorWrappingOptionsTests
{
    [Fact]
    public void Defaults_MatchMonaco()
    {
        var o = new EditorWrappingOptions();

        Assert.Equal(WordWrap.Off, o.WordWrap);
        Assert.Equal(80, o.WordWrapColumn);
        Assert.False(o.WordWrapIndicator);
        Assert.Equal(WrappingIndent.Same, o.WrappingIndent);
        Assert.Equal(WordBreak.Normal, o.WordBreak);
        Assert.False(o.WrapOnEscapedLineFeeds);
        Assert.False(o.UseTwoCellFullwidthCharacters);
        Assert.NotEmpty(o.WordWrapBreakBeforeCharacters);
        Assert.NotEmpty(o.WordWrapBreakAfterCharacters);
        Assert.Contains('(', o.WordWrapBreakBeforeCharacters);
        Assert.Contains(')', o.WordWrapBreakAfterCharacters);
    }

    [Theory]
    [InlineData("none", "None")]
    [InlineData("same", "Same")]
    [InlineData("indent", "Indent")]
    [InlineData("deepIndent", "DeepIndent")]
    [InlineData("INDENT", "Same")]
    [InlineData(null, "Same")]
    public void FromString_KnownValues_ParseAndUnknownFallsBack_WrappingIndent(string? s, string expected)
    {
        Assert.Equal(expected, WrappingIndents.FromString(s).ToString());
    }

    [Theory]
    [InlineData("off", "Off")]
    [InlineData("on", "On")]
    [InlineData("wordWrapColumn", "WordWrapColumn")]
    [InlineData("bounded", "Bounded")]
    [InlineData("ON", "Off")]
    [InlineData(null, "Off")]
    public void FromString_KnownValues_ParseAndUnknownFallsBack_WordWrap(string? s, string expected)
    {
        Assert.Equal(expected, WordWraps.FromString(s).ToString());
    }

    [Theory]
    [InlineData("normal", "Normal")]
    [InlineData("keepAll", "KeepAll")]
    [InlineData("KEEPALL", "Normal")]
    [InlineData(null, "Normal")]
    public void FromString_KnownValues_ParseAndUnknownFallsBack_WordBreak(string? s, string expected)
    {
        Assert.Equal(expected, WordBreaks.FromString(s).ToString());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(120, 120)]
    public void Validated_WordWrapColumnBelowOne_IsClampedToOne(int input, int expected)
    {
        Assert.Equal(expected, (new EditorWrappingOptions { WordWrapColumn = input }).Validated().WordWrapColumn);
    }
}
