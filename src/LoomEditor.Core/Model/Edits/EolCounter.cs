// Ported from microsoft/vscode: src/vs/editor/common/core/misc/eolCounter.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Edits;

/// <summary>The line break statistics of a text (Monaco: the tuple returned by <c>countEOL</c>, without the EOL kind).</summary>
/// <param name="Count">Number of line breaks (<c>\r\n</c>, <c>\n</c> or lone <c>\r</c>).</param>
/// <param name="FirstLineLength">Length of the text before the first line break, or the whole length.</param>
/// <param name="LastLineLength">Length of the text after the last line break, or the whole length.</param>
internal readonly record struct EolCount(int Count, int FirstLineLength, int LastLineLength);

/// <summary>Counts the line breaks of a text.</summary>
/// <remarks>
/// <para>Monaco: <c>countEOL</c>. The returned EOL kind is not ported: nothing uses it yet.</para>
/// <para>Threading: pure and thread-safe.</para>
/// </remarks>
internal static class EolCounter
{
    /// <summary>Counts the line breaks of <paramref name="text"/> and measures its first and last lines.</summary>
    public static EolCount Count(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int eolCount = 0;
        int firstLineLength = 0;
        int lastLineStart = 0;
        for (int i = 0, len = text.Length; i < len; i++)
        {
            char chr = text[i];
            if (chr == '\r')
            {
                if (eolCount == 0)
                {
                    firstLineLength = i;
                }

                eolCount++;
                if (i + 1 < len && text[i + 1] == '\n')
                {
                    i++;
                }

                lastLineStart = i + 1;
            }
            else if (chr == '\n')
            {
                if (eolCount == 0)
                {
                    firstLineLength = i;
                }

                eolCount++;
                lastLineStart = i + 1;
            }
        }

        if (eolCount == 0)
        {
            firstLineLength = text.Length;
        }

        return new EolCount(eolCount, firstLineLength, text.Length - lastLineStart);
    }
}
