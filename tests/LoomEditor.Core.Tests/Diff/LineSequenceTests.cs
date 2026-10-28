using LoomEditor.Core.Diff;
using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class LineSequenceTests
{
    private static readonly string[] Lines = ["a", "  b", "\t\tc", "d"];

    private static LineSequence Make() => new([1, 2, 3, 4], Lines);

    // Monaco: LineSequence.getBoundaryScore
    [Theory]
    [InlineData(0, 1000 - 0)]
    [InlineData(1, 1000 - 2)]
    [InlineData(2, 1000 - 4)]
    [InlineData(3, 1000 - 2)]
    [InlineData(4, 1000 - 0)]
    public void BoundaryScore_IndentationBeforeAndAfter_LowersTheScore(int length, int expected)
    {
        Assert.Equal(expected, Make().GetBoundaryScore(length));
    }

    // Monaco: LineSequence.isStronglyEqual
    [Fact]
    public void IsStronglyEqual_ComparesUntrimmedLines()
    {
        LineSequence s = new([7, 7, 7], ["x", " x", "x"]);
        Assert.False(s.IsStronglyEqual(0, 1));
        Assert.True(s.IsStronglyEqual(0, 2));
        Assert.Equal(s.GetElement(0), s.GetElement(1));
    }

    // Monaco: LineSequence.getText / length / getElement
    [Fact]
    public void GetText_Range_JoinsLinesWithLf()
    {
        LineSequence s = Make();
        Assert.Equal(4, s.Length);
        Assert.Equal(3, s.GetElement(2));
        Assert.Equal("  b\n\t\tc", s.GetText(new OffsetRange(1, 3)));
    }

    // ISequence.getBoundaryScore is optional in Monaco
    [Fact]
    public void BoundaryScore_NotImplemented_DefaultsToZero()
    {
        ISequence s = new NoScoreSequence();
        Assert.Equal(0, s.GetBoundaryScore(1));
    }

    private sealed class NoScoreSequence : ISequence
    {
        public int Length => 0;

        public int GetElement(int offset) => 0;

        public bool IsStronglyEqual(int offset1, int offset2) => true;
    }
}
