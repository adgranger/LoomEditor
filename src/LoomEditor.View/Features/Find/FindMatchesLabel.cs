// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findWidget.ts (_updateMatchesCount, NLS_MATCHES_LOCATION, NLS_NO_RESULTS)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>The "3 of 12" text of the find widget.</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class FindMatchesLabel
{
    /// <summary>The tooltip of the label when the matches reached the limit (Monaco: <c>NLS_MATCHES_COUNT_LIMIT_TITLE</c>).</summary>
    public static string LimitTitle =>
        $"Only the first {FindConstants.MatchesLimit} results are highlighted, but all find operations work on the entire text.";

    /// <summary>"<c>{position} of {count}</c>", "<c>No results</c>" when there is no match, <c>?</c> for a position of 0, a <c>+</c> after a count at the limit.</summary>
    public static string Format(int matchesPosition, int matchesCount)
    {
        if (matchesCount <= 0)
        {
            return "No results";
        }

        string count = matchesCount >= FindConstants.MatchesLimit ? $"{matchesCount}+" : matchesCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string position = matchesPosition == 0 ? "?" : matchesPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"{position} of {count}";
    }
}
