// Ported from microsoft/vscode: src/vs/editor/common/viewModel/monospaceLineBreaksComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>Creates the monospace line-breaks computers (Monaco: <c>MonospaceLineBreaksComputerFactory</c>).</summary>
/// <remarks>Threading: immutable after construction (the classifier is not modified); safe from any thread.</remarks>
internal sealed class MonospaceLineBreaksComputerFactory : ILineBreaksComputerFactory
{
    private readonly WrappingCharacterClassifier _classifier;

    public MonospaceLineBreaksComputerFactory(string breakBeforeChars, string breakAfterChars)
    {
        _classifier = new WrappingCharacterClassifier(breakBeforeChars, breakAfterChars);
    }

    public static MonospaceLineBreaksComputerFactory Create(EditorWrappingOptions options) =>
        new(options.WordWrapBreakBeforeCharacters, options.WordWrapBreakAfterCharacters);

    public ILineBreaksComputer CreateLineBreaksComputer(
        ILineBreaksComputerContext context,
        FontInfo fontInfo,
        int tabSize,
        int wrappingColumn,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool wrapOnEscapedLineFeeds,
        bool useTwoCellFullwidthCharacters) =>
        new Computer(_classifier, context, fontInfo, tabSize, wrappingColumn, wrappingIndent, wordBreak, wrapOnEscapedLineFeeds, useTwoCellFullwidthCharacters);

    private sealed class Computer(
        WrappingCharacterClassifier classifier,
        ILineBreaksComputerContext context,
        FontInfo fontInfo,
        int tabSize,
        int wrappingColumn,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool wrapOnEscapedLineFeeds,
        bool useTwoCellFullwidthCharacters) : ILineBreaksComputer
    {
        private readonly List<int> _lineNumbers = [];
        private readonly List<ModelLineProjectionData?> _previousBreakingData = [];

        public void AddRequest(int lineNumber, ModelLineProjectionData? previousLineBreakData)
        {
            _lineNumbers.Add(lineNumber);
            _previousBreakingData.Add(previousLineBreakData);
        }

        public ModelLineProjectionData?[] Finalize()
        {
            double columnsForFullWidthChar = fontInfo.TypicalFullwidthCharacterWidth / fontInfo.TypicalHalfwidthCharacterWidth;
            var result = new ModelLineProjectionData?[_lineNumbers.Count];
            for (int i = 0; i < result.Length; i++)
            {
                int lineNumber = _lineNumbers[i];
                var injectedText = context.GetLineInjectedText(lineNumber);
                string lineText = context.GetLineContent(lineNumber);
                bool isLineFeedWrappingEnabled = wrapOnEscapedLineFeeds && lineText.Contains('"') && lineText.Contains("\\n", StringComparison.Ordinal);

                // P6-12: createLineBreaksFromPreviousLineBreaks
                result[i] = MonospaceLineBreaksComputer.CreateLineBreaks(classifier, lineText, injectedText, tabSize, wrappingColumn, columnsForFullWidthChar, fontInfo, wrappingIndent, wordBreak, isLineFeedWrappingEnabled, useTwoCellFullwidthCharacters);
            }

            return result;
        }
    }
}

