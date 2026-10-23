// Ported from microsoft/vscode: src/vs/base/common/filters.ts (isSeparatorAtPos, isWhitespaceAtPos)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Filters;

/// <summary>Character tests of the fuzzy scorer (Monaco: <c>isSeparatorAtPos</c>, <c>isWhitespaceAtPos</c>).</summary>
/// <remarks>Threading: stateless, safe from any thread. <c>isEmojiImprecise</c> is <see cref="Strings.IsEmojiImprecise"/>.</remarks>
internal static class CharacterClasses
{
    /// <summary>Monaco: <c>isSeparatorAtPos</c>. Uses the full code point at a surrogate pair.</summary>
    public static bool IsSeparatorAtPos(string value, int index)
    {
        if (index < 0 || index >= value.Length)
        {
            return false;
        }

        var c = value[index];
        var code = char.IsHighSurrogate(c) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])
            ? char.ConvertToUtf32(c, value[index + 1])
            : c;
        switch (code)
        {
            case '_': case '-': case '.': case ' ': case '/': case '\\': case '\'': case '"': case ':':
            case '$': case '<': case '>': case '(': case ')': case '[': case ']': case '{': case '}':
                return true;
            default:
                return Strings.IsEmojiImprecise(code);
        }
    }

    /// <summary>Monaco: <c>isWhitespaceAtPos</c>.</summary>
    public static bool IsWhitespaceAtPos(string value, int index)
        => index >= 0 && index < value.Length && (value[index] == ' ' || value[index] == '\t');
}
