// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts (the _modifyText of the case actions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>The text transforms of the case commands (upper, lower, title, snake, camel, Pascal and kebab case).</summary>
/// <remarks>
/// <para>
/// Behavior difference: Monaco uses <c>toLocaleUpperCase</c> and <c>toLocaleLowerCase</c> (the locale of the user); this
/// port uses the invariant culture (the Turkish dotted i differs).
/// </para>
/// <para>
/// Monaco's <c>BackwardsCompatibleRegExp</c> is not ported: every regex compiles in .NET. Where the JavaScript flags
/// have <c>m</c>, the regexes are <see cref="RegexOptions.Multiline"/>. Threading: stateless; thread-safe.
/// </para>
/// </remarks>
internal static class CaseTransforms
{
    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Multiline;

    internal static readonly Regex TitleBoundary = new("(^|[^\\p{L}\\p{N}']|((^|\\P{L})'))\\p{L}", Options);

    internal static readonly Regex SnakeCaseBoundary = new("(\\p{Ll})(\\p{Lu})", Options);
    internal static readonly Regex SnakeSingleLetters = new("(\\p{Lu}|\\p{N})(\\p{Lu})(\\p{Ll})", Options);

    internal static readonly Regex CamelSingleLineWordBoundary = new("[_\\s-]+", Options);
    internal static readonly Regex CamelMultiLineWordBoundary = new("[_-]+", Options);
    internal static readonly Regex CamelValidWordStart = new("^(\\p{Lu}[^\\p{Lu}])", Options);

    internal static readonly Regex PascalWordBoundary = new("[_ \\t-]", Options);
    internal static readonly Regex PascalWordBoundaryToMaintain = new("(?<=\\.)", Options);
    internal static readonly Regex PascalUpperCaseWordMatcher = new("^\\p{Lu}+$", Options);

    internal static readonly Regex KebabCaseBoundary = new("(\\p{Ll})(\\p{Lu})", Options);
    internal static readonly Regex KebabSingleLetters = new("(\\p{Lu}|\\p{N})(\\p{Lu}\\p{Ll})", Options);
    internal static readonly Regex KebabUnderscoreBoundary = new("(\\S)(_)(\\S)", Options);

    private static readonly Regex NewLine = new("\\r\\n|\\r|\\n", RegexOptions.CultureInvariant);

    /// <summary>Monaco: <c>UpperCaseAction._modifyText</c>.</summary>
    public static string ToUpper(string text) => text.ToUpperInvariant();

    /// <summary>Monaco: <c>LowerCaseAction._modifyText</c>.</summary>
    public static string ToLower(string text) => text.ToLowerInvariant();

    /// <summary>Monaco: <c>TitleCaseAction._modifyText</c>.</summary>
    public static string ToTitleCase(string text) =>
        TitleBoundary.Replace(text.ToLowerInvariant(), m => m.Value.ToUpperInvariant());

    /// <summary>Monaco: <c>SnakeCaseAction._modifyText</c>.</summary>
    public static string ToSnakeCase(string text) =>
        SnakeSingleLetters.Replace(SnakeCaseBoundary.Replace(text, "${1}_${2}"), "${1}_${2}${3}").ToLowerInvariant();

    /// <summary>Monaco: <c>CamelCaseAction._modifyText</c>.</summary>
    public static string ToCamelCase(string text)
    {
        Regex wordBoundary = NewLine.IsMatch(text) ? CamelMultiLineWordBoundary : CamelSingleLineWordBoundary;
        // Split never returns an empty array: the first word always exists.
        string[] words = wordBoundary.Split(text);
        string firstWord = CamelValidWordStart.Replace(words[0], m => m.Value.ToLowerInvariant());
        return firstWord + string.Concat(words.Skip(1).Select(word => FirstChar(word).ToUpperInvariant() + word[FirstChar(word).Length..]));
    }

    /// <summary>Monaco: <c>PascalCaseAction._modifyText</c>.</summary>
    public static string ToPascalCase(string text)
    {
        string[] wordsWithMaintainBoundaries = PascalWordBoundaryToMaintain.Split(text);
        IEnumerable<string> words = wordsWithMaintainBoundaries.SelectMany(word => PascalWordBoundary.Split(word));

        return string.Concat(words.Select(word =>
        {
            string normalizedWord = FirstChar(word).ToUpperInvariant() + word[FirstChar(word).Length..];
            bool isAllCaps = normalizedWord.Length > 1 && PascalUpperCaseWordMatcher.IsMatch(normalizedWord);
            return isAllCaps ? normalizedWord[..1] + normalizedWord[1..].ToLowerInvariant() : normalizedWord;
        }));
    }

    /// <summary>Monaco: <c>KebabCaseAction._modifyText</c>.</summary>
    public static string ToKebabCase(string text) =>
        KebabSingleLetters.Replace(
            KebabCaseBoundary.Replace(KebabUnderscoreBoundary.Replace(text, "${1}-${3}"), "${1}-${2}"),
            "${1}-${2}").ToLowerInvariant();

    // JavaScript's substring(0, 1) and charAt(0) work on UTF-16 code units.
    private static string FirstChar(string word) => word.Length == 0 ? string.Empty : word[..1];
}
