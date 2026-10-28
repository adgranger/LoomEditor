// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/viewLineRenderer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>Which whitespace characters are drawn as <c>·</c> and <c>→</c> (Monaco: <c>RenderWhitespace</c>).</summary>
internal enum RenderWhitespace
{
    /// <summary>No whitespace is drawn.</summary>
    None,

    /// <summary>Whitespace inside the selections.</summary>
    Selection,

    /// <summary>Leading, trailing and repeated whitespace.</summary>
    Boundary,

    /// <summary>Trailing whitespace only.</summary>
    Trailing,

    /// <summary>All whitespace.</summary>
    All,
}

/// <summary>
/// Decides which characters of a line are drawn as whitespace (the <c>isInWhitespace</c> decision of Monaco's
/// <c>_applyRenderWhitespace</c>). Behavior differences: no <c>LinePart</c>s and no RTL rule (see docs/PORTING.md).
/// </summary>
/// <remarks>Threading: pure, thread-safe.</remarks>
internal static class RenderWhitespaceComputer
{
    /// <summary>
    /// The sorted offsets (below <paramref name="length"/>) of every space or tab to draw. Characters before
    /// <paramref name="fauxIndentLength"/> are never returned.
    /// </summary>
    public static IReadOnlyList<int> Compute(
        string content,
        int length,
        int fauxIndentLength,
        RenderWhitespace mode,
        bool continuesWithWrappedLine,
        IReadOnlyList<OffsetRange>? selectionsOnLine)
    {
        ArgumentNullException.ThrowIfNull(content);

        // viewLineRenderer.ts 497-503
        var run = mode == RenderWhitespace.All
            || mode == RenderWhitespace.Boundary
            || (mode == RenderWhitespace.Selection && selectionsOnLine is not null)
            || (mode == RenderWhitespace.Trailing && !continuesWithWrappedLine);
        if (!run)
        {
            return [];
        }

        length = Math.Min(length, content.Length);
        var onlyBoundary = mode == RenderWhitespace.Boundary;
        var onlyTrailing = mode == RenderWhitespace.Trailing;

        var lineIsEmptyOrWhitespace = false;
        var firstNonWhitespaceIndex = StringClassification.FirstNonWhitespaceIndex(content);
        int lastNonWhitespaceIndex;
        if (firstNonWhitespaceIndex == -1)
        {
            lineIsEmptyOrWhitespace = true;
            firstNonWhitespaceIndex = length;
            lastNonWhitespaceIndex = length;
        }
        else
        {
            lastNonWhitespaceIndex = StringClassification.LastNonWhitespaceIndex(content);
        }

        var selections = selectionsOnLine?.OrderBy(static s => s.Start).ToArray();
        var result = new List<int>();
        var wasInWhitespace = false;
        var currentSelectionIndex = 0;
        for (var charIndex = fauxIndentLength; charIndex < length; charIndex++)
        {
            var ch = content[charIndex];

            while (selections is not null
                && currentSelectionIndex < selections.Length
                && selections[currentSelectionIndex].EndExclusive <= charIndex)
            {
                currentSelectionIndex++;
            }

            bool isInWhitespace;
            if (charIndex < firstNonWhitespaceIndex || charIndex > lastNonWhitespaceIndex)
            {
                isInWhitespace = true;
            }
            else if (ch == '\t')
            {
                isInWhitespace = true;
            }
            else if (ch == ' ')
            {
                if (onlyBoundary)
                {
                    if (wasInWhitespace)
                    {
                        isInWhitespace = true;
                    }
                    else
                    {
                        var next = charIndex + 1 < length ? content[charIndex + 1] : '\0';
                        isInWhitespace = next == ' ' || next == '\t';
                    }
                }
                else
                {
                    isInWhitespace = true;
                }
            }
            else
            {
                isInWhitespace = false;
            }

            if (isInWhitespace && selections is not null)
            {
                isInWhitespace = currentSelectionIndex < selections.Length
                    && selections[currentSelectionIndex].Start <= charIndex
                    && charIndex < selections[currentSelectionIndex].EndExclusive;
            }

            if (isInWhitespace && onlyTrailing)
            {
                isInWhitespace = lineIsEmptyOrWhitespace || charIndex > lastNonWhitespaceIndex;
            }

            if (isInWhitespace && (ch == ' ' || ch == '\t'))
            {
                result.Add(charIndex);
            }

            wasInWhitespace = isInWhitespace;
        }

        // viewLineRenderer.ts 927-940: a wrapped line ends in a single space, which is not drawn in boundary mode
        if (wasInWhitespace && continuesWithWrappedLine && onlyBoundary)
        {
            var last = length > 0 ? content[length - 1] : '\0';
            var prev = length > 1 ? content[length - 2] : '\0';
            if (last == ' ' && prev != ' ' && prev != '\t')
            {
                result.RemoveAt(result.Count - 1);
            }
        }

        return result;
    }

    /// <summary>
    /// The non-empty parts of the selections that lie on a line, as offsets (<c>viewLine.ts</c> 124-150), or null
    /// when there is none.
    /// </summary>
    public static IReadOnlyList<OffsetRange>? SelectionsOnLine(
        IReadOnlyList<Selection> selections,
        int lineNumber,
        int minColumn,
        int maxColumn)
    {
        List<OffsetRange>? result = null;
        foreach (var selection in selections)
        {
            var range = selection.Range;
            if (range.EndLineNumber < lineNumber || range.StartLineNumber > lineNumber)
            {
                continue;
            }

            var startColumn = range.StartLineNumber == lineNumber ? range.StartColumn : minColumn;
            var endColumn = range.EndLineNumber == lineNumber ? range.EndColumn : maxColumn;
            if (startColumn < endColumn)
            {
                (result ??= []).Add(new OffsetRange(startColumn - 1, endColumn - 1));
            }
        }

        return result;
    }
}
