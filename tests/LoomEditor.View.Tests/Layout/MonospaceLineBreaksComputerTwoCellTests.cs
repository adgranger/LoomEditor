// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;
using static LoomEditor.View.Tests.Layout.TestHelpers.LineBreaksTestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class MonospaceLineBreaksComputerTwoCellTests
{
    private static readonly MonospaceLineBreaksComputerFactory Factory = new("(", "\t)");

    private static string WrapWith(string text, int breakAfter, double columnsForFullWidthChar, bool useTwoCellFullwidthCharacters) =>
        ToAnnotatedText(text, GetLineBreakData(Factory, 4, breakAfter, columnsForFullWidthChar, WrappingIndent.None, WordBreak.Normal, false, text, null, null, useTwoCellFullwidthCharacters));

    private static int? WrappedIndentWith(string lineText, int breakAfter, double columnsForFullWidthChar, bool useTwoCellFullwidthCharacters) =>
        GetLineBreakData(Factory, 4, breakAfter, columnsForFullWidthChar, WrappingIndent.Same, WordBreak.Normal, false, lineText, null, null, useTwoCellFullwidthCharacters)?.WrappedTextIndentLength;

    // Monaco: 'MonospaceLineBreaksComputer - two-cell full-width character width'
    [Fact]
    public void TwoCellFullWidthCharacterWidth()
    {
        string text = new('あ', 4);

        Assert.Equal("あ|あ|あ|あ", WrapWith(text, 5, 3, false)); // lyingFont
        Assert.Equal("ああ|ああ", WrapWith(text, 5, 3, true)); // lyingFontTwoCells
        Assert.Equal("ああ|ああ", WrapWith(text, 5, 2, false)); // gridAlignedFont
    }

    // Monaco: 'MonospaceLineBreaksComputer - two-cell width determines whether continuation indentation fits'
    [Fact]
    public void TwoCellWidthDeterminesWhetherContinuationIndentationFits()
    {
        const string text = "    あああ";

        Assert.Equal(0, WrappedIndentWith(text, 6, 3, false)); // wideNatural
        Assert.Equal(4, WrappedIndentWith(text, 6, 3, true)); // wideTwoCells
        Assert.Equal(4, WrappedIndentWith(text, 5, 1, false)); // narrowNatural
        Assert.Equal(0, WrappedIndentWith(text, 5, 1, true)); // narrowTwoCells
    }

    // Monaco: 'MonospaceLineBreaksComputer - two-cell width uses shared full-width classification'
    [Fact]
    public void TwoCellWidthUsesSharedFullWidthClassification()
    {
        string fullwidthAscii = new('Ａ', 4);
        string vaiSyllables = new('ꔀ', 4);

        Assert.Equal("Ａ|Ａ|Ａ|Ａ", WrapWith(fullwidthAscii, 5, 3, false)); // fullwidthAsciiNatural
        Assert.Equal("ＡＡ|ＡＡ", WrapWith(fullwidthAscii, 5, 3, true)); // fullwidthAsciiTwoCells
        Assert.Equal("ꔀ|ꔀ|ꔀ|ꔀ", WrapWith(vaiSyllables, 5, 3, false)); // vaiNatural
        Assert.Equal("ꔀꔀ|ꔀꔀ", WrapWith(vaiSyllables, 5, 3, true)); // vaiTwoCells
    }

    // Monaco: 'MonospaceLineBreaksComputer - two-cell width preserves natural control-character metrics'
    [Fact]
    public void TwoCellWidthPreservesNaturalControlCharacterMetrics()
    {
        const string controlCharacters = "\u0001\u0001";
        const string controlCharacterWithTab = "\u0001\t";

        Assert.Equal("\u0001|\u0001", WrapWith(controlCharacters, 4, 3, false)); // controlNatural
        Assert.Equal("\u0001|\u0001", WrapWith(controlCharacters, 4, 3, true)); // controlTwoCells

        ModelLineProjectionData? natural = GetLineBreakData(Factory, 4, 3, 3, WrappingIndent.None, WordBreak.Normal, false, controlCharacterWithTab, null, null, false);
        ModelLineProjectionData? twoCells = GetLineBreakData(Factory, 4, 3, 3, WrappingIndent.None, WordBreak.Normal, false, controlCharacterWithTab, null, null, true);
        Assert.NotNull(natural);
        Assert.NotNull(twoCells);
        Assert.Equal([3, 4], natural.BreakOffsetsVisibleColumn); // controlWithTabNaturalColumns
        Assert.Equal([3, 4], twoCells.BreakOffsetsVisibleColumn); // controlWithTabTwoCellsColumns
    }

    // Monaco: 'MonospaceLineBreaksComputer - two-cell width treats combining marks independently'
    [Fact]
    public void TwoCellWidthTreatsCombiningMarksIndependently()
    {
        const string combinedCharacter = "あ́aa";

        Assert.Equal("あ|́aa", WrapWith(combinedCharacter, 4, 3, false)); // combinedNatural
        Assert.Equal("あ|́aa", WrapWith(combinedCharacter, 4, 3, true)); // combinedTwoCells
    }

    // Monaco: 'MonospaceLineBreaksComputer - two-cell width supports incremental line breaks'
    [Fact]
    public void TwoCellWidthSupportsIncrementalLineBreaks()
    {
        var factory = new MonospaceLineBreaksComputerFactory(EditorWrappingOptions.DefaultBreakBeforeCharacters, EditorWrappingOptions.DefaultBreakAfterCharacters);

        // A font that lies about its full-width ratio: each ideograph still occupies exactly two cells.
        AssertIncrementalLineBreaks(
            factory, "ああああああ", 4,
            5, "ああ|ああ|ああ",
            7, "あああ|あああ",
            WrappingIndent.None,
            3,
            true);

        AssertIncrementalLineBreaks(
            factory, "    ああああ", 4,
            8, "    ああ|ああ",
            10, "    あああ|あ",
            WrappingIndent.Same,
            3,
            true);

        AssertIncrementalLineBreaks(
            factory, "你好 **hello** **hello** **hello-world** hey there!", 4,
            15, "你好 **hello** |**hello** |**hello-world**| hey there!",
            1, "你|好| |*|*|h|e|l|l|o|*|*| |*|*|h|e|l|l|o|*|*| |*|*|h|e|l|l|o|-|w|o|r|l|d|*|*| |h|e|y| |t|h|e|r|e|!",
            WrappingIndent.Same,
            1.6605405405405405,
            true);
    }
}
