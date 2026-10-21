// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/common/indentUtils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;

namespace LoomEditor.View.Features.Indentation;

/// <summary>Helpers on indentation strings (Monaco: <c>indentUtils.ts</c>).</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class IndentUtils
{
    /// <summary>The width of <paramref name="str"/> in spaces, a tab counting for <paramref name="tabSize"/> (Monaco: <c>getSpaceCnt</c>).</summary>
    public static int GetSpaceCnt(string str, int tabSize)
    {
        int spacesCnt = 0;

        foreach (char c in str)
        {
            spacesCnt += c == (char)9 ? tabSize : 1;
        }

        return spacesCnt;
    }

    /// <summary>An indentation of <paramref name="spacesCnt"/> spaces, with tabs when not <paramref name="insertSpaces"/> (Monaco: <c>generateIndent</c>).</summary>
    public static string GenerateIndent(int spacesCnt, int tabSize, bool insertSpaces)
    {
        spacesCnt = spacesCnt < 0 ? 0 : spacesCnt;

        var result = new StringBuilder();
        if (!insertSpaces)
        {
            int tabsCnt = spacesCnt / tabSize;
            spacesCnt %= tabSize;
            result.Append((char)9, tabsCnt);
        }

        result.Append(' ', spacesCnt);
        return result.ToString();
    }
}
