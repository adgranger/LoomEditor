using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no common test for these options: all tests are new.
public class EditorFontOptionsTests
{
    [Theory]
    [InlineData(null, EditorFontOptions.LigaturesOff)]
    [InlineData("", EditorFontOptions.LigaturesOff)]
    [InlineData("false", EditorFontOptions.LigaturesOff)]
    [InlineData("true", EditorFontOptions.LigaturesOn)]
    [InlineData("'ss01' 1", "'ss01' 1")]
    public void ValidateLigatures_Input_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, EditorFontOptions.ValidateLigatures(input));
    }

    [Theory]
    [InlineData(null, EditorFontOptions.VariationsOff)]
    [InlineData("false", EditorFontOptions.VariationsOff)]
    [InlineData("true", EditorFontOptions.VariationsTranslate)]
    [InlineData("'wght' 500", "'wght' 500")]
    public void ValidateVariations_Input_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, EditorFontOptions.ValidateVariations(input));
    }

    [Theory]
    [InlineData("normal", "normal")]
    [InlineData("bold", "bold")]
    [InlineData("700", "700")]
    [InlineData("0", "1")]
    [InlineData("2000", "1000")]
    [InlineData("abc", "normal")]
    [InlineData(null, "normal")]
    public void ValidateWeight_Input_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, EditorFontOptions.ValidateWeight(input));
    }

    [Fact]
    public void Default_IsNormalWithLigaturesOff()
    {
        Assert.Equal(new EditorFontOptions("normal", 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff), EditorFontOptions.Default);
    }
}
