using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class AdditionalLineHeightsTests
{
    private const int Lh = 10;

    private static readonly IReadOnlySet<string> None = new HashSet<string>();

    private static IReadOnlyList<AdditionalLineHeight> Compute(
        (string, int, int)[] zones, IReadOnlySet<string>? ignored = null, Func<int, int>? viewToModel = null, Func<int, int>? viewLineCount = null) =>
        AdditionalLineHeights.Compute(zones, ignored ?? None, viewToModel ?? (v => v), 10, viewLineCount, Lh);

    [Fact]
    public void Compute_NoZones_NoHeights() => Assert.Empty(Compute([]));

    [Fact]
    public void Compute_ForeignZone_ReturnsItsHeightAtModelLine() =>
        Assert.Equal([new AdditionalLineHeight(3, 25)], Compute([("a", 3, 25)]));

    [Fact]
    public void Compute_OwnZone_Ignored() =>
        Assert.Equal([new AdditionalLineHeight(5, 7)], Compute([("own", 3, 25), ("b", 5, 7)], new HashSet<string> { "own" }));

    [Fact]
    public void Compute_AfterLineZero_LineNumberZero() =>
        Assert.Equal([new AdditionalLineHeight(0, 12)], Compute([("a", 0, 12)], viewToModel: _ => throw new InvalidOperationException()));

    [Fact]
    public void Compute_WrappedLine_ExtraRows() =>
        Assert.Equal([new AdditionalLineHeight(4, 2 * Lh)], Compute([], viewLineCount: l => l == 4 ? 3 : 1));

    [Fact]
    public void Compute_ZoneAndWrap_SameLine_Combined() =>
        Assert.Equal([new AdditionalLineHeight(2, 5 + Lh)], Compute([("a", 2, 5)], viewLineCount: l => l == 2 ? 2 : 1));

    [Fact]
    public void Compute_ZoneInHiddenView_MapsToModelLine() =>
        Assert.Equal([new AdditionalLineHeight(8, 9)], Compute([("a", 2, 9)], viewToModel: v => v + 6));

    [Fact]
    public void JoinCombine_InterleavedKeys_Sorted() =>
        Assert.Equal([1, 2, 3, 4], AdditionalLineHeights.JoinCombine<int>([1, 3], [2, 4], k => k, (a, _) => a));

    [Fact]
    public void JoinCombine_EqualKeys_Combined() =>
        Assert.Equal([(1, 15), (2, 1)], AdditionalLineHeights.JoinCombine<(int K, int V)>([(1, 5)], [(1, 10), (2, 1)], t => t.K, (a, b) => (a.K, a.V + b.V)));

    [Fact]
    public void JoinCombine_OneListEmpty_ReturnsTheOther()
    {
        Assert.Equal([1, 2], AdditionalLineHeights.JoinCombine<int>([], [1, 2], k => k, (a, _) => a));
        Assert.Equal([1, 2], AdditionalLineHeights.JoinCombine<int>([1, 2], [], k => k, (a, _) => a));
    }
}
