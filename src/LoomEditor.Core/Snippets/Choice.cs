// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (Choice)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>The options of a <c>${1|a,b|}</c> tabstop; the first one is what is inserted (Monaco: <c>Choice</c>).</summary>
/// <remarks>
/// Behavior difference: <see cref="Clone"/> copies the options; Monaco re-parents the same <c>Text</c> instances to the clone.
/// Threading: not thread-safe (a snippet is owned by one session).
/// </remarks>
internal sealed class Choice : SnippetMarker
{
    /// <summary>The options, in order.</summary>
    public List<TextMarker> Options { get; } = [];

    /// <inheritdoc />
    public override int Length => Options[0].Length;

    /// <summary>Adds an option; anything but a <see cref="TextMarker"/> is ignored.</summary>
    public override SnippetMarker AppendChild(SnippetMarker child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child is TextMarker text)
        {
            text.Parent = this;
            Options.Add(text);
        }

        return this;
    }

    /// <inheritdoc />
    public override string ToString() => Options[0].Value;

    /// <inheritdoc />
    public override string ToTextmateString() =>
        string.Join(
            ',',
            Options.Select(option => option.Value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("|", "\\|", StringComparison.Ordinal)
                .Replace(",", "\\,", StringComparison.Ordinal)));

    /// <inheritdoc />
    public override SnippetMarker Clone()
    {
        var clone = new Choice();
        foreach (TextMarker option in Options)
        {
            clone.AppendChild(new TextMarker(option.Value));
        }

        return clone;
    }
}
