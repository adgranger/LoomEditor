// Ported from microsoft/vscode: src/vs/editor/common/core/misc/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Model;

namespace LoomEditor.Core.Text;

/// <summary>Indentation helpers (Monaco: <c>misc/indentation.ts</c>; only <c>normalizeIndentation</c> is ported).</summary>
/// <remarks>Threading: pure static methods; thread-safe.</remarks>
internal static class Indentation
{
    private static string NormalizeIndentationFromWhitespace(string str, int indentSize, bool insertSpaces)
    {
        var spacesCnt = 0;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] == '\t')
            {
                spacesCnt = CursorColumns.NextIndentTabStop(spacesCnt, indentSize);
            }
            else
            {
                spacesCnt++;
            }
        }

        var result = new StringBuilder();
        if (!insertSpaces)
        {
            var tabsCnt = spacesCnt / indentSize;
            spacesCnt %= indentSize;
            result.Append('\t', tabsCnt);
        }

        result.Append(' ', spacesCnt);
        return result.ToString();
    }

    /// <summary>Rewrites the leading whitespace of <paramref name="str"/> with tabs or spaces only.</summary>
    public static string NormalizeIndentation(string str, int indentSize, bool insertSpaces)
    {
        var firstNonWhitespaceIndex = StringClassification.FirstNonWhitespaceIndex(str);
        if (firstNonWhitespaceIndex == -1)
        {
            firstNonWhitespaceIndex = str.Length;
        }

        return string.Concat(
            NormalizeIndentationFromWhitespace(str.Substring(0, firstNonWhitespaceIndex), indentSize, insertSpaces),
            str.AsSpan(firstNonWhitespaceIndex));
    }
}
