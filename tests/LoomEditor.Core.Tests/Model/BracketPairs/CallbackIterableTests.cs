using LoomEditor.Core.Model.BracketPairs;

namespace LoomEditor.Core.Tests.Model.BracketPairs;

// No upstream test targets CallbackIterable (base/common/arrays.ts).
public class CallbackIterableTests
{
    private static CallbackIterable<string> Of(params string[] items)
    {
        return Of(null, items);
    }

    // Records the items it hands out in `visited`, if any.
    private static CallbackIterable<string> Of(List<string>? visited, string[] items)
    {
        return new CallbackIterable<string>(callback =>
        {
            foreach (var item in items)
            {
                visited?.Add(item);
                if (!callback(item))
                {
                    return;
                }
            }
        });
    }

    [Fact]
    public void FindLastMaxBy_EqualMaxima_KeepsFirstOfEqualMaxima()
    {
        var iterable = Of("bb", "aa", "c");

        var result = iterable.FindLastMaxBy((a, b) => a.Length.CompareTo(b.Length));

        Assert.Equal("bb", result);
    }

    [Fact]
    public void FindLastMaxBy_StrictlyGreaterItem_ReplacesTheResult()
    {
        var iterable = Of("a", "bbb", "cc", "ddd");

        var result = iterable.FindLastMaxBy((a, b) => a.Length.CompareTo(b.Length));

        Assert.Equal("bbb", result);
    }

    [Fact]
    public void FindLastMaxBy_NoItem_ReturnsNull()
    {
        Assert.Null(CallbackIterable<string>.Empty.FindLastMaxBy((a, b) => string.CompareOrdinal(a, b)));
    }

    [Fact]
    public void FindFirst_StopsIterating()
    {
        var visited = new List<string>();
        var iterable = Of(visited, ["a", "b", "c", "d"]);

        var result = iterable.FindFirst(item => item == "b");

        Assert.Equal("b", result);
        Assert.Equal(["a", "b"], visited);
    }

    [Fact]
    public void FindFirst_NoMatch_ReturnsNull()
    {
        Assert.Null(Of("a", "b").FindFirst(item => item == "z"));
    }

    [Fact]
    public void FindLast_SeveralMatches_ReturnsTheLastOne()
    {
        Assert.Equal("c2", Of("c1", "x", "c2", "y").FindLast(item => item.StartsWith('c')));
    }

    [Fact]
    public void Empty_ToArray_IsEmpty()
    {
        Assert.Empty(CallbackIterable<string>.Empty.ToArray());
        Assert.Same(CallbackIterable<string>.Empty, CallbackIterable<string>.Empty);
    }

    [Fact]
    public void ToArray_ReturnsTheItemsInOrder()
    {
        Assert.Equal(["a", "b", "c"], Of("a", "b", "c").ToArray());
    }

    [Fact]
    public void ForEach_VisitsEveryItem()
    {
        var visited = new List<string>();

        Of("a", "b", "c").ForEach(visited.Add);

        Assert.Equal(["a", "b", "c"], visited);
    }

    [Fact]
    public void Filter_KeepsTheMatchingItemsOnly()
    {
        Assert.Equal(["aa", "cc"], Of("aa", "b", "cc").Filter(item => item.Length == 2).ToArray());
    }

    [Fact]
    public void Filter_CallbackStops_StopsTheSource()
    {
        var iterable = Of("aa", "bb", "cc").Filter(item => item != "bb");
        var seen = new List<string>();

        iterable.Iterate(item =>
        {
            seen.Add(item);
            return false;
        });

        Assert.Equal(["aa"], seen);
    }

    [Fact]
    public void Map_TransformsEveryItem()
    {
        Assert.Equal(["A", "B"], Of("a", "b").Map(item => item.ToUpperInvariant()).ToArray());
    }

    [Fact]
    public void Some_StopsAtTheFirstMatch()
    {
        var visited = new List<string>();
        var iterable = Of(visited, ["a", "b", "c"]);

        Assert.True(iterable.Some(item => item == "a"));
        Assert.Equal(["a"], visited);
        Assert.False(Of("a").Some(item => item == "z"));
    }
}
