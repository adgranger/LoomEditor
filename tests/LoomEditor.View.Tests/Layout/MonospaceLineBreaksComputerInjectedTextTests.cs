// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;
using static LoomEditor.View.Tests.Layout.TestHelpers.LineBreaksTestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class MonospaceLineBreaksComputerInjectedTextTests
{
    private static readonly MonospaceLineBreaksComputerFactory Factory = new("", "");

    private static ModelLineProjectionData? Break(
        int breakAfter,
        string text,
        Core.Model.Events.LineInjectedText[] injected,
        string breakAfterChars = "",
        WrappingIndent indent = WrappingIndent.None,
        bool wrapOnEscapedLineFeeds = false) =>
        GetLineBreakData(
            breakAfterChars == "" ? Factory : new MonospaceLineBreaksComputerFactory("", breakAfterChars),
            4, breakAfter, 2, indent, WordBreak.Normal, wrapOnEscapedLineFeeds, text, null, injected);

    private static void AssertBreaks(ModelLineProjectionData? data, int[] offsets, double[] visibleColumns)
    {
        Assert.Equal(offsets, data?.BreakOffsets);
        Assert.Equal(visibleColumns, data?.BreakOffsetsVisibleColumn);
    }

    // Monaco: 'accounts for fixed injected text width when wrapping'
    [Fact]
    public void AccountsForFixedInjectedTextWidthWhenWrapping() =>
        AssertBreaks(Break(5, "abcdef", [Injected(5, " ", 1)]), [4, 7], [4, 7]);

    // Monaco: 'treats multi-character fixed-width injected text as an atomic span'
    [Fact]
    public void TreatsMultiCharacterFixedWidthInjectedTextAsAnAtomicSpan() =>
        AssertBreaks(Break(5, "abcdef", [Injected(4, "hello", 1)]), [8, 11], [8, 11]);

    // Monaco: 'wraps after an escaped line feed followed by fixed-width injected text'
    [Fact]
    public void WrapsAfterAnEscapedLineFeedFollowedByFixedWidthInjectedText() =>
        Assert.Equal([4, 10], Break(100, "\"a\\nb\"", [Injected(5, "hint", 1)], wrapOnEscapedLineFeeds: true)!.BreakOffsets);

    // Monaco: 'treats adjacent fixed-width injected texts as separate atomic spans'
    [Fact]
    public void TreatsAdjacentFixedWidthInjectedTextsAsSeparateAtomicSpans() =>
        AssertBreaks(Break(5, "abcdef", [Injected(4, "x", 0.5), Injected(4, "y", 1, 1)]), [4, 8], [4, 8]);

    // Monaco: 'keeps fixed-width injected text wider than the wrap column atomic'
    [Fact]
    public void KeepsFixedWidthInjectedTextWiderThanTheWrapColumnAtomic() =>
        AssertBreaks(Break(5, "abcdef", [Injected(4, "x", 3)]), [3, 4, 7], [3, 4, 7]);

    // Monaco: 'wraps before a tab that no longer fits after fixed-width injected text'
    [Fact]
    public void WrapsBeforeATabThatNoLongerFitsAfterFixedWidthInjectedText() =>
        AssertBreaks(Break(4, "ab\tcd", [Injected(3, " ", 0.75)]), [3, 6], [3, 6]);

    // Monaco: 'expands a tab following fixed-width injected text from the character column'
    [Fact]
    public void ExpandsATabFollowingFixedWidthInjectedTextFromTheCharacterColumn() =>
        AssertBreaks(Break(10, "ab\tcdefgh", [Injected(3, " ", 1.5)]), [8, 10], [8, 10]);

    // Monaco: 'reserves width for a spacing-only injection at the beginning of a line'
    [Fact]
    public void ReservesWidthForASpacingOnlyInjectionAtTheBeginningOfALine() =>
        AssertBreaks(Break(6, "abcdef", [Injected(1, "", 1)]), [4, 6], [4, 6]);

    // Monaco: 'does not create empty lines for oversized spacing-only injected text'
    [Fact]
    public void DoesNotCreateEmptyLinesForOversizedSpacingOnlyInjectedText() =>
        Assert.Equal([1, 3], Break(2, "abc", [Injected(1, "", 2)])!.BreakOffsets);

    // Monaco: 'lets a spacing-only injection in the middle of a line inherit surrounding break opportunities'
    [Fact]
    public void LetsASpacingOnlyInjectionInTheMiddleOfALineInheritSurroundingBreakOpportunities() =>
        AssertBreaks(Break(8, "ab cdef", [Injected(4, "", 1)], " "), [3, 7], [3, 7]);

    // Monaco: 'ignores a spacing-only injection at the end of a line'
    [Fact]
    public void IgnoresASpacingOnlyInjectionAtTheEndOfALine() =>
        AssertBreaks(Break(6, "abcdef", [Injected(7, "", 1)]), [6], [6]);

    // Monaco: 'expands a tab following a spacing-only injection from the character column'
    [Fact]
    public void ExpandsATabFollowingASpacingOnlyInjectionFromTheCharacterColumn() =>
        AssertBreaks(Break(6, "ab\tcd", [Injected(3, "", 1)]), [3, 5], [4, 6]);

    // Monaco: 'does not use fixed-width injected whitespace as continuation indentation'
    [Fact]
    public void DoesNotUseFixedWidthInjectedWhitespaceAsContinuationIndentation()
    {
        ModelLineProjectionData? data = Break(5, "abcdef", [Injected(1, " ", 0.75)], indent: WrappingIndent.Same);

        Assert.Equal([4, 7], data!.BreakOffsets);
        Assert.Equal(0, data!.WrappedTextIndentLength);
    }
}
