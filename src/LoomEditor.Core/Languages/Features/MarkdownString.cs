// Ported from microsoft/vscode: src/vs/base/common/htmlContent.ts (IMarkdownString, MarkdownString.appendText, isEmptyMarkdownString, escapeMarkdownSyntaxTokens)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Languages.Features;

/// <summary>
/// Markdown text shown in hovers and completion documentation. <see cref="Value"/> is always Markdown source.
/// </summary>
/// <remarks>
/// Threading: immutable, safe to share between threads.
/// Not ported from Monaco: <c>supportThemeIcons</c>, <c>supportHtml</c>, <c>baseUri</c>, <c>uris</c>.
/// </remarks>
/// <param name="Value">The Markdown source.</param>
/// <param name="IsTrusted">True when the Markdown may contain command links and other trusted content.</param>
public sealed partial record MarkdownString(string Value, bool IsTrusted = false)
{
    /// <summary>True when <see cref="Value"/> is empty.</summary>
    public bool IsEmpty => Value.Length == 0;

    /// <summary>True for null or an empty <paramref name="value"/> (port of <c>isEmptyMarkdownString</c> for one value).</summary>
    public static bool IsNullOrEmpty(MarkdownString? value) => value is null || value.IsEmpty;

    /// <summary>True for null, an empty list, or a list where every value is empty.</summary>
    public static bool IsNullOrEmpty(IReadOnlyList<MarkdownString>? values)
    {
        if (values is null)
        {
            return true;
        }

        foreach (MarkdownString value in values)
        {
            if (!IsNullOrEmpty(value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a Markdown string that displays <paramref name="text"/> literally (port of <c>appendText</c> with the
    /// <c>Paragraph</c> newline style on an empty string).
    /// </summary>
    /// <remarks>Line breaks must be <c>\n</c>: a <c>\r</c> is treated as part of the text, so this is not meant for <c>\r\n</c>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public static MarkdownString FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string escaped = EscapeMarkdownSyntaxTokens(text);
        escaped = BlanksRegex().Replace(escaped, static m => string.Concat(Enumerable.Repeat("&nbsp;", m.Length)));
        escaped = escaped.Replace(">", "\\>").Replace("\n", "\n\n");
        return new MarkdownString(escaped);
    }

    private static string EscapeMarkdownSyntaxTokens(string text)
    {
        string escaped = SyntaxTokensRegex().Replace(text, "\\$&");
        return LeadingDashRegex().Replace(escaped, "$1\\-");
    }

    [GeneratedRegex(@"[\\`*_{}\[\]()#+!~]")]
    private static partial Regex SyntaxTokensRegex();

    [GeneratedRegex(@"^([ \t]*)-", RegexOptions.Multiline)]
    private static partial Regex LeadingDashRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex BlanksRegex();
}
