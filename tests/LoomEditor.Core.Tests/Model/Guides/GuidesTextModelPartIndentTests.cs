using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

namespace LoomEditor.Core.Tests.Model.Guides;

public class GuidesTextModelPartIndentTests
{
    private static TextModel Create(string text, bool offSide)
    {
        var test = TestLanguageModels.Create(
            text,
            TestLanguageModels.DefaultLanguageId,
            new LanguageConfiguration { Folding = new FoldingRules(OffSide: offSide) });
        test.Model.UpdateOptions(new TextModelUpdateOptions(TabSize: 2, IndentSize: 2));
        return test.Model;
    }

    private static int[] Guides(TextModel model) => model.Guides.GetLinesIndentGuides(1, model.LineCount);

    [Fact]
    public void GetLinesIndentGuides_IndentedLines_CountsLevels()
    {
        Assert.Equal([0, 1, 2], Guides(Create("a\n  b\n    c", offSide: false)));
    }

    [Fact]
    public void GetLinesIndentGuides_BlankLineInsideABlock_TakesTheBlockLevel()
    {
        Assert.Equal([0, 1, 1, 1], Guides(Create("a\n  b\n\n  c", offSide: false)));
        Assert.Equal([0, 1, 2, 2], Guides(Create("a\n  b\n\n    c", offSide: false)));
    }

    [Fact]
    public void GetLinesIndentGuides_BlankLineAtTheEnd_IsZero()
    {
        Assert.Equal([0, 1, 0], Guides(Create("a\n  b\n", offSide: false)));
    }

    [Fact]
    public void GetLinesIndentGuides_OffSideLanguage_BlankLineBeforeDedent_UsesTheLevelBelow()
    {
        const string Text = "a\n  b\n\nc";
        Assert.Equal([0, 1, 0, 0], Guides(Create(Text, offSide: true)));
        Assert.Equal([0, 1, 1, 0], Guides(Create(Text, offSide: false)));
    }

    [Fact]
    public void GetLinesIndentGuides_IndentNotAMultiple_RoundsUp()
    {
        Assert.Equal([2], Guides(Create("   a", offSide: false)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(1, 0)]
    public void GetLinesIndentGuides_InvalidRange_Throws(int start, int end)
    {
        var model = Create("a\nb", offSide: false);
        Assert.Throws<ArgumentOutOfRangeException>(() => model.Guides.GetLinesIndentGuides(start, end));
    }
}
