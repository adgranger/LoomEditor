using LoomEditor.Rendering.Tests.TestHelpers;

namespace LoomEditor.Rendering.Tests;

public class LineLayoutCacheTests
{
    [Fact]
    public void Get_SameTextTwice_CreatesOneLayout()
    {
        var measurer = new FakeTextMeasurer();
        using var cache = new LineLayoutCache(measurer, capacity: 10);

        ILineLayout first = cache.Get("abc");
        ILineLayout second = cache.Get("abc");

        Assert.Same(first, second);
        Assert.Single(measurer.Layouts);
    }

    [Fact]
    public void Get_BeyondCapacity_DisposesTheLeastRecentlyUsedLayout()
    {
        var measurer = new FakeTextMeasurer();
        using var cache = new LineLayoutCache(measurer, capacity: 2);

        cache.Get("a");
        cache.Get("b");
        cache.Get("a"); // "b" is now the least recently used
        cache.Get("c");

        Assert.Equal(2, cache.Count);
        Assert.False(measurer.Layouts.Single(l => l.Text == "a").IsDisposed);
        Assert.True(measurer.Layouts.Single(l => l.Text == "b").IsDisposed);
        Assert.False(measurer.Layouts.Single(l => l.Text == "c").IsDisposed);
    }

    [Fact]
    public void Get_AfterEviction_CreatesTheLayoutAgain()
    {
        var measurer = new FakeTextMeasurer();
        using var cache = new LineLayoutCache(measurer, capacity: 1);

        cache.Get("a");
        cache.Get("b");
        cache.Get("a");

        Assert.Equal(3, measurer.Layouts.Count);
    }

    [Fact]
    public void EnsureCapacity_Larger_KeepsMoreLayouts()
    {
        var measurer = new FakeTextMeasurer();
        using var cache = new LineLayoutCache(measurer, capacity: 1);

        cache.EnsureCapacity(3);
        cache.EnsureCapacity(2); // never shrinks
        cache.Get("a");
        cache.Get("b");
        cache.Get("c");

        Assert.Equal(3, cache.Count);
        Assert.All(measurer.Layouts, layout => Assert.False(layout.IsDisposed));
    }

    [Fact]
    public void Clear_Always_DisposesEveryLayout()
    {
        var measurer = new FakeTextMeasurer();
        var cache = new LineLayoutCache(measurer, capacity: 10);
        cache.Get("a");
        cache.Get("b");

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.All(measurer.Layouts, layout => Assert.True(layout.IsDisposed));
    }
}
