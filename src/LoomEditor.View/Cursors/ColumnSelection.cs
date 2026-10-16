// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorColumnSelection.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The result of a column selection (Monaco: <c>IColumnSelectResult</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record ColumnSelectResult(
    IReadOnlyList<SingleCursorState> ViewStates,
    bool Reversed,
    int FromLineNumber,
    int FromVisualColumn,
    int ToLineNumber,
    int ToVisualColumn);

/// <summary>Selects a rectangle of text: one cursor per line, between the same two visible columns.</summary>
/// <remarks>Threading: pure functions; thread-safe as long as the model is not modified concurrently.</remarks>
internal static class ColumnSelection
{
    public static ColumnSelectResult ColumnSelect(
        CursorConfiguration config,
        ICursorSimpleModel model,
        int fromLineNumber,
        int fromVisibleColumn,
        int toLineNumber,
        int toVisibleColumn)
    {
        int lineCount = Math.Abs(toLineNumber - fromLineNumber) + 1;
        bool reversed = fromLineNumber > toLineNumber;
        bool isRtl = fromVisibleColumn > toVisibleColumn;
        bool isLtr = fromVisibleColumn < toVisibleColumn;

        var result = new List<SingleCursorState>();

        for (int i = 0; i < lineCount; i++)
        {
            int lineNumber = fromLineNumber + (reversed ? -i : i);

            int startColumn = config.ColumnFromVisibleColumn(model, lineNumber, fromVisibleColumn);
            int endColumn = config.ColumnFromVisibleColumn(model, lineNumber, toVisibleColumn);
            int visibleStartColumn = config.VisibleColumnFromColumn(model, new Position(lineNumber, startColumn));
            int visibleEndColumn = config.VisibleColumnFromColumn(model, new Position(lineNumber, endColumn));

            if (isLtr)
            {
                if (visibleStartColumn > toVisibleColumn)
                {
                    continue;
                }

                if (visibleEndColumn < fromVisibleColumn)
                {
                    continue;
                }
            }

            if (isRtl)
            {
                if (visibleEndColumn > fromVisibleColumn)
                {
                    continue;
                }

                if (visibleStartColumn < toVisibleColumn)
                {
                    continue;
                }
            }

            result.Add(new SingleCursorState(
                new Range(lineNumber, startColumn, lineNumber, startColumn),
                SelectionStartKind.Simple,
                0,
                new Position(lineNumber, endColumn),
                0));
        }

        if (result.Count == 0)
        {
            // We are after all the lines, so add cursor at the end of each line
            for (int i = 0; i < lineCount; i++)
            {
                int lineNumber = fromLineNumber + (reversed ? -i : i);
                int maxColumn = model.GetLineMaxColumn(lineNumber);

                result.Add(new SingleCursorState(
                    new Range(lineNumber, maxColumn, lineNumber, maxColumn),
                    SelectionStartKind.Simple,
                    0,
                    new Position(lineNumber, maxColumn),
                    0));
            }
        }

        return new ColumnSelectResult(result, reversed, fromLineNumber, fromVisibleColumn, toLineNumber, toVisibleColumn);
    }

    public static ColumnSelectResult ColumnSelectLeft(CursorConfiguration config, ICursorSimpleModel model, ColumnSelectData prevColumnSelectData)
    {
        int toViewVisualColumn = prevColumnSelectData.ToViewVisualColumn;
        if (toViewVisualColumn > 0)
        {
            toViewVisualColumn--;
        }

        return ColumnSelect(
            config,
            model,
            prevColumnSelectData.FromViewLineNumber,
            prevColumnSelectData.FromViewVisualColumn,
            prevColumnSelectData.ToViewLineNumber,
            toViewVisualColumn);
    }

    public static ColumnSelectResult ColumnSelectRight(CursorConfiguration config, ICursorSimpleModel model, ColumnSelectData prevColumnSelectData)
    {
        int maxVisualViewColumn = 0;
        int minViewLineNumber = Math.Min(prevColumnSelectData.FromViewLineNumber, prevColumnSelectData.ToViewLineNumber);
        int maxViewLineNumber = Math.Max(prevColumnSelectData.FromViewLineNumber, prevColumnSelectData.ToViewLineNumber);
        for (int lineNumber = minViewLineNumber; lineNumber <= maxViewLineNumber; lineNumber++)
        {
            int lineMaxViewColumn = model.GetLineMaxColumn(lineNumber);
            int lineMaxVisualViewColumn = config.VisibleColumnFromColumn(model, new Position(lineNumber, lineMaxViewColumn));
            maxVisualViewColumn = Math.Max(maxVisualViewColumn, lineMaxVisualViewColumn);
        }

        int toViewVisualColumn = prevColumnSelectData.ToViewVisualColumn;
        if (toViewVisualColumn < maxVisualViewColumn)
        {
            toViewVisualColumn++;
        }

        return ColumnSelect(
            config,
            model,
            prevColumnSelectData.FromViewLineNumber,
            prevColumnSelectData.FromViewVisualColumn,
            prevColumnSelectData.ToViewLineNumber,
            toViewVisualColumn);
    }

    public static ColumnSelectResult ColumnSelectUp(CursorConfiguration config, ICursorSimpleModel model, ColumnSelectData prevColumnSelectData, bool isPaged)
    {
        int linesCount = isPaged ? config.PageSize : 1;
        int toViewLineNumber = Math.Max(1, prevColumnSelectData.ToViewLineNumber - linesCount);
        return ColumnSelect(
            config,
            model,
            prevColumnSelectData.FromViewLineNumber,
            prevColumnSelectData.FromViewVisualColumn,
            toViewLineNumber,
            prevColumnSelectData.ToViewVisualColumn);
    }

    public static ColumnSelectResult ColumnSelectDown(CursorConfiguration config, ICursorSimpleModel model, ColumnSelectData prevColumnSelectData, bool isPaged)
    {
        int linesCount = isPaged ? config.PageSize : 1;
        int toViewLineNumber = Math.Min(model.GetLineCount(), prevColumnSelectData.ToViewLineNumber + linesCount);
        return ColumnSelect(
            config,
            model,
            prevColumnSelectData.FromViewLineNumber,
            prevColumnSelectData.FromViewVisualColumn,
            toViewLineNumber,
            prevColumnSelectData.ToViewVisualColumn);
    }
}
