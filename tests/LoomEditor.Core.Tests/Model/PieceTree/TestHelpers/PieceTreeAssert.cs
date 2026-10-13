// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

// Monaco: trimLineFeed, testLinesContent, testLineStarts
internal static class PieceTreeAssert
{
    // Monaco: splitLines (base/common/strings.ts): split on \r\n, \r or \n
    private static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' || text[i] == '\n')
            {
                lines.Add(text[start..i]);
                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }
        }

        lines.Add(text[start..]);
        return lines;
    }

    // Monaco: trimLineFeed
    private static string TrimLineFeed(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        if (text.Length == 1)
        {
            return text[0] is '\n' or '\r' ? "" : text;
        }

        if (text[^1] == '\n')
        {
            return text[^2] == '\r' ? text[..^2] : text[..^1];
        }

        if (text[^1] == '\r')
        {
            return text[..^1];
        }

        return text;
    }

    // Monaco: testLinesContent
    public static void LinesContent(string text, PieceTreeBase tree)
    {
        List<string> lines = SplitLines(text);
        Assert.Equal(lines.Count, tree.GetLineCount());
        Assert.Equal(text, tree.GetLinesRawContent());
        for (int i = 0; i < lines.Count; i++)
        {
            Assert.Equal(lines[i], tree.GetLineContent(i + 1));
            Assert.Equal(
                lines[i],
                TrimLineFeed(tree.GetValueInRange(
                    new Range(i + 1, 1, i + 1, lines[i].Length + (i == lines.Count - 1 ? 1 : 2)))));
        }
    }

    // Monaco: testLineStarts
    public static void LineStarts(string text, PieceTreeBase tree)
    {
        var lineStarts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                i++;
                lineStarts.Add(i + 1);
            }
            else if (text[i] is '\r' or '\n')
            {
                lineStarts.Add(i + 1);
            }
        }

        for (int i = 0; i < lineStarts.Count; i++)
        {
            Assert.Equal(new Position(i + 1, 1), tree.GetPositionAt(lineStarts[i]));
            Assert.Equal(lineStarts[i], tree.GetOffsetAt(i + 1, 1));
        }

        for (int i = 1; i < lineStarts.Count; i++)
        {
            Position pos = tree.GetPositionAt(lineStarts[i] - 1);
            Assert.Equal(lineStarts[i] - 1, tree.GetOffsetAt(pos.LineNumber, pos.Column));
        }
    }
}
