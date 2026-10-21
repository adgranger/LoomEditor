using LoomEditor.Core.Model.Search;

namespace LoomEditor.Core.Tests.Model.Search.TestHelpers;

internal static class SearchAssert
{
    /// <summary>Monaco: <c>assertFindMatch</c>; a record compares its array by reference, so compare the parts.</summary>
    public static void Match(FindMatch? actual, Range expectedRange, string?[]? expectedMatches = null, string? message = null)
    {
        Assert.NotNull(actual);
        Assert.True(expectedRange == actual.Range, $"{message} range: expected {expectedRange}, actual {actual.Range}");
        Assert.True(
            expectedMatches is null ? actual.Matches is null : actual.Matches is not null && expectedMatches.SequenceEqual(actual.Matches),
            $"{message} matches differ");
    }

    public static void Matches(IReadOnlyList<FindMatch> actual, IReadOnlyList<FindMatch> expected, string message)
    {
        Assert.True(expected.Count == actual.Count, $"{message}: expected {expected.Count} matches, actual {actual.Count}");
        for (var i = 0; i < expected.Count; i++)
        {
            Match(actual[i], expected[i].Range, expected[i].Matches, $"{message} #{i}");
        }
    }
}
