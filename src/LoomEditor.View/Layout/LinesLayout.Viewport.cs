// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/linesLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

// Viewport queries: visible lines with relative offsets, and the whitespaces in view.
internal sealed partial class LinesLayout
{
    /// <summary>
    /// Get all the lines and their relative vertical offsets that are positioned between
    /// <paramref name="verticalOffset1"/> and <paramref name="verticalOffset2"/>.
    /// </summary>
    /// <param name="verticalOffset1">The beginning of the viewport.</param>
    /// <param name="verticalOffset2">The end of the viewport.</param>
    public PartialViewLinesViewportData GetLinesViewportData(int verticalOffset1, int verticalOffset2)
    {
        // Find first line number
        // The line number might start before or after verticalOffset1
        var startLineNumber = GetLineNumberAtOrAfterVerticalOffset(verticalOffset1);
        var startLineNumberVerticalOffset = GetVerticalOffsetForLineNumber(startLineNumber);

        var endLineNumber = _lineCount;

        // Also keep track of what whitespace we've got
        var whitespaceIndex = GetFirstWhitespaceIndexAfterLineNumber(startLineNumber);
        var whitespaceCount = GetWhitespacesCount();
        int currentWhitespaceHeight;
        int currentWhitespaceAfterLineNumber;

        if (whitespaceIndex == -1)
        {
            whitespaceIndex = whitespaceCount;
            currentWhitespaceAfterLineNumber = endLineNumber + 1;
            currentWhitespaceHeight = 0;
        }
        else
        {
            currentWhitespaceAfterLineNumber = GetAfterLineNumberForWhitespaceIndex(whitespaceIndex);
            currentWhitespaceHeight = GetHeightForWhitespaceIndex(whitespaceIndex);
        }

        var currentVerticalOffset = startLineNumberVerticalOffset;
        var currentLineRelativeOffset = currentVerticalOffset;

        // IE (all versions) cannot handle units above about 1,533,908 px, so every 500k pixels bring numbers down
        const int StepSize = 500000;
        var bigNumbersDelta = 0;
        if (startLineNumberVerticalOffset >= StepSize)
        {
            // Compute a delta that guarantees that lines are positioned at `lineHeight` increments
            bigNumbersDelta = startLineNumberVerticalOffset / StepSize * StepSize;
            bigNumbersDelta = bigNumbersDelta / _lineHeightsManager.DefaultLineHeight * _lineHeightsManager.DefaultLineHeight;

            currentLineRelativeOffset -= bigNumbersDelta;
        }

        var linesOffsets = new List<int>();

        // Not an integer in Monaco either (JavaScript numbers).
        var verticalCenter = verticalOffset1 + (verticalOffset2 - verticalOffset1) / 2.0;
        var centeredLineNumber = -1;

        // Figure out how far the lines go
        for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            var lineHeight = GetLineHeightForLineNumber(lineNumber);
            if (centeredLineNumber == -1)
            {
                var currentLineTop = currentVerticalOffset;
                var currentLineBottom = currentVerticalOffset + lineHeight;
                if ((currentLineTop <= verticalCenter && verticalCenter < currentLineBottom) || currentLineTop > verticalCenter)
                {
                    centeredLineNumber = lineNumber;
                }
            }

            // Count current line height in the vertical offsets
            currentVerticalOffset += lineHeight;
            linesOffsets.Add(currentLineRelativeOffset);

            // Next line starts immediately after this one
            currentLineRelativeOffset += lineHeight;
            while (currentWhitespaceAfterLineNumber == lineNumber)
            {
                // Push down next line with the height of the current whitespace
                currentLineRelativeOffset += currentWhitespaceHeight;

                // Count current whitespace in the vertical offsets
                currentVerticalOffset += currentWhitespaceHeight;
                whitespaceIndex++;

                if (whitespaceIndex >= whitespaceCount)
                {
                    currentWhitespaceAfterLineNumber = endLineNumber + 1;
                }
                else
                {
                    currentWhitespaceAfterLineNumber = GetAfterLineNumberForWhitespaceIndex(whitespaceIndex);
                    currentWhitespaceHeight = GetHeightForWhitespaceIndex(whitespaceIndex);
                }
            }

            if (currentVerticalOffset >= verticalOffset2)
            {
                // We have covered the entire viewport area, time to stop
                endLineNumber = lineNumber;
                break;
            }
        }

