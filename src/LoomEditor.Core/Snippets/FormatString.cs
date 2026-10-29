// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (FormatString)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Snippets;

/// <summary>A <c>${1:/upcase}</c> or <c>${1:+if}</c> group of the format of a <see cref="Transform"/> (Monaco: <c>FormatString</c>).</summary>
/// <remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>
internal sealed partial class FormatString(int index, string? shorthandName = null, string? ifValue = null, string? elseValue = null) : SnippetMarker
{
    /// <summary>The capture group this format reads.</summary>
    public int Index { get; } = index;

    /// <summary><c>upcase</c>, <c>downcase</c>, <c>capitalize</c>, <c>pascalcase</c>, <c>camelcase</c>, <c>kebabcase</c>, <c>snakecase</c> or <see langword="null"/>.</summary>
    public string? ShorthandName { get; } = shorthandName;

    /// <summary>The text used when the group is not empty (<c>+</c> and <c>?</c> forms).</summary>
    public string? IfValue { get; } = ifValue;

    /// <summary>The text used when the group is empty (<c>-</c> and <c>?</c> forms).</summary>
    public string? ElseValue { get; } = elseValue;

    /// <summary>Formats <paramref name="value"/>, the text of the capture group (empty when it did not match).</summary>
    public string Resolve(string? value)
    {
        bool hasValue = !string.IsNullOrEmpty(value);
        switch (ShorthandName)
        {
            case "upcase":
                return hasValue ? value!.ToUpperInvariant() : string.Empty;
            case "downcase":
                return hasValue ? value!.ToLowerInvariant() : string.Empty;
            case "capitalize":
                return hasValue ? char.ToUpperInvariant(value![0]) + value[1..] : string.Empty;
            case "pascalcase":
                return hasValue ? ToPascalCase(value!) : string.Empty;
            case "camelcase":
                return hasValue ? ToCamelCase(value!) : string.Empty;
            case "kebabcase":
                return hasValue ? ToKebabCase(value!) : string.Empty;
            case "snakecase":
                return hasValue ? ToSnakeCase(value!) : string.Empty;
        }

        if (hasValue && IfValue is not null)
        {
            return IfValue;
        }

        if (!hasValue && ElseValue is not null)
        {
            return ElseValue;
        }

        return value ?? string.Empty;
    }

    // `$` of the JavaScript patterns is `\z` here: without the multiline flag it only matches at the very end.
    [GeneratedRegex(@"[\p{L}0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"^_+|_+\z", RegexOptions.CultureInvariant)]
    private static partial Regex EdgeUnderscoresRegex();

    [GeneratedRegex(@"[\s_]+", RegexOptions.CultureInvariant)]
    private static partial Regex SpacesOrUnderscoresRegex();

    [GeneratedRegex(@"[\s_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorsRegex();

    [GeneratedRegex(@"\p{Lu}{2,}(?=\p{Lu}\p{Ll}+[0-9]*|[\s_-]|\z)|\p{Lu}?\p{Ll}+[0-9]*|\p{Lu}(?=\p{Lu}\p{Ll})|\p{Lu}(?=[\s_-]|\z)|[0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex KebabWordRegex();

    [GeneratedRegex(@"(\p{Ll})(\p{Lu})", RegexOptions.CultureInvariant)]
    private static partial Regex LowerUpperRegex();

    [GeneratedRegex(@"[\s\-]+", RegexOptions.CultureInvariant)]
    private static partial Regex SpacesOrDashesRegex();

    // Note: word-based case transforms rely on uppercase/lowercase distinctions.
    // For scripts without case, transforms are effectively no-ops.
    private static string ToKebabCase(string value)
    {
        MatchCollection match = WordRegex().Matches(value);
        if (match.Count == 0)
        {
            return value;
        }

        string cleaned = EdgeUnderscoresRegex().Replace(value.Trim(), string.Empty);
        MatchCollection words = KebabWordRegex().Matches(cleaned);
        if (words.Count == 0)
        {
            return string.Join('-', SeparatorsRegex().Split(cleaned).Where(word => word.Length > 0).Select(word => word.ToLowerInvariant()));
        }

        return string.Join('-', words.Select(word => word.Value.ToLowerInvariant()));
    }

    private static string ToPascalCase(string value)
    {
        MatchCollection match = WordRegex().Matches(value);
        if (match.Count == 0)
        {
            return value;
        }

        return string.Concat(match.Select(word => char.ToUpperInvariant(word.Value[0]) + word.Value[1..]));
    }

    private static string ToCamelCase(string value)
    {
        MatchCollection match = WordRegex().Matches(value);
        if (match.Count == 0)
        {
            return value;
        }

        return string.Concat(match.Select((word, index) =>
            (index == 0 ? char.ToLowerInvariant(word.Value[0]) : char.ToUpperInvariant(word.Value[0])) + word.Value[1..]));
    }

    private static string ToSnakeCase(string value) =>
        SpacesOrDashesRegex().Replace(LowerUpperRegex().Replace(value, "$1_$2"), "_").ToLowerInvariant();

    /// <inheritdoc />
    public override string ToTextmateString()
    {
        string value = "${" + Index;
        if (!string.IsNullOrEmpty(ShorthandName))
        {
            value += ":/" + ShorthandName;
        }
        else if (!string.IsNullOrEmpty(IfValue) && !string.IsNullOrEmpty(ElseValue))
        {
            value += ":?" + IfValue + ":" + ElseValue;
        }
        else if (!string.IsNullOrEmpty(IfValue))
        {
            value += ":+" + IfValue;
        }
        else if (!string.IsNullOrEmpty(ElseValue))
        {
            value += ":-" + ElseValue;
        }

        return value + "}";
    }

    /// <inheritdoc />
    public override SnippetMarker Clone() => new FormatString(Index, ShorthandName, IfValue, ElseValue);
}
