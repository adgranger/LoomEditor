// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorAtomicMoveOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>Which way an atomic tab move goes (Monaco: <c>Direction</c> of <c>cursorAtomicMoveOperations.ts</c>).</summary>
internal enum AtomicMoveDirection
{
    Left = 0,
    Right = 1,
    Nearest = 2,
}

/// <summary>With <c>stickyTabStops</c>, makes indentation made of spaces behave like tabs (Monaco: <c>AtomicTabMoveOperations</c>).</summary>
/// <remarks>Positions and return values are 0-based, and -1 means "not applicable", like Monaco. Threading: pure functions; thread-safe.</remarks>
internal static class AtomicTabMoveOperations
{
    /// <summary>
    /// Gets the visible column at the position. If a non-whitespace character comes first, or the position is past
    /// the end of the string, returns <c>(-1, -1, -1)</c>.
    /// </summary>
    /// <remarks><paramref name="position"/> and the returned values are 0-based.</remarks>
    public static (int PrevTabStopPosition, int PrevTabStopVisibleColumn, int VisibleColumn) WhitespaceVisibleColumn(
        string lineContent,
        int position,
        int tabSize)
    {
        int lineLength = lineContent.Length;
        int visibleColumn = 0;
        int prevTabStopPosition = -1;
        int prevTabStopVisibleColumn = -1;
        for (int i = 0; i < lineLength; i++)
        {
            if (i == position)
            {
                return (prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn);
            }

            if (visibleColumn % tabSize == 0)
            {
                prevTabStopPosition = i;
                prevTabStopVisibleColumn = visibleColumn;
            }

            switch (lineContent[i])
            {
                case ' ':
                    visibleColumn += 1;
                    break;
                case '\t':
                    // Skip to the next multiple of tabSize.
                    visibleColumn = CursorColumns.NextRenderTabStop(visibleColumn, tabSize);
                    break;
                default:
                    return (-1, -1, -1);
            }
        }

        if (position == lineLength)
        {
            return (prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn);
        }

        return (-1, -1, -1);
    }

    /// <summary>
    /// Returns the position that should result from a move left, right or to the nearest tab, if atomic tabs are
    /// enabled. Left and right are used for the arrow keys, nearest for mouse selection. Returns -1 if atomic tabs are
    /// not relevant and the normal behavior should be used.
    /// </summary>
    /// <remarks><paramref name="position"/> and the return value are 0-based.</remarks>
    public static int AtomicPosition(string lineContent, int position, int tabSize, AtomicMoveDirection direction)
    {
        int lineLength = lineContent.Length;

        // Get the 0-based visible column corresponding to the position, or return
        // -1 if it is not in the initial whitespace.
        (int prevTabStopPosition, int prevTabStopVisibleColumn, int visibleColumn) = WhitespaceVisibleColumn(lineContent, position, tabSize);

        if (visibleColumn == -1)
        {
            return -1;
        }

        // Is the output left or right of the current position. The case for nearest
        // where it is the same as the current position is handled in the switch.
        bool left;
        switch (direction)
        {
            case AtomicMoveDirection.Left:
                left = true;
                break;
            case AtomicMoveDirection.Right:
                left = false;
                break;
            default:
                // The code below assumes the output position is either left or right
                // of the input position. If it is the same, return immediately.
                if (visibleColumn % tabSize == 0)
                {
                    return position;
                }

                // Go to the nearest indentation. (JavaScript: tabSize / 2 is a floating point division.)
                left = visibleColumn % tabSize <= tabSize / 2.0;
                break;
        }

        // If going left, we can just use the info about the last tab stop position and
        // last tab stop visible column that we computed in the first walk over the whitespace.
        if (left)
        {
            if (prevTabStopPosition == -1)
            {
                return -1;
            }

            // If the direction is left, we need to keep scanning right to ensure
            // that targetVisibleColumn + tabSize is before non-whitespace.
            // This is so that when we press left at the end of a partial
            // indentation it only goes one character. For example "      foo" with
            // tabSize 4, should jump from position 6 to position 5, not 4.
            int leftVisibleColumn = prevTabStopVisibleColumn;
            for (int i = prevTabStopPosition; i < lineLength; ++i)
            {
                if (leftVisibleColumn == prevTabStopVisibleColumn + tabSize)
                {
                    // It is a full indentation.
                    return prevTabStopPosition;
                }

                switch (lineContent[i])
                {
                    case ' ':
                        leftVisibleColumn += 1;
                        break;
                    case '\t':
                        leftVisibleColumn = CursorColumns.NextRenderTabStop(leftVisibleColumn, tabSize);
                        break;
                    default:
                        return -1;
                }
            }

            if (leftVisibleColumn == prevTabStopVisibleColumn + tabSize)
            {
                return prevTabStopPosition;
            }

            // It must have been a partial indentation.
            return -1;
        }

        // We are going right.
        int targetVisibleColumn = CursorColumns.NextRenderTabStop(visibleColumn, tabSize);

        // We can just continue from where WhitespaceVisibleColumn got to.
        int currentVisibleColumn = visibleColumn;
        for (int i = position; i < lineLength; i++)
        {
            if (currentVisibleColumn == targetVisibleColumn)
            {
                return i;
            }

            switch (lineContent[i])
            {
                case ' ':
                    currentVisibleColumn += 1;
                    break;
                case '\t':
                    currentVisibleColumn = CursorColumns.NextRenderTabStop(currentVisibleColumn, tabSize);
                    break;
                default:
                    return -1;
            }
        }

        // This condition handles when the target column is at the end of the line.
        if (currentVisibleColumn == targetVisibleColumn)
        {
            return lineLength;
        }

        return -1;
    }
}
