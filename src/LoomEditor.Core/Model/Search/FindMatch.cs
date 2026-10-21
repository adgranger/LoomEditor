// Ported from microsoft/vscode: src/vs/editor/common/model.ts, src/vs/editor/common/model/textModelSearch.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Search;

/// <summary>A match of a search (Monaco: <c>FindMatch</c>).</summary>
/// <param name="Range">The range of the match.</param>
/// <param name="Matches">
/// The captured groups (index 0 is the whole match), or <c>null</c> when captures were not requested. A <c>null</c>
/// entry is a group that did not take part in the match (JavaScript <c>undefined</c>).
/// </param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record FindMatch(Range Range, string?[]? Matches)
{
    /// <summary>Creates a match from a regex match (Monaco: <c>createFindMatch</c>).</summary>
    public static FindMatch Create(Range range, Match match, bool captureMatches)
    {
        if (!captureMatches)
        {
            return new FindMatch(range, null);
        }

        var matches = new string?[match.Groups.Count];
        for (var i = 0; i < matches.Length; i++)
        {
            var group = match.Groups[i];
            matches[i] = group.Success ? group.Value : null;
        }

        return new FindMatch(range, matches);
    }
}
