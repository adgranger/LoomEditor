using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Monaco hands the string to the browser: all tests are new.
public class FontFeatureSettingsTests
{
    [Fact]
    public void Parse_OnAndZero_GivesTwoItemsInOrder()
    {
        var result = FontFeatureSettings.Parse("'liga' on, \"calt\" 0");

        Assert.Equal(new[] { new FontFeature("liga", 1), new FontFeature("calt", 0) }, result);
    }

    [Fact]
    public void Parse_TagAlone_DefaultsToOne()
    {
        Assert.Equal(new[] { new FontFeature("ss01", 1) }, FontFeatureSettings.Parse("'ss01'"));
    }

    [Fact]
    public void Parse_OffAndInteger_AreParsed()
    {
        Assert.Equal(new[] { new FontFeature("liga", 0), new FontFeature("salt", 3) }, FontFeatureSettings.Parse("\"liga\" off, 'salt' 3"));
    }

    [Theory]
    [InlineData("'abc'")]
    [InlineData("'liga' -1")]
    [InlineData("'liga")]
    [InlineData("liga")]
    [InlineData("'liga' maybe")]
    [InlineData("normal")]
    [InlineData("")]
    public void Parse_MalformedOrEmpty_GivesEmpty(string input)
    {
        Assert.Empty(FontFeatureSettings.Parse(input));
    }

    [Fact]
    public void Parse_MalformedItemIsSkipped_OthersKept()
    {
        Assert.Equal(new[] { new FontFeature("calt", 1) }, FontFeatureSettings.Parse("'abc', 'calt'"));
    }

    [Fact]
    public void Parse_Null_GivesEmpty()
    {
        Assert.Empty(FontFeatureSettings.Parse(null));
    }
}
