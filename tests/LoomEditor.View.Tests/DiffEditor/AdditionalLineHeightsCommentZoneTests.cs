using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

// A comment thread's zone is a whitespace the diff editor does not own: the other side must be padded by its height.
public class AdditionalLineHeightsCommentZoneTests
{
    private const int Lh = 10;
    private const string CommentZone = "comment-zone";

    private static IReadOnlyList<AdditionalLineHeight> Compute((string, int, int)[] zones, IReadOnlySet<string> ignored, Func<int, int>? viewLineCount = null) =>
        AdditionalLineHeights.Compute(zones, ignored, v => v, 20, viewLineCount, Lh);

    [Fact]
    public void CommentZone_NotIgnored_CountsAsAdditionalHeight() =>
        Assert.Equal([new AdditionalLineHeight(5, 100)], Compute([(CommentZone, 5, 100)], new HashSet<string> { "diff-own" }));

    [Fact]
    public void CommentZone_Ignored_NotCounted() =>
        Assert.Empty(Compute([(CommentZone, 5, 100)], new HashSet<string> { CommentZone }));

    // As in Monaco, two zones give two entries for the line (only a zone and wrapping are joined); LineRangeAlignment pads the other side for
    // each entry, so the heights add up.
    [Fact]
    public void TwoCommentZonesSameLine_HeightsAdd()
    {
        IReadOnlyList<AdditionalLineHeight> heights = Compute([(CommentZone, 5, 100), ("comment-zone-2", 5, 120)], new HashSet<string>());
        Assert.All(heights, h => Assert.Equal(5, h.LineNumber));
        Assert.Equal(220, heights.Sum(h => h.HeightInPx));
    }

    [Fact]
    public void CommentZoneAndWrappedLine_Combine() =>
        Assert.Equal([new AdditionalLineHeight(5, 100 + (2 * Lh))], Compute([(CommentZone, 5, 100)], new HashSet<string>(), l => l == 5 ? 3 : 1));
}