        if (centeredLineNumber == -1)
        {
            centeredLineNumber = endLineNumber;
        }

        var endLineNumberVerticalOffset = GetVerticalOffsetForLineNumber(endLineNumber);

        var completelyVisibleStartLineNumber = startLineNumber;
        var completelyVisibleEndLineNumber = endLineNumber;

        if (completelyVisibleStartLineNumber < completelyVisibleEndLineNumber)
        {
            if (startLineNumberVerticalOffset < verticalOffset1)
            {
                completelyVisibleStartLineNumber++;
            }
        }

        if (completelyVisibleStartLineNumber < completelyVisibleEndLineNumber)
        {
            var endLineHeight = GetLineHeightForLineNumber(endLineNumber);
            if (endLineNumberVerticalOffset + endLineHeight > verticalOffset2)
            {
                completelyVisibleEndLineNumber--;
            }
        }

        return new PartialViewLinesViewportData(
            bigNumbersDelta,
            startLineNumber,
            endLineNumber,
            [.. linesOffsets],
            centeredLineNumber,
            completelyVisibleStartLineNumber,
            completelyVisibleEndLineNumber,
            _lineHeightsManager.DefaultLineHeight);
    }

    /// <summary>Get exactly the whitespace that is laid out at <paramref name="verticalOffset"/>.</summary>
    /// <returns>Precisely the whitespace that is laid out at <paramref name="verticalOffset"/>, or <c>null</c>.</returns>
    public ViewWhitespaceViewportData? GetWhitespaceAtVerticalOffset(int verticalOffset)
    {
        var candidateIndex = GetWhitespaceIndexAtOrAfterVerticallOffset(verticalOffset);

        if (candidateIndex < 0)
        {
            return null;
        }

        if (candidateIndex >= GetWhitespacesCount())
        {
            return null;
        }

        var candidateTop = GetVerticalOffsetForWhitespaceIndex(candidateIndex);

        if (candidateTop > verticalOffset)
        {
            return null;
        }

        return new ViewWhitespaceViewportData(
            GetIdForWhitespaceIndex(candidateIndex),
            GetAfterLineNumberForWhitespaceIndex(candidateIndex),
            candidateTop,
            GetHeightForWhitespaceIndex(candidateIndex));
    }

    /// <summary>
    /// Get a list of whitespaces that are positioned between <paramref name="verticalOffset1"/> and
    /// <paramref name="verticalOffset2"/>.
    /// </summary>
    /// <returns>All the whitespaces in the viewport; empty if none is in view.</returns>
    public IReadOnlyList<ViewWhitespaceViewportData> GetWhitespaceViewportData(int verticalOffset1, int verticalOffset2)
    {
        var startIndex = GetWhitespaceIndexAtOrAfterVerticallOffset(verticalOffset1);
        var endIndex = GetWhitespacesCount() - 1;

        if (startIndex < 0)
        {
            return [];
        }

        var result = new List<ViewWhitespaceViewportData>();
        for (var i = startIndex; i <= endIndex; i++)
        {
            var top = GetVerticalOffsetForWhitespaceIndex(i);
            var height = GetHeightForWhitespaceIndex(i);
            if (top >= verticalOffset2)
            {
                break;
            }

            result.Add(new ViewWhitespaceViewportData(
                GetIdForWhitespaceIndex(i),
                GetAfterLineNumberForWhitespaceIndex(i),
                top,
                height));
        }

        return result;
    }
}
