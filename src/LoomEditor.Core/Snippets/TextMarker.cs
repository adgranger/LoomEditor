// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (Text)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Plain text of a snippet (Monaco: <c>Text</c>).</summary>
/// <remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>
internal sealed class TextMarker(string value) : SnippetMarker
{
    /// <summary>The text.</summary>
    public string Value { get; set; } = value;

    /// <inheritdoc />
    public override int Length => Value.Length;

    /// <summary>Puts a backslash before <c>\</c>, <c>$</c> and <c>}</c> (Monaco: <c>Text.escape</c>).</summary>
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // The backslash first, so that the ones added for `$` and `}` are not escaped again.
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal).Replace("}", "\\}", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    public override string ToTextmateString() => Escape(Value);

    /// <inheritdoc />
    public override SnippetMarker Clone() => new TextMarker(Value);
}
