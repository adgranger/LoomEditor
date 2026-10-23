// Ported from microsoft/vscode: src/vs/editor/common/viewModel/monospaceLineBreaksComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

internal static partial class MonospaceLineBreaksComputer
{
    /// <summary>
    /// The line breaks of a line whose wrapping column changed, computed from its previous breaks (the resize fast path).
    /// Monaco updates <c>previousBreakingData</c> in place and reuses pooled arrays; here the result is a new instance
    /// (the pools are not ported, the arrays are local) and the previous data is left untouched.
    /// </summary>
    public static ModelLineProjectionData? CreateLineBreaksFromPreviousLineBreaks(
        WrappingCharacterClassifier classifier,
        ModelLineProjectionData previousBreakingData,
        string lineText,
        int tabSize,
        int firstLineBreakColumn,
        double columnsForFullWidthChar,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool useTwoCellFullwidthCharacters)
    {
        if (firstLineBreakColumn == -1)
        {
            return null;
        }

        int len = lineText.Length;
        if (len <= 1)
        {
            return null;
        }

        bool isKeepAll = wordBreak == WordBreak.KeepAll;

        int[] prevBreakingOffsets = previousBreakingData.BreakOffsets;
        double[] prevBreakingOffsetsVisibleColumn = previousBreakingData.BreakOffsetsVisibleColumn;

        int wrappedTextIndentLength = ComputeWrappedTextIndentLength(lineText, tabSize, firstLineBreakColumn, FullWidthCharacterColumns(columnsForFullWidthChar, useTwoCellFullwidthCharacters), wrappingIndent);
        int wrappedLineBreakColumn = firstLineBreakColumn - wrappedTextIndentLength;

        var breakingOffsets = new List<int>();
        var breakingOffsetsVisibleColumn = new List<double>();
        int lastBreakingOffset = 0;
        double lastBreakingOffsetVisibleColumn = 0;

        double breakingColumn = firstLineBreakColumn;
        int prevLen = prevBreakingOffsets.Length;
        int prevIndex = 0;

        // JavaScript reads past the end of an array or a string as undefined / NaN; C# throws, so those reads are guarded.
        double CharWidthAt(int offset, double visibleColumn)
        {
            if (offset >= len)
            {
                // Monaco: computeCharWidth(NaN, ...) is 1
                return 1;
            }

            return ComputeCharWidth(lineText[offset], visibleColumn, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
        }

        if (prevIndex >= 0)
        {
            double bestDistance = Math.Abs(prevBreakingOffsetsVisibleColumn[prevIndex] - breakingColumn);
            while (prevIndex + 1 < prevLen)
            {
                double distance = Math.Abs(prevBreakingOffsetsVisibleColumn[prevIndex + 1] - breakingColumn);
                if (distance >= bestDistance)
                {
                    break;
                }

                bestDistance = distance;
                prevIndex++;
            }
        }

        while (prevIndex < prevLen)
        {
            // Allow for prevIndex to be -1 (for the case where we hit a tab when walking backwards from the first break)
            int prevBreakOffset = prevIndex < 0 ? 0 : prevBreakingOffsets[prevIndex];
            double prevBreakOffsetVisibleColumn = prevIndex < 0 ? 0 : prevBreakingOffsetsVisibleColumn[prevIndex];
            if (lastBreakingOffset > prevBreakOffset)
            {
                prevBreakOffset = lastBreakingOffset;
                prevBreakOffsetVisibleColumn = lastBreakingOffsetVisibleColumn;
            }

            int breakOffset = 0;
            double breakOffsetVisibleColumn = 0;

            int forcedBreakOffset = 0;
            double forcedBreakOffsetVisibleColumn = 0;

            // initially, we search as much as possible to the right (if it fits)
            if (prevBreakOffsetVisibleColumn <= breakingColumn)
            {
                double visibleColumn = prevBreakOffsetVisibleColumn;
                int prevCharCode = prevBreakOffset == 0 ? 0 : lineText[prevBreakOffset - 1];
                CharacterClass prevCharCodeClass = prevBreakOffset == 0 ? CharacterClass.None : classifier.Get(prevCharCode);
                bool entireLineFits = true;
                for (int i = prevBreakOffset; i < len; i++)
                {
                    int charStartOffset = i;
                    int charCode = lineText[i];
                    CharacterClass charCodeClass;
                    double charWidth;

                    if (Strings.IsHighSurrogate(charCode))
                    {
                        // A surrogate pair must always be considered as a single unit, so it is never to be broken
                        i++;
                        charCodeClass = CharacterClass.None;
                        charWidth = 2;
                    }
                    else
                    {
                        charCodeClass = classifier.Get(charCode);
                        charWidth = ComputeCharWidth(charCode, visibleColumn, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
                    }

                    if (charStartOffset > lastBreakingOffset && CanBreak(prevCharCode, prevCharCodeClass, charCode, charCodeClass, isKeepAll))
                    {
                        breakOffset = charStartOffset;
                        breakOffsetVisibleColumn = visibleColumn;
                    }

                    visibleColumn += charWidth;

                    // check if adding character at `i` will go over the breaking column
                    if (visibleColumn > breakingColumn)
                    {
                        // We need to break at least before character at `i`:
                        if (charStartOffset > lastBreakingOffset)
                        {
                            forcedBreakOffset = charStartOffset;
                            forcedBreakOffsetVisibleColumn = visibleColumn - charWidth;
                        }
                        else
                        {
                            // we need to advance at least by one character
                            forcedBreakOffset = i + 1;
                            forcedBreakOffsetVisibleColumn = visibleColumn;
                        }

                        if (visibleColumn - breakOffsetVisibleColumn > wrappedLineBreakColumn)
                        {
                            // Cannot break at `breakOffset` => reset it if it was set
                            breakOffset = 0;
                        }

                        entireLineFits = false;
                        break;
                    }

                    prevCharCode = charCode;
                    prevCharCodeClass = charCodeClass;
                }

                if (entireLineFits)
                {
                    // there is no more need to break => stop the outer loop!
                    if (breakingOffsets.Count > 0)
                    {
                        // Add last segment, no need to assign to `lastBreakingOffset` and `lastBreakingOffsetVisibleColumn`
                        breakingOffsets.Add(prevBreakingOffsets[^1]);
                        breakingOffsetsVisibleColumn.Add(prevBreakingOffsetsVisibleColumn[^1]);
                    }

                    break;
                }
            }

            if (breakOffset == 0)
            {
                // must search left
                double visibleColumn = prevBreakOffsetVisibleColumn;
                // Monaco: charCodeAt past the end is NaN, which the classifier maps to the default class
                int charCode = prevBreakOffset < len ? lineText[prevBreakOffset] : -1;
                CharacterClass charCodeClass = classifier.Get(charCode);
                bool hitATabCharacter = false;
                for (int i = prevBreakOffset - 1; i >= lastBreakingOffset; i--)
                {
                    int charStartOffset = i + 1;
                    int prevCharCode = lineText[i];

                    if (prevCharCode == '\t')
                    {
                        // cannot determine the width of a tab when going backwards, so we must go forwards
                        hitATabCharacter = true;
                        break;
                    }

                    CharacterClass prevCharCodeClass;
                    double prevCharWidth;

                    if (Strings.IsLowSurrogate(prevCharCode))
                    {
                        // A surrogate pair must always be considered as a single unit, so it is never to be broken
                        i--;
                        prevCharCodeClass = CharacterClass.None;
                        prevCharWidth = 2;
                    }
                    else
                    {
                        prevCharCodeClass = classifier.Get(prevCharCode);
                        prevCharWidth = Strings.IsFullWidthCharacter(prevCharCode) ? FullWidthCharacterColumns(columnsForFullWidthChar, useTwoCellFullwidthCharacters) : 1;
                    }

                    if (visibleColumn <= breakingColumn)
                    {
                        if (forcedBreakOffset == 0)
                        {
                            forcedBreakOffset = charStartOffset;
                            forcedBreakOffsetVisibleColumn = visibleColumn;
                        }

                        if (visibleColumn <= breakingColumn - wrappedLineBreakColumn)
                        {
                            // went too far!
                            break;
                        }

                        if (CanBreak(prevCharCode, prevCharCodeClass, charCode, charCodeClass, isKeepAll))
                        {
                            breakOffset = charStartOffset;
                            breakOffsetVisibleColumn = visibleColumn;
                            break;
                        }
                    }

                    visibleColumn -= prevCharWidth;
                    charCode = prevCharCode;
                    charCodeClass = prevCharCodeClass;
                }

                if (breakOffset != 0)
                {
                    double remainingWidthOfNextLine = wrappedLineBreakColumn - (forcedBreakOffsetVisibleColumn - breakOffsetVisibleColumn);
                    if (remainingWidthOfNextLine <= tabSize)
                    {
                        int charCodeAtForcedBreakOffset = forcedBreakOffset < len ? lineText[forcedBreakOffset] : -1;
                        double charWidth;
                        if (Strings.IsHighSurrogate(charCodeAtForcedBreakOffset))
                        {
                            // A surrogate pair must always be considered as a single unit, so it is never to be broken
                            charWidth = 2;
                        }
                        else
                        {
                            charWidth = CharWidthAt(forcedBreakOffset, forcedBreakOffsetVisibleColumn);
                        }

                        if (remainingWidthOfNextLine - charWidth < 0)
                        {
                            // it is not worth it to break at breakOffset, it just introduces an extra needless line!
                            breakOffset = 0;
                        }
                    }
                }

                if (hitATabCharacter)
                {
                    // cannot determine the width of a tab when going backwards, so we must go forwards from the previous break
                    prevIndex--;
                    continue;
                }
            }

            if (breakOffset == 0)
            {
                // Could not find a good breaking point
                breakOffset = forcedBreakOffset;
                breakOffsetVisibleColumn = forcedBreakOffsetVisibleColumn;
            }

            if (breakOffset <= lastBreakingOffset)
            {
                // Make sure that we are advancing (at least one character)
                if (lastBreakingOffset < len && Strings.IsHighSurrogate(lineText[lastBreakingOffset]))
                {
                    // A surrogate pair must always be considered as a single unit, so it is never to be broken
                    breakOffset = lastBreakingOffset + 2;
                    breakOffsetVisibleColumn = lastBreakingOffsetVisibleColumn + 2;
                }
                else
                {
                    breakOffset = lastBreakingOffset + 1;
                    breakOffsetVisibleColumn = lastBreakingOffsetVisibleColumn + CharWidthAt(lastBreakingOffset, lastBreakingOffsetVisibleColumn);
                }
            }

            lastBreakingOffset = breakOffset;
            breakingOffsets.Add(breakOffset);
            lastBreakingOffsetVisibleColumn = breakOffsetVisibleColumn;
            breakingOffsetsVisibleColumn.Add(breakOffsetVisibleColumn);
            breakingColumn = breakOffsetVisibleColumn + wrappedLineBreakColumn;

            while (prevIndex < 0 || (prevIndex < prevLen && prevBreakingOffsetsVisibleColumn[prevIndex] < breakOffsetVisibleColumn))
            {
                prevIndex++;
            }

            // Monaco reads NaN when prevIndex reached prevLen: nothing left to compare
            if (prevIndex < prevLen)
            {
                double bestDistance = Math.Abs(prevBreakingOffsetsVisibleColumn[prevIndex] - breakingColumn);
                while (prevIndex + 1 < prevLen)
                {
                    double distance = Math.Abs(prevBreakingOffsetsVisibleColumn[prevIndex + 1] - breakingColumn);
                    if (distance >= bestDistance)
                    {
                        break;
                    }

                    bestDistance = distance;
                    prevIndex++;
                }
            }
        }

        if (breakingOffsets.Count == 0)
        {
            return null;
        }

        return new ModelLineProjectionData(null, null, [.. breakingOffsets], [.. breakingOffsetsVisibleColumn], wrappedTextIndentLength);
    }
}
