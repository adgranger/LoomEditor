// Ported from microsoft/vscode: src/vs/base/common/search.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>Case-preserving replace (Monaco: <c>base/common/search.ts</c>).</summary>
/// <remarks>
/// Behavior difference: upper-casing and lower-casing are invariant and keep the string length, so the few characters
/// whose JavaScript upper case is longer (<c>ß</c> → <c>SS</c>) keep their length.
/// Threading: pure static methods; thread-safe.
/// </remarks>
internal static class SearchStrings
{
    /// <summary>Applies the case of the match to the replace <paramref name="pattern"/> (Monaco: <c>buildReplaceStringWithCasePreserved</c>).</summary>
    /// <param name="matches">The groups of the match (index 0 is the whole match), or <c>null</c>.</param>
    /// <param name="pattern">The replace string.</param>
    public static string BuildReplaceStringWithCasePreserved(IReadOnlyList<string?>? matches, string pattern)
    {
        string match0 = matches is { Count: > 0 } ? matches[0] ?? "" : "";
        if (match0.Length == 0)
        {
            return pattern;
        }

        var containsHyphens = ValidateSpecificSpecialCharacter(match0, pattern, '-');
        var containsUnderscores = ValidateSpecificSpecialCharacter(match0, pattern, '_');
        if (containsHyphens && !containsUnderscores)
        {
            return BuildReplaceStringForSpecificSpecialCharacter(match0, pattern, '-');
        }

        if (!containsHyphens && containsUnderscores)
        {
            return BuildReplaceStringForSpecificSpecialCharacter(match0, pattern, '_');
        }

        if (string.Equals(match0.ToUpperInvariant(), match0, StringComparison.Ordinal))
        {
            return pattern.ToUpperInvariant();
        }

        if (string.Equals(match0.ToLowerInvariant(), match0, StringComparison.Ordinal))
        {
            return pattern.ToLowerInvariant();
        }

        if (Strings.ContainsUppercaseCharacter(match0[0].ToString()) && pattern.Length > 0)
        {
            return char.ToUpperInvariant(pattern[0]) + pattern[1..];
        }

        if (char.ToUpperInvariant(match0[0]) != match0[0] && pattern.Length > 0)
        {
            return char.ToLowerInvariant(pattern[0]) + pattern[1..];
        }

        // we don't understand its pattern yet.
        return pattern;
    }

    private static bool ValidateSpecificSpecialCharacter(string match0, string pattern, char specialCharacter) =>
        match0.Contains(specialCharacter)
        && pattern.Contains(specialCharacter)
        && match0.Split(specialCharacter).Length == pattern.Split(specialCharacter).Length;

    private static string BuildReplaceStringForSpecificSpecialCharacter(string match0, string pattern, char specialCharacter)
    {
        string[] splitPattern = pattern.Split(specialCharacter);
        string[] splitMatch = match0.Split(specialCharacter);
        var replaceString = new System.Text.StringBuilder();
        for (var index = 0; index < splitPattern.Length; index++)
        {
            replaceString.Append(BuildReplaceStringWithCasePreserved([splitMatch[index]], splitPattern[index])).Append(specialCharacter);
        }

        return replaceString.ToString(0, replaceString.Length - 1);
    }
}
