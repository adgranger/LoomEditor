// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/lineSequence.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

/// <summary>The lines of a text as a sequence of trimmed-line hashes, for the first (line) diff pass (Monaco: <c>LineSequence</c>).</summary>
/// <remarks>Threading: immutable; callers must not mutate the arrays.</remarks>
internal sealed class LineSequence(int[] trimmedHash, string[] lines) : ISequence
{
    /// <inheritdoc />
    public int Length => trimmedHash.Length;

    /// <inheritdoc />
    public int GetElement(int offset) => trimmedHash[offset];

    /// <inheritdoc />
    public int GetBoundaryScore(int length)
    {
        int indentationBefore = length == 0 ? 0 : GetIndentation(lines[length - 1]);
        int indentationAfter = length == lines.Length ? 0 : GetIndentation(lines[length]);
        return 1000 - (indentationBefore + indentationAfter);
    }

    /// <summary><c>getText</c>: the lines of <paramref name="range"/> joined with <c>\n</c>.</summary>
    public string GetText(OffsetRange range) => string.Join('\n', lines, range.Start, range.Length);

    /// <inheritdoc />
    public bool IsStronglyEqual(int offset1, int offset2) => lines[offset1] == lines[offset2];

    private static int GetIndentation(string str)
    {
        int i = 0;
        while (i < str.Length && (str[i] == ' ' || str[i] == '\t'))
        {
            i++;
        }

        return i;
    }
}
