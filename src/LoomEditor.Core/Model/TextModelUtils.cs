// Ported from microsoft/vscode: src/vs/editor/common/model/utils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>Helpers on lines of text (Monaco: <c>common/model/utils.ts</c>).</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class TextModelUtils
{
    /// <summary>
    /// Returns the indent level of a line: -1 when the line only consists of whitespace, the visible column of its first
    /// non-whitespace character otherwise. Monaco: <c>computeIndentLevel</c>.
    /// </summary>
    public static int ComputeIndentLevel(string line, int tabSize)
    {
        ArgumentNullException.ThrowIfNull(line);
        int indent = 0;
        int i = 0;
        int len = line.Length;
        while (i < len)
        {
            char ch = line[i];
            if (ch == ' ')
            {
                indent++;
            }
            else if (ch == '\t')
            {
                indent = indent - (indent % tabSize) + tabSize;
            }
            else
            {
                break;
            }

            i++;
        }

        return i == len ? -1 : indent;
    }
}