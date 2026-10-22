// Ported from microsoft/vscode: src/vs/editor/common/viewModel/monospaceLineBreaksComputer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>The classes of the Kinsoku Shori rules (Monaco: <c>CharacterClass</c>).</summary>
internal enum CharacterClass : byte
{
    None = 0,
    BreakBefore = 1,
    BreakAfter = 2,

    /// <summary>For Han and Kana.</summary>
    BreakIdeographic = 3,
}

/// <summary>Classifies characters for line breaking (Monaco: <c>WrappingCharacterClassifier</c>).</summary>
/// <remarks>Threading: immutable once built; safe to share.</remarks>
internal sealed class WrappingCharacterClassifier : CharacterClassifier<CharacterClass>
{
    public WrappingCharacterClassifier(string breakBefore, string breakAfter)
        : base(CharacterClass.None)
    {
        foreach (char c in breakBefore)
        {
            Set(c, CharacterClass.BreakBefore);
        }

        foreach (char c in breakAfter)
        {
            Set(c, CharacterClass.BreakAfter);
        }
    }

    /// <summary>Beyond ASCII, the CJK ranges are <see cref="CharacterClass.BreakIdeographic"/> before the stored classes.</summary>
    public override CharacterClass Get(int charCode)
    {
        // CJK Unified Ideographs (0x4E00-0x9FFF), Extension A (0x3400-0x4DBF), Hiragana and Katakana (0x3040-0x30FF).
        if (charCode >= 256
            && ((charCode >= 0x3040 && charCode <= 0x30FF)
                || (charCode >= 0x3400 && charCode <= 0x4DBF)
                || (charCode >= 0x4E00 && charCode <= 0x9FFF)))
        {
            return CharacterClass.BreakIdeographic;
        }

        return base.Get(charCode);
    }
}

/// <summary>Port of the monospace line-breaks computer (<c>monospaceLineBreaksComputer.ts</c>).</summary>
/// <remarks>Threading: stateless helpers, safe from any thread.</remarks>
internal static partial class MonospaceLineBreaksComputer
{
    public static double ComputeCharWidth(int charCode, double visibleColumn, int tabSize, double columnsForFullWidthChar, bool useTwoCellFullwidthCharacters)
    {
        if (charCode == '\t')
        {
            return tabSize - (visibleColumn % tabSize);
        }

        if (Strings.IsFullWidthCharacter(charCode))
        {
            return FullWidthCharacterColumns(columnsForFullWidthChar, useTwoCellFullwidthCharacters);
        }

        if (charCode < 32)
        {
            // when using `editor.renderControlCharacters`, the substitutions are often wide
            return columnsForFullWidthChar;
        }

        return 1;
    }

    /// <summary>Exactly two cells when full-width characters are forced into two cells, otherwise the ratio measured from the font.</summary>
    public static double FullWidthCharacterColumns(double columnsForFullWidthChar, bool useTwoCellFullwidthCharacters) =>
        useTwoCellFullwidthCharacters ? 2 : columnsForFullWidthChar;

    public static double TabCharacterWidth(double visibleColumn, int tabSize) => tabSize - (visibleColumn % tabSize);

    /// <summary>The width in pixels of a character; the wrapping decision reasons in real widths because injected text can request any width.</summary>
    public static double ComputeCharPixelWidth(int charCode, double visibleColumn, int tabSize, FontInfo fontInfo, bool useTwoCellFullwidthCharacters)
    {
        if (charCode == '\t')
        {
            return TabCharacterWidth(visibleColumn, tabSize) * fontInfo.TypicalHalfwidthCharacterWidth;
        }

        if (Strings.IsFullWidthCharacter(charCode))
        {
            return useTwoCellFullwidthCharacters ? 2 * fontInfo.TypicalHalfwidthCharacterWidth : fontInfo.TypicalFullwidthCharacterWidth;
        }

        if (charCode < 32)
        {
            // when using `editor.renderControlCharacters`, the substitutions are often wide
            return fontInfo.TypicalFullwidthCharacterWidth;
        }

        return fontInfo.TypicalHalfwidthCharacterWidth;
    }

