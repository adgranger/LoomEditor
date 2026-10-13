// Not ported from Monaco: Monaco has no direct tests for PieceTreeSearchCache. All tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeSearchCacheTests
{
    private static TreeNode MakeNode(int length, int lineFeeds) =>
        new(new Piece(0, default, default, lineFeeds, length), NodeColor.Black);

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(15, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void Get_OffsetAtEdges_HitsOnlyInsideClosedInterval(int offset, bool expectedHit)
    {
        var cache = new PieceTreeSearchCache(5);
        var entry = new CacheEntry(MakeNode(10, 0), 10, null);
        cache.Set(entry);

        Assert.Same(expectedHit ? entry : null, cache.Get(offset));
    }

    [Fact]
    public void Get_SeveralMatches_ReturnsMostRecent()
    {
        var cache = new PieceTreeSearchCache(5);
        var older = new CacheEntry(MakeNode(10, 0), 0, null);
        var newer = new CacheEntry(MakeNode(10, 0), 10, null);
        cache.Set(older);
        cache.Set(newer);

        Assert.Same(newer, cache.Get(10));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public void GetByLine_LineNumber_HitsOnlyInHalfOpenInterval(int lineNumber, bool expectedHit)
    {
        var cache = new PieceTreeSearchCache(5);
        var entry = new CacheEntry(MakeNode(10, 3), 0, 5);
        cache.Set(entry);

        Assert.Same(expectedHit ? entry : null, cache.GetByLine(lineNumber));
    }

    [Fact]
    public void GetByLine_NoStartLineNumber_Misses()
    {
        var cache = new PieceTreeSearchCache(5);
        cache.Set(new CacheEntry(MakeNode(10, 3), 0, null));

        Assert.Null(cache.GetByLine(2));
    }

    [Fact]
    public void Set_BeyondLimit_EvictsOldestEntry()
    {
        var cache = new PieceTreeSearchCache(2);
        var first = new CacheEntry(MakeNode(1, 0), 0, null);
        var second = new CacheEntry(MakeNode(1, 0), 100, null);
        var third = new CacheEntry(MakeNode(1, 0), 200, null);
        cache.Set(first);
        cache.Set(second);
        cache.Set(third);

        Assert.Null(cache.Get(0));
        Assert.Same(second, cache.Get(100));
        Assert.Same(third, cache.Get(200));
    }

    [Fact]
    public void Validate_Offset_RemovesLateAndDetachedEntriesOnly()
    {
        var cache = new PieceTreeSearchCache(10);
        var sentinel = new TreeNode(new Piece(0, default, default, 0, 0), NodeColor.Black);
        var kept = new CacheEntry(MakeNode(10, 0), 0, null);
        var atOffset = new CacheEntry(MakeNode(10, 0), 50, null);
        var afterOffset = new CacheEntry(MakeNode(10, 0), 60, null);
        var detachedNode = MakeNode(10, 0);
        var detached = new CacheEntry(detachedNode, 20, null);
        cache.Set(kept);
        cache.Set(atOffset);
        cache.Set(afterOffset);
        cache.Set(detached);
        detachedNode.Detach(sentinel);

        cache.Validate(50);

        Assert.Same(kept, cache.Get(5));
        Assert.Null(cache.Get(50));
        Assert.Null(cache.Get(65));
        Assert.Null(cache.Get(25));
    }
}
