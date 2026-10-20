// Ported from microsoft/vscode: src/vs/editor/common/core/wordHelper.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using System.Text.RegularExpressions;

namespace LoomEditor.Core.Text;

/// <summary>Word inside a model (Monaco: <c>IWordAtPosition</c>).</summary>
/// <param name="Word">The word.</param>
/// <param name="StartColumn">The column where the word starts.</param>
/// <param name="EndColumn">The column where the word ends.</param>
internal readonly record struct WordAtPosition(string Word, int StartColumn, int EndColumn);

/// <summary>Constants and word regular expressions of Monaco's <c>wordHelper.ts</c>.</summary>
/// <remarks>Threading: immutable; thread-safe (.NET regexes are stateless).</remarks>
internal static class WordHelper
{
    /// <summary>The default value of the <c>wordSeparators</c> editor option (Monaco: <c>USUAL_WORD_SEPARATORS</c>).</summary>
    public const string UsualWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

    /// <summary>
    /// Catches numbers (including floating numbers) in the first group, and alphanum in the second
    /// (Monaco: <c>DEFAULT_WORD_REGEXP</c>).
    /// </summary>
    public static readonly Regex DefaultWordRegExp = CreateWordRegExp();

    /// <summary>
    /// Creates a word definition regular expression based on default word separators. Optionally provide allowed
    /// separators that should be included in words (Monaco: <c>createWordRegExp</c>).
    /// </summary>
    /// <remarks>
    /// <c>\d</c> is written <c>[0-9]</c> and <c>\w</c> <c>[a-zA-Z0-9_]</c>: .NET's classes are Unicode, JavaScript's are ASCII.
    /// There is no <c>g</c> flag in .NET.
    /// </remarks>
    public static Regex CreateWordRegExp(string allowInWords = "")
    {
        var source = new StringBuilder(@"(-?[0-9]*\.[0-9][a-zA-Z0-9_]*)|([^");
        foreach (char sep in UsualWordSeparators)
        {
            if (allowInWords.Contains(sep, StringComparison.Ordinal))
            {
                continue;
            }

            source.Append('\\').Append(sep);
        }

        source.Append(@"\s]+)");
        return new Regex(source.ToString(), RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Returns <paramref name="wordDefinition"/>, or <see cref="DefaultWordRegExp"/> when it is <see langword="null"/>
    /// (Monaco: <c>ensureValidWordDefinition</c>; a .NET regex has no <c>global</c> flag and no <c>lastIndex</c> to reset).
    /// </summary>
    public static Regex EnsureValidWordDefinition(Regex? wordDefinition) => wordDefinition ?? DefaultWordRegExp;
}
