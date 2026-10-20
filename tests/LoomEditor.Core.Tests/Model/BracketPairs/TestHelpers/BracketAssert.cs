// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelWithTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

/// <summary>The assertions of the bracket matching tests (Monaco: <c>assertIsNotBracket</c>, <c>assertIsBracket</c>).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class BracketAssert
{
    /// <summary>Asserts that <c>MatchBracket</c> finds nothing at the position.</summary>
    public static void IsNotBracket(TextModel model, int lineNumber, int column)
    {
        var match = model.BracketPairs.MatchBracket(new Position(lineNumber, column));
        Assert.True(match is null, $"is not matching brackets at {lineNumber}, {column}");
    }

    /// <summary>
    /// Asserts that <c>MatchBracket</c> finds the <paramref name="expected"/> pair at <paramref name="testPosition"/>; the
    /// two ranges of both pairs are sorted with <see cref="Range.CompareUsingStarts"/> first, like Monaco.
    /// </summary>
    public static void IsBracket(TextModel model, Position testPosition, (Range First, Range Second) expected)
    {
        Range[] expectedSorted = Sort(expected.First, expected.Second);
        var actual = model.BracketPairs.MatchBracket(testPosition);
        Range[]? actualSorted = actual is { } pair ? Sort(pair.Opening, pair.Closing) : null;

        Assert.True(actualSorted is not null && expectedSorted.SequenceEqual(actualSorted), $"matches brackets at {testPosition}");
    }

    private static Range[] Sort(Range a, Range b)
    {
        Range[] ranges = [a, b];
        Array.Sort(ranges, Range.CompareUsingStarts);
        return ranges;
    }
}
