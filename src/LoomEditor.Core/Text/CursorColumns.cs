// Ported from microsoft/vscode: src/vs/editor/common/core/cursorColumns.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>
/// Converts between a column (UTF-16 position in a line, starting at 1) and a visible column (where it is drawn, starting at 0:
/// tabs jump to the next tab stop, full-width characters and emoji take 2) (Monaco: <c>CursorColumns</c>).
/// </summary>
/// <remarks>Threading: pure static methods; thread-safe.</remarks>
internal static class CursorColumns
{
    private static int NextVisibleColumn(int codePoint, int visibleColumn, int tabSize)
    {
        if (codePoint == '\t')
        {
            return NextRenderTabStop(visibleColumn, tabSize);
        }

        if (Strings.IsFullWidthCharacter(codePoint) || Strings.IsEmojiImprecise(codePoint))
        {
            return visibleColumn + 2;
        }

        return visibleColumn + 1;
    }

    /// <summary>Returns a visible column from a column.</summary>
    public static int VisibleColumnFromColumn(string lineContent, int column, int tabSize)
    {
        // JS substring clamps a negative end to 0.
        var textLen = Math.Max(0, Math.Min(column - 1, lineContent.Length));
        var text = lineContent.Substring(0, textLen);
        var iterator = new GraphemeIterator(text);

        var result = 0;
        while (!iterator.Eol())
        {
            var codePoint = Strings.GetNextCodePoint(text, textLen, iterator.Offset);
            iterator.NextGraphemeLength();

            result = NextVisibleColumn(codePoint, result, tabSize);
        }

        return result;
    }

    /// <summary>Returns the value to display as "Col" in the status bar.</summary>
    public static int ToStatusbarColumn(string lineContent, int column, int tabSize)
    {
        var text = lineContent.Substring(0, Math.Max(0, Math.Min(column - 1, lineContent.Length)));
        var iterator = new CodePointIterator(text);

        var result = 0;
        while (!iterator.Eol())
        {
            var codePoint = iterator.NextCodePoint();

            if (codePoint == '\t')
            {
                result = NextRenderTabStop(result, tabSize);
            }
            else
            {
                result += 1;
            }
        }

        return result + 1;
    }

    /// <summary>
    /// Returns a column from a visible column. The result can be <c>lineContent.Length + 1</c> at most; it is not clamped to the model.
    /// </summary>
    public static int ColumnFromVisibleColumn(string lineContent, int visibleColumn, int tabSize)
    {
        if (visibleColumn <= 0)
        {
            return 1;
        }

        var lineContentLength = lineContent.Length;
        var iterator = new GraphemeIterator(lineContent);

        var beforeVisibleColumn = 0;
        var beforeColumn = 1;
        while (!iterator.Eol())
        {
            var codePoint = Strings.GetNextCodePoint(lineContent, lineContentLength, iterator.Offset);
            iterator.NextGraphemeLength();

            var afterVisibleColumn = NextVisibleColumn(codePoint, beforeVisibleColumn, tabSize);
            var afterColumn = iterator.Offset + 1;

            if (afterVisibleColumn >= visibleColumn)
            {
                var beforeDelta = visibleColumn - beforeVisibleColumn;
                var afterDelta = afterVisibleColumn - visibleColumn;
                return afterDelta < beforeDelta ? afterColumn : beforeColumn;
            }

            beforeVisibleColumn = afterVisibleColumn;
            beforeColumn = afterColumn;
        }

        // walked the entire string
        return lineContentLength + 1;
    }

    /// <summary>ATTENTION: this works with 0-based columns (as opposed to the regular 1-based columns).</summary>
    public static int NextRenderTabStop(int visibleColumn, int tabSize) => visibleColumn + tabSize - (visibleColumn % tabSize);

    /// <summary>ATTENTION: this works with 0-based columns (as opposed to the regular 1-based columns).</summary>
    public static int NextIndentTabStop(int visibleColumn, int indentSize) => NextRenderTabStop(visibleColumn, indentSize);

    /// <summary>ATTENTION: this works with 0-based columns (as opposed to the regular 1-based columns).</summary>
    public static int PrevRenderTabStop(int column, int tabSize) => Math.Max(0, column - 1 - ((column - 1) % tabSize));

    /// <summary>ATTENTION: this works with 0-based columns (as opposed to the regular 1-based columns).</summary>
    public static int PrevIndentTabStop(int column, int indentSize) => PrevRenderTabStop(column, indentSize);
}
