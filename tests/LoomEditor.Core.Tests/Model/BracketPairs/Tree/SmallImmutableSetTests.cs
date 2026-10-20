using LoomEditor.Core.Model.BracketPairs.Tree;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

public class SmallImmutableSetTests
{
    // Monaco: 'Bracket Pair Colorizer - ImmutableSet' > 'Basic'
    [Fact]
    public void Basic()
    {
        var keyProvider = new DenseKeyProvider<string>();

        var empty = SmallImmutableSet<string>.Empty;
        var items1 = empty.Add("item1", keyProvider);
        var items12 = items1.Add("item2", keyProvider);
        var items2 = empty.Add("item2", keyProvider);
        var items21 = items2.Add("item1", keyProvider);

        var items3 = empty.Add("item3", keyProvider);

        Assert.True(items12.Intersects(items1));
        Assert.True(items12.Has("item1", keyProvider));

        Assert.False(items12.Intersects(items3));
        Assert.False(items12.Has("item3", keyProvider));

        Assert.True(items21.Equals(items12));
        Assert.False(items21.Equals(items2));
    }

    // Monaco: 'Bracket Pair Colorizer - ImmutableSet' > 'Many Elements'
    [Fact]
    public void ManyElements()
    {
        var keyProvider = new DenseKeyProvider<string>();
        var set = SmallImmutableSet<string>.Empty;

        for (var i = 0; i < 100; i++)
        {
            keyProvider.GetKey($"item{i}");
            if (i % 2 == 0)
            {
                set = set.Add($"item{i}", keyProvider);
            }
        }

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(i % 2 == 0, set.Has($"item{i}", keyProvider));
        }
    }

    [Fact]
    public void Add_KeyAbove31_UsesAdditionalItems()
    {
        var set = SmallImmutableSet<int>.Empty;
        foreach (var key in new[] { 0, 31, 32, 70 })
        {
            set = set.Add(key, IdentityKeyProvider.Instance);
        }

        foreach (var key in new[] { 0, 31, 32, 70 })
        {
            Assert.True(set.Has(key, IdentityKeyProvider.Instance));
        }

        Assert.False(set.Has(33, IdentityKeyProvider.Instance));
        Assert.False(set.Has(200, IdentityKeyProvider.Instance));
    }

    [Fact]
    public void Merge_SetsWithAdditionalItems_ContainsBoth()
    {
        var a = SmallImmutableSet<int>.Empty.Add(0, IdentityKeyProvider.Instance).Add(70, IdentityKeyProvider.Instance);
        var b = SmallImmutableSet<int>.Empty.Add(31, IdentityKeyProvider.Instance).Add(32, IdentityKeyProvider.Instance);

        var merged = a.Merge(b);

        foreach (var key in new[] { 0, 31, 32, 70 })
        {
            Assert.True(merged.Has(key, IdentityKeyProvider.Instance));
        }

        Assert.True(a.Intersects(merged));
    }

    [Fact]
    public void GetEmpty_And_SmallSets_AreCachedInstances()
    {
        var keyProvider = new DenseKeyProvider<string>();
        keyProvider.GetKey("a");
        keyProvider.GetKey("b");
        keyProvider.GetKey("c");
        keyProvider.GetKey("d");

        var first = SmallImmutableSet<string>.Empty.Add("d", keyProvider);
        var second = SmallImmutableSet<string>.Empty.Add("d", keyProvider);

        Assert.Same(first, second);
    }
}
