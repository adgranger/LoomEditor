// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Text;

internal static partial class Strings
{
    /// <summary>
    /// Creates a regex from a search string (Monaco: <c>createRegExp</c>, always with <c>global</c> and <c>unicode</c>).
    /// </summary>
    /// <remarks>
    /// Behavior differences: the JavaScript flags map to .NET options (<c>i</c> → <c>IgnoreCase</c>, <c>m</c> →
    /// <c>Multiline</c>, always <c>CultureInvariant</c>; never <c>ECMAScript</c>). <c>g</c> and <c>u</c> have no .NET
    /// option (the searcher keeps its own index), so the pattern uses .NET syntax. <c>wholeWord</c> is not ported (no caller).
    /// </remarks>
    /// <exception cref="ArgumentException">The search string is empty or is not a valid pattern.</exception>
    public static Regex CreateRegExp(string searchString, bool isRegex, bool matchCase, bool multiline)
    {
        if (searchString.Length == 0)
        {
            throw new ArgumentException("Cannot create regex from empty string", nameof(searchString));
        }

        if (!isRegex)
        {
            searchString = EscapeRegExpCharacters(searchString);
        }

        var options = RegexOptions.CultureInvariant;
        if (!matchCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (multiline)
        {
            options |= RegexOptions.Multiline;
        }

        return new Regex(searchString, options);
    }

    /// <summary>Whether a string has an upper case character (Monaco: <c>containsUppercaseCharacter</c>).</summary>
    /// <param name="target">The string to test.</param>
    /// <param name="ignoreEscapedChars">Skip every backslash and the character after it.</param>
    public static bool ContainsUppercaseCharacter(string target, bool ignoreEscapedChars = false)
    {
        if (target.Length == 0)
        {
            return false;
        }

        if (ignoreEscapedChars)
        {
            var kept = new System.Text.StringBuilder(target.Length);
            for (var i = 0; i < target.Length; i++)
            {
                if (target[i] == '\\' && i + 1 < target.Length)
                {
                    i++;
                    continue;
                }

                kept.Append(target[i]);
            }

            target = kept.ToString();
        }

        return !string.Equals(target.ToLowerInvariant(), target, StringComparison.Ordinal);
    }
}
