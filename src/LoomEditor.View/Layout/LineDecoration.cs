// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/lineDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// An inline decoration cut to one line (Monaco: <c>LineDecoration</c>). <c>equalsArr</c>, <c>extractWrapped</c>
/// (word wrap: phase 6) and <c>LineDecorationsNormalizer</c> are not ported.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record LineDecoration(int StartColumn, int EndColumn, string ClassName, InlineDecorationType Type)
{
    /// <summary>The decorations of <paramref name="lineNumber"/>, with columns on that line (Monaco: <c>filter</c>).</summary>
    public static IReadOnlyList<LineDecoration> Filter(IReadOnlyList<InlineDecoration> lineDecorations, int lineNumber, int minLineColumn, int maxLineColumn)
    {
        if (lineDecorations.Count == 0)
        {
            return [];
        }

        var result = new List<LineDecoration>();
        foreach (InlineDecoration d in lineDecorations)
        {
            Range range = d.Range;

            if (range.EndLineNumber < lineNumber || range.StartLineNumber > lineNumber)
            {
                // Ignore decorations that sit outside this line
                continue;
            }

            if (range.IsEmpty && (d.Type == InlineDecorationType.Regular || d.Type == InlineDecorationType.RegularAffectingLetterSpacing))
            {
                // Ignore empty range decorations
                continue;
            }

            int startColumn = range.StartLineNumber == lineNumber ? range.StartColumn : minLineColumn;
            int endColumn = range.EndLineNumber == lineNumber ? range.EndColumn : maxLineColumn;

            result.Add(new LineDecoration(startColumn, endColumn, d.InlineClassName, d.Type));
        }

        return result;
    }
}
