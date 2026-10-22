// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;
using static LoomEditor.View.Tests.Layout.TestHelpers.LineBreaksTestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class MonospaceLineBreaksComputerTests
{
    private static MonospaceLineBreaksComputerFactory DefaultFactory() =>
        new(EditorWrappingOptions.DefaultBreakBeforeCharacters, EditorWrappingOptions.DefaultBreakAfterCharacters);

    // Monaco: 'MonospaceLineBreaksComputer'
    [Fact]
    public void MonospaceLineBreaksComputer()
    {
        var factory = new MonospaceLineBreaksComputerFactory("(", "\t).");

        // Empty string
        AssertLineBreaks(factory, 4, 5, "");

        // No wrapping if not necessary
        AssertLineBreaks(factory, 4, 5, "aaa");
        AssertLineBreaks(factory, 4, 5, "aaaaa");
        AssertLineBreaks(factory, 4, -1, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        // Acts like hard wrapping if no char found
        AssertLineBreaks(factory, 4, 5, "aaaaa|a");

        // Honors wrapping character
        AssertLineBreaks(factory, 4, 5, "aaaaa|.");
        AssertLineBreaks(factory, 4, 5, "aaaaa|a.|aaa.|aa");
        AssertLineBreaks(factory, 4, 5, "aaaaa|a..|aaa.|aa");
        AssertLineBreaks(factory, 4, 5, "aaaaa|a...|aaa.|aa");
        AssertLineBreaks(factory, 4, 5, "aaaaa|a....|aaa.|aa");

        // Honors tabs when computing wrapping position
        AssertLineBreaks(factory, 4, 5, "\t");
        AssertLineBreaks(factory, 4, 5, "\t|aaa");
        AssertLineBreaks(factory, 4, 5, "\t|a\t|aa");
        AssertLineBreaks(factory, 4, 5, "aa\ta");
        AssertLineBreaks(factory, 4, 5, "aa\t|aa");

        // Honors wrapping before characters (& gives it priority)
        AssertLineBreaks(factory, 4, 5, "aaa.|aa");
        AssertLineBreaks(factory, 4, 5, "aaa(.|aa");

        // Honors wrapping after characters (& gives it priority)
        AssertLineBreaks(factory, 4, 5, "aaa))|).aaa");
        AssertLineBreaks(factory, 4, 5, "aaa))|).|aaaa");
        AssertLineBreaks(factory, 4, 5, "aaa)|().|aaa");
        AssertLineBreaks(factory, 4, 5, "aaa|(().|aaa");
        AssertLineBreaks(factory, 4, 5, "aa.|(().|aaa");
        AssertLineBreaks(factory, 4, 5, "aa.|(.).|aaa");
    }

    // Monaco: 'MonospaceLineBreaksComputer - CJK and Kinsoku Shori'
    [Fact]
    public void CjkAndKinsokuShori()
    {
        var factory = new MonospaceLineBreaksComputerFactory("(", "\t)");
        AssertLineBreaks(factory, 4, 5, "aa 安|安");
        AssertLineBreaks(factory, 4, 5, "あ 安|安");
        AssertLineBreaks(factory, 4, 5, "ああ|安安");
        AssertLineBreaks(factory, 4, 5, "aa |安)安|安");
        AssertLineBreaks(factory, 4, 5, "aa あ|安あ)|安");
        AssertLineBreaks(factory, 4, 5, "aa |(安aa|安");
    }

    // Monaco: 'MonospaceLineBreaksComputer - WrappingIndent.Same'
    [Fact]
    public void WrappingIndentSame()
    {
        var factory = new MonospaceLineBreaksComputerFactory("", "\t ");
        AssertLineBreaks(factory, 4, 38, " *123456789012345678901234567890123456|7890", WrappingIndent.Same);
    }

    // Monaco: 'issue #16332: Scroll bar overlaying on top of text'
    [Fact]
    public void Issue16332_ScrollBarOverlayingOnTopOfText()
    {
        var factory = new MonospaceLineBreaksComputerFactory("", "\t ");
        AssertLineBreaks(factory, 4, 24, "a/ very/long/line/of/tex|t/that/expands/beyon|d/your/typical/line/|of/code/", WrappingIndent.Indent);
    }

    // Monaco: 'issue #35162: wrappingIndent not consistently working'
    [Fact]
    public void Issue35162_WrappingIndentNotConsistentlyWorking()
    {
        var factory = new MonospaceLineBreaksComputerFactory("", "\t ");
        ModelLineProjectionData? mapper = AssertLineBreaks(factory, 4, 24, "                t h i s |i s |a l |o n |g l |i n |e", WrappingIndent.Indent);
        Assert.Equal("                    ".Length, mapper!.WrappedTextIndentLength);
    }

    // Monaco: 'issue #75494: surrogate pairs'
    [Fact]
    public void Issue75494_SurrogatePairs()
    {
        var factory = new MonospaceLineBreaksComputerFactory("\t", " ");
        AssertLineBreaks(factory, 4, 49, "🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼|🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼|🐇👬", WrappingIndent.Same);
    }

    // Monaco: 'issue #75494: surrogate pairs overrun 1'
    [Fact]
    public void Issue75494_SurrogatePairsOverrun1() =>
        AssertLineBreaks(DefaultFactory(), 4, 4, "🐇👬|&|🌞🌖", WrappingIndent.Same);

    // Monaco: 'issue #75494: surrogate pairs overrun 2'
    [Fact]
    public void Issue75494_SurrogatePairsOverrun2() =>
        AssertLineBreaks(DefaultFactory(), 4, 17, "factory, |\"xtxtfunc|(x\"🌞🏇🍼🌞🏇🍼🐇|&👬🌖🌞👬🌖🌞🏇🍼|🐇👬x\"", WrappingIndent.Same);

    // Monaco: 'MonospaceLineBreaksComputer - WrappingIndent.DeepIndent'
    [Fact]
    public void WrappingIndentDeepIndent()
    {
        var factory = new MonospaceLineBreaksComputerFactory("", "\t ");
        ModelLineProjectionData? mapper = AssertLineBreaks(factory, 4, 26, "        W e A r e T e s t |i n g D e |e p I n d |e n t a t |i o n", WrappingIndent.DeepIndent);
        Assert.Equal("                ".Length, mapper!.WrappedTextIndentLength);
    }

    // Monaco: 'issue #33366: Word wrap algorithm behaves differently around punctuation'
    [Fact]
    public void Issue33366_WordWrapAroundPunctuation() =>
        AssertLineBreaks(DefaultFactory(), 4, 23, "this is a line of |text, text that sits |on a line", WrappingIndent.Same);

    // Monaco: 'issue #152773: Word wrap algorithm behaves differently with bracket followed by comma'
    [Fact]
    public void Issue152773_BracketFollowedByComma() =>
        AssertLineBreaks(DefaultFactory(), 4, 24, "this is a line of |(text), text that sits |on a line", WrappingIndent.Same);

    // Monaco: 'issue #112382: Word wrap doesn't work well with control characters'
    [Fact]
    public void Issue112382_ControlCharacters() =>
        AssertLineBreaks(DefaultFactory(), 4, 6, "\u0006\u0006\u0006|\u0006\u0006\u0006", WrappingIndent.Same);

    // Monaco: 'Word break work well with Chinese/Japanese/Korean (CJK) text when setting normal'
    [Fact]
    public void WordBreak_Cjk_Normal() =>
        AssertLineBreaks(DefaultFactory(), 4, 5, "你好|1111", WrappingIndent.Same, WordBreak.Normal);

    // Monaco: 'Word break work well with Chinese/Japanese/Korean (CJK) text when setting keepAll'
    [Fact]
    public void WordBreak_Cjk_KeepAll() =>
        AssertLineBreaks(DefaultFactory(), 4, 8, "你好1111", WrappingIndent.Same, WordBreak.KeepAll);

    // Monaco: 'issue #258022: wrapOnEscapedLineFeeds: should work correctly after editor resize'
    [Fact]
    public void Issue258022_WrapOnEscapedLineFeeds_AfterResize()
    {
        MonospaceLineBreaksComputerFactory factory = DefaultFactory();
        const string text = "\"Short text with\\nescaped newline and an escaped\\\\nbackslash\"";

        ModelLineProjectionData? initialBreakData = GetLineBreakData(factory, 4, 30, 2, WrappingIndent.None, WordBreak.Normal, true, text, null);
        string initialAnnotatedText = ToAnnotatedText(text, initialBreakData);
        Assert.Contains("with\\n", initialAnnotatedText);

        ModelLineProjectionData? resizedBreakData = GetLineBreakData(factory, 4, 35, 2, WrappingIndent.None, WordBreak.Normal, true, text, initialBreakData);
        string resizedAnnotatedText = ToAnnotatedText(text, resizedBreakData);

        ModelLineProjectionData? freshBreakData = GetLineBreakData(factory, 4, 35, 2, WrappingIndent.None, WordBreak.Normal, true, text, null);
        string freshAnnotatedText = ToAnnotatedText(text, freshBreakData);
        Assert.Contains("with\\n", freshAnnotatedText);

        Assert.Equal(freshAnnotatedText, resizedAnnotatedText);
    }
}
