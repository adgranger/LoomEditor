using LoomEditor.Core.Model.Guides;

namespace LoomEditor.Core.Tests.Model.Guides;

public class IndentGuideTests
{
    [Fact]
    public void IndentGuide_BothColumnsSet_Throws()
    {
        Assert.Throws<ArgumentException>(() => new IndentGuide(3, 2, "c", null, -1, -1));
    }

    [Fact]
    public void IndentGuide_NeitherColumnSet_Throws()
    {
        Assert.Throws<ArgumentException>(() => new IndentGuide(-1, -1, "c", null, -1, -1));
    }

    [Fact]
    public void IndentGuide_OneColumnSet_IsCreated()
    {
        IndentGuide guide = new(-1, 2, "c", new IndentGuideHorizontalLine(true, 5), 1, 4);

        Assert.Equal(-1, guide.VisibleColumn);
        Assert.Equal(2, guide.Column);
        Assert.Equal("c", guide.ClassName);
        Assert.Equal(new IndentGuideHorizontalLine(true, 5), guide.HorizontalLine);
        Assert.Equal(1, guide.ForWrappedLinesAfterColumn);
        Assert.Equal(4, guide.ForWrappedLinesBeforeOrAtColumn);
    }

    [Fact]
    public void GetInlineClassNameOfLevel_WrapsAt30()
    {
        Assert.Equal("bracket-indent-guide lvl-0", BracketPairGuidesClassNames.GetInlineClassNameOfLevel(0));
        Assert.Equal("bracket-indent-guide lvl-29", BracketPairGuidesClassNames.GetInlineClassNameOfLevel(29));
        Assert.Equal("bracket-indent-guide lvl-0", BracketPairGuidesClassNames.GetInlineClassNameOfLevel(30));
        Assert.Equal("bracket-indent-guide lvl-1", BracketPairGuidesClassNames.GetInlineClassNameOfLevel(31));
    }

    [Fact]
    public void GetInlineClassName_IndependentPool_UsesTheEqualTypeLevel()
    {
        Assert.Equal("bracket-indent-guide lvl-2", BracketPairGuidesClassNames.GetInlineClassName(5, 2, true));
        Assert.Equal("bracket-indent-guide lvl-5", BracketPairGuidesClassNames.GetInlineClassName(5, 2, false));
    }
}
