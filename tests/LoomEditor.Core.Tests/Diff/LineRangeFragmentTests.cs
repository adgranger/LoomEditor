using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class LineRangeFragmentTests
{
    private static LineRangeFragment Fragment(string[] lines, LineRangeFragmentContext context)
    {
        var range = new LineRange(1, lines.Length + 1);
        return new LineRangeFragment(range, lines, new DetailedLineRangeMapping(range, range, []), context);
    }

    [Fact]
    public void LineRangeFragment_ComputeSimilarity_IdenticalOne_DisjointZero()
    {
        var context = new LineRangeFragmentContext();
        LineRangeFragment a = Fragment(["abc", "abc"], context);
        Assert.Equal(1, a.ComputeSimilarity(Fragment(["abc", "abc"], context)));
        // Disjoint characters still share the new-line of each line, so Monaco never reports exactly 0: 1 - 6 / 8.
        Assert.Equal(0.25, a.ComputeSimilarity(Fragment(["xyz", "xyz"], context)), 10);
    }

    [Fact]
    public void LineRangeFragment_ComputeSimilarity_Partial()
    {
        var context = new LineRangeFragmentContext();
        // "ab\n" vs "ac\n": histogram difference is 2 (b and c), total count 3 + 3.
        double similarity = Fragment(["ab"], context).ComputeSimilarity(Fragment(["ac"], context));
        Assert.Equal(1 - (2.0 / 6), similarity, 10);
    }
}