internal static partial class MonospaceLineBreaksComputer
{
    /// <summary>The line breaks of a line with its injected text, or null when nothing wraps and nothing is injected.</summary>
    public static ModelLineProjectionData? CreateLineBreaks(
        WrappingCharacterClassifier classifier,
        string rawLineText,
        IReadOnlyList<LineInjectedText>? injectedTexts,
        int tabSize,
        int firstLineBreakColumn,
        double columnsForFullWidthChar,
        FontInfo fontInfo,
        WrappingIndent wrappingIndent,
        WordBreak wordBreak,
        bool wrapOnEscapedLineFeeds,
        bool useTwoCellFullwidthCharacters)
    {
        string lineText = LineInjectedText.ApplyInjectedText(rawLineText, injectedTexts);
        var fixedWidthRanges = LineInjectedText.GetFixedWidthInjectedTextRanges(injectedTexts);

        Core.Decorations.ModelDecorationInjectedTextOptions[]? injectionOptions;
        int[]? injectionOffsets;
        if (injectedTexts is { Count: > 0 })
        {
            injectionOptions = [.. injectedTexts.Select(t => t.Options)];
            injectionOffsets = [.. injectedTexts.Select(t => t.Column - 1)];
        }
        else
        {
            injectionOptions = null;
            injectionOffsets = null;
        }

        if (firstLineBreakColumn == -1)
        {
            // An invalid BreakOffsetsVisibleColumn is OK: it is never used when the line contains injected text.
            return injectionOptions is null ? null : new ModelLineProjectionData(injectionOffsets, injectionOptions, [lineText.Length], [], 0);
        }

        int len = lineText.Length;
        if (len <= 1)
        {
            return injectionOptions is null ? null : new ModelLineProjectionData(injectionOffsets, injectionOptions, [lineText.Length], [], 0);
        }

        bool isKeepAll = wordBreak == WordBreak.KeepAll;
        int wrappedTextIndentLength = ComputeWrappedTextIndentLength(lineText, tabSize, firstLineBreakColumn, FullWidthCharacterColumns(columnsForFullWidthChar, useTwoCellFullwidthCharacters), wrappingIndent, fixedWidthRanges);

        // The wrapping decision is taken in pixels, because injected text can request an arbitrary
        // width via WidthInEm which does not map to a whole number of columns.
        double typicalHalfwidthCharacterWidth = fontInfo.TypicalHalfwidthCharacterWidth;
        double wrappedLineBreakPixelWidth = (firstLineBreakColumn - wrappedTextIndentLength) * typicalHalfwidthCharacterWidth;

        var breakingOffsets = new List<int>();
        var breakingOffsetsVisibleColumn = new List<double>();
        int breakOffset = 0;
        double breakOffsetVisibleColumn = 0;
        double breakOffsetPixelWidth = 0;

        double breakingPixelWidth = firstLineBreakColumn * typicalHalfwidthCharacterWidth;
        int fixedWidthRangeIndex = 0;
        FixedWidthInjectedTextRange? firstFixedWidthRange = fixedWidthRanges.Count > 0 ? fixedWidthRanges[0] : null;
        bool startsWithFixedWidth = firstFixedWidthRange is { StartOffset: 0 };

        int prevCharCode;
        CharacterClass prevCharCodeClass;
        double visibleColumn;
        double currentLinePixelWidth;
        int startOffset;
        if (startsWithFixedWidth)
        {
            var first = firstFixedWidthRange!.Value;
            prevCharCode = 0;
            prevCharCodeClass = CharacterClass.None;
            visibleColumn = ComputeFixedWidthRangeColumnWidth(lineText, first, 0, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
            currentLinePixelWidth = first.WidthInEm * fontInfo.FontSize;
            startOffset = first.EndOffset;
            fixedWidthRangeIndex++;
        }
        else
        {
            prevCharCode = lineText[0];
            visibleColumn = ComputeCharWidth(prevCharCode, 0, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
            currentLinePixelWidth = ComputeCharPixelWidth(prevCharCode, 0, tabSize, fontInfo, useTwoCellFullwidthCharacters);
            startOffset = 1;
            if (Strings.IsHighSurrogate(prevCharCode))
            {
                // A surrogate pair must always be considered as a single unit, so it is never to be broken
                visibleColumn += 1;
                currentLinePixelWidth += typicalHalfwidthCharacterWidth;
                prevCharCode = lineText[1];
                startOffset++;
            }

            prevCharCodeClass = classifier.Get(prevCharCode);
        }

        for (int i = startOffset; i < len; i++)
        {
            FixedWidthInjectedTextRange? fixedWidthRange = fixedWidthRangeIndex < fixedWidthRanges.Count ? fixedWidthRanges[fixedWidthRangeIndex] : null;
            int charStartOffset = i;
            int charCode = lineText[i];
            CharacterClass charCodeClass;
            double charWidth;
            double charPixelWidth;
            bool wrapEscapedLineFeed = false;

            if (fixedWidthRange is { } range && range.StartOffset == i)
            {
                charCode = 0;
                charCodeClass = CharacterClass.None;
                charWidth = ComputeFixedWidthRangeColumnWidth(lineText, range, visibleColumn, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
                charPixelWidth = range.WidthInEm * fontInfo.FontSize;
                i = range.EndOffset - 1;
                fixedWidthRangeIndex++;
            }
            else if (Strings.IsHighSurrogate(charCode))
            {
                // A surrogate pair must always be considered as a single unit, so it is never to be broken
                i++;
                charCodeClass = CharacterClass.None;
                charWidth = 2;
                charPixelWidth = 2 * typicalHalfwidthCharacterWidth;
            }
            else
            {
                charCodeClass = classifier.Get(charCode);
                charWidth = ComputeCharWidth(charCode, visibleColumn, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
                charPixelWidth = ComputeCharPixelWidth(charCode, visibleColumn, tabSize, fontInfo, useTwoCellFullwidthCharacters);
            }

            // literal \n shall trigger a softwrap
            if (wrapOnEscapedLineFeeds && IsEscapedLineBreakAtPosition(lineText, charStartOffset))
            {
                breakOffset = charStartOffset;
                breakOffsetVisibleColumn = visibleColumn;
                breakOffsetPixelWidth = currentLinePixelWidth;
                wrapEscapedLineFeed = true;
            }
            else if (CanBreak(prevCharCode, prevCharCodeClass, charCode, charCodeClass, isKeepAll))
            {
                breakOffset = charStartOffset;
                breakOffsetVisibleColumn = visibleColumn;
                breakOffsetPixelWidth = currentLinePixelWidth;
            }

            visibleColumn += charWidth;
            currentLinePixelWidth += charPixelWidth;

            // check if adding character at `i` will go over the breaking width
            if (currentLinePixelWidth > breakingPixelWidth || wrapEscapedLineFeed)
            {
                // We need to break at least before character at `i`:
                if (breakOffset == 0 || currentLinePixelWidth - breakOffsetPixelWidth > wrappedLineBreakPixelWidth)
                {
                    // Cannot break at `breakOffset`, must break at `i`
                    breakOffset = charStartOffset;
                    breakOffsetVisibleColumn = visibleColumn - charWidth;
                    breakOffsetPixelWidth = currentLinePixelWidth - charPixelWidth;
                }

                int currentLineStartOffset = breakingOffsets.Count > 0 ? breakingOffsets[^1] : 0;
                if (breakOffset > currentLineStartOffset)
                {
                    // Breaking at the start of the current output line would emit an empty line, which
                    // happens when an oversized leading width-only injection cannot fit before the first
                    // character. In that case keep the content on the current line and defer the break.
                    breakingOffsets.Add(breakOffset);
                    breakingOffsetsVisibleColumn.Add(breakOffsetVisibleColumn);
                    breakingPixelWidth = breakOffsetPixelWidth + wrappedLineBreakPixelWidth;
                    breakOffset = 0;
                }
            }

            prevCharCode = charCode;
            prevCharCodeClass = charCodeClass;
        }

        if (breakingOffsets.Count == 0 && injectedTexts is not { Count: > 0 })
        {
            return null;
        }

        // Add last segment
        breakingOffsets.Add(len);
        breakingOffsetsVisibleColumn.Add(visibleColumn);

        return new ModelLineProjectionData(injectionOffsets, injectionOptions, [.. breakingOffsets], [.. breakingOffsetsVisibleColumn], wrappedTextIndentLength);
    }
}