    /// <summary>
    /// The columns of the characters of a fixed width injected text range (0 for a width-only injection).
    /// <c>WidthInEm</c> is ignored on purpose: the visible column only drives tab expansion, which must match the rendering.
    /// </summary>
    public static double ComputeFixedWidthRangeColumnWidth(string lineText, FixedWidthInjectedTextRange range, double visibleColumn, int tabSize, double columnsForFullWidthChar, bool useTwoCellFullwidthCharacters)
    {
        double width = 0;
        for (int i = range.StartOffset; i < range.EndOffset; i++)
        {
            width += ComputeCharWidth(lineText[i], visibleColumn + width, tabSize, columnsForFullWidthChar, useTwoCellFullwidthCharacters);
        }

        return width;
    }

    /// <summary>Whether position <paramref name="i"/> follows an escaped line feed (<c>\n</c> with an odd number of backslashes).</summary>
    public static bool IsEscapedLineBreakAtPosition(string lineText, int i)
    {
        if (i >= 2 && lineText[i - 1] == 'n')
        {
            // Check if there's an odd number of backslashes
            int escapeCount = 0;
            for (int j = i - 2; j >= 0; j--)
            {
                if (lineText[j] == '\\')
                {
                    escapeCount++;
                }
                else
                {
                    return escapeCount % 2 == 1;
                }
            }
        }

        return false;
    }

    /// <summary>Kinsoku Shori: no break after a leading character (open bracket), none before a trailing one (period).</summary>
    public static bool CanBreak(int prevCharCode, CharacterClass prevCharCodeClass, int charCode, CharacterClass charCodeClass, bool isKeepAll) =>
        charCode != ' '
        && ((prevCharCodeClass == CharacterClass.BreakAfter && charCodeClass != CharacterClass.BreakAfter) // break at the end of multiple BreakAfter
            || (prevCharCodeClass != CharacterClass.BreakBefore && charCodeClass == CharacterClass.BreakBefore) // break at the start of multiple BreakBefore
            || (!isKeepAll && prevCharCodeClass == CharacterClass.BreakIdeographic && charCodeClass != CharacterClass.BreakAfter)
            || (!isKeepAll && charCodeClass == CharacterClass.BreakIdeographic && prevCharCodeClass != CharacterClass.BreakBefore));

    public static int ComputeWrappedTextIndentLength(string lineText, int tabSize, int firstLineBreakColumn, double columnsForFullWidthChar, WrappingIndent wrappingIndent, IReadOnlyList<FixedWidthInjectedTextRange>? fixedWidthRanges = null)
    {
        int wrappedTextIndentLength = 0;
        if (wrappingIndent != WrappingIndent.None)
        {
            int firstNonWhitespaceIndex = StringClassification.FirstNonWhitespaceIndex(lineText);
            if (firstNonWhitespaceIndex != -1)
            {
                // Track existing indent
                for (int i = 0; i < firstNonWhitespaceIndex; i++)
                {
                    if (fixedWidthRanges is { Count: > 0 } && fixedWidthRanges[0].StartOffset == i)
                    {
                        break;
                    }

                    wrappedTextIndentLength += lineText[i] == '\t' ? (int)TabCharacterWidth(wrappedTextIndentLength, tabSize) : 1;
                }

                // Increase indent of continuation lines, if desired
                int numberOfAdditionalTabs = wrappingIndent == WrappingIndent.DeepIndent ? 2 : wrappingIndent == WrappingIndent.Indent ? 1 : 0;
                for (int i = 0; i < numberOfAdditionalTabs; i++)
                {
                    wrappedTextIndentLength += (int)TabCharacterWidth(wrappedTextIndentLength, tabSize);
                }

                // Force sticking to beginning of line if no character would fit except for the indentation
                if (wrappedTextIndentLength + columnsForFullWidthChar > firstLineBreakColumn)
                {
                    wrappedTextIndentLength = 0;
                }
            }
        }

        return wrappedTextIndentLength;
    }
}
