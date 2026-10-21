using LoomEditor.View.Features.Find;

namespace LoomEditor.View.Tests.Features.Find;

public class FindMatchesLabelTests
{
    [Fact]
    public void Format_NoMatches_NoResults()
    {
        Assert.Equal("No results", FindMatchesLabel.Format(0, 0));
    }

    [Fact]
    public void Format_PositionZero_QuestionMark()
    {
        Assert.Equal("? of 12", FindMatchesLabel.Format(0, 12));
    }

    [Fact]
    public void Format_AtLimit_AddsPlus()
    {
        Assert.Equal("5 of 19999+", FindMatchesLabel.Format(5, FindConstants.MatchesLimit));
    }

    [Fact]
    public void Format_Normal_NOfM()
    {
        Assert.Equal("3 of 12", FindMatchesLabel.Format(3, 12));
    }
}
