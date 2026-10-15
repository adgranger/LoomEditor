// Ported from microsoft/vscode: src/vs/base/test/common/strings.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class StringsTests
{
    // Monaco: 'lastNonWhitespaceIndex'
    [Fact]
    public void LastNonWhitespaceIndex_Monaco()
    {
        Assert.Equal(2, Strings.LastNonWhitespaceIndex("abc  \t \t "));
        Assert.Equal(2, Strings.LastNonWhitespaceIndex("abc"));
        Assert.Equal(2, Strings.LastNonWhitespaceIndex("abc\t"));
        Assert.Equal(2, Strings.LastNonWhitespaceIndex("abc "));
        Assert.Equal(2, Strings.LastNonWhitespaceIndex("abc  \t \t "));
        Assert.Equal(11, Strings.LastNonWhitespaceIndex("abc  \t \t abc \t \t "));
        Assert.Equal(2, Strings.LastNonWhitespaceIndex("abc  \t \t abc \t \t ", 8));
        Assert.Equal(-1, Strings.LastNonWhitespaceIndex("  \t \t "));
    }

    // Monaco: 'issue #115221: isEmojiImprecise misses ⭐'
    [Fact]
    public void IsEmojiImprecise_Star_Issue115221()
    {
        var codePoint = Strings.GetNextCodePoint("⭐", "⭐".Length, 0);
        Assert.True(Strings.IsEmojiImprecise(codePoint));
    }

    // Monaco: 'isFullWidthCharacter'
    [Fact]
    public void IsFullWidthCharacter_Monaco()
    {
        Assert.True(Strings.IsFullWidthCharacter('Ａ'), "Ａ U+FF21 fullwidth A");
        Assert.True(Strings.IsFullWidthCharacter('？'), "？ U+FF1F fullwidth question mark");
        Assert.True(Strings.IsFullWidthCharacter('＃'), "＃ U+FF03 fullwidth number sign");
        Assert.True(Strings.IsFullWidthCharacter('＝'), "＝ U+FF1D fullwidth equals sign");
        Assert.True(Strings.IsFullWidthCharacter('あ'), "あ U+3042 hiragana");
        Assert.True(Strings.IsFullWidthCharacter('￥'), "￥ U+FFE5 fullwidth yen sign");
        Assert.False(Strings.IsFullWidthCharacter('A'), "A regular ASCII");
        Assert.False(Strings.IsFullWidthCharacter('?'), "? regular ASCII");
    }

    // Monaco: 'getLeadingWhitespace'
    [Fact]
    public void GetLeadingWhitespace_Monaco()
    {
        Assert.Equal("  ", Strings.GetLeadingWhitespace("  foo"));
        Assert.Equal("", Strings.GetLeadingWhitespace("  foo", 2));
        Assert.Equal("", Strings.GetLeadingWhitespace("  foo", 1, 1));
        Assert.Equal(" ", Strings.GetLeadingWhitespace("  foo", 0, 1));
        Assert.Equal("  ", Strings.GetLeadingWhitespace("  "));
        Assert.Equal(" ", Strings.GetLeadingWhitespace("  ", 1));
        Assert.Equal(" ", Strings.GetLeadingWhitespace("  ", 0, 1));
        Assert.Equal("\t", Strings.GetLeadingWhitespace("\t\tfunction foo(){", 0, 1));
        Assert.Equal("\t\t", Strings.GetLeadingWhitespace("\t\tfunction foo(){", 0, 2));
    }

    [Fact]
    public void CodePointIterator_SurrogatePair_WalksBothDirections()
    {
        var it = new CodePointIterator("a😀b");
        Assert.Equal(0x61, it.NextCodePoint());
        Assert.Equal(1, it.Offset);
        Assert.Equal(0x1F600, it.NextCodePoint());
        Assert.Equal(3, it.Offset);
        Assert.Equal(0x62, it.NextCodePoint());
        Assert.Equal(4, it.Offset);
        Assert.True(it.Eol());

        Assert.Equal(0x62, it.PrevCodePoint());
        Assert.Equal(3, it.Offset);
        Assert.Equal(0x1F600, it.PrevCodePoint());
        Assert.Equal(1, it.Offset);
        Assert.Equal(0x61, it.PrevCodePoint());
        Assert.Equal(0, it.Offset);
    }

    [Fact]
    public void GetNextCodePoint_LoneHighSurrogateAtEnd_ReturnsSurrogate()
    {
        const string text = "a\uD83D";
        Assert.Equal(0xD83D, Strings.GetNextCodePoint(text, text.Length, 1));
    }

    [Fact]
    public void CommonPrefixAndSuffixLength_ReturnExpectedLengths()
    {
        Assert.Equal(2, Strings.CommonPrefixLength("abcd", "abxy"));
        Assert.Equal(3, Strings.CommonSuffixLength("xbcd", "abcd"));
    }

    [Fact]
    public void GetLeftDeleteOffset_SkinToneEmoji_DeletesWholeEmoji()
    {
        const string text = "a👍🏽";
        Assert.Equal(1, Strings.GetLeftDeleteOffset(text.Length, text));
    }

    [Fact]
    public void GetLeftDeleteOffset_ZwjSequence_DeletesLastComponent()
    {
        const string text = "👨‍👩‍👧";
        Assert.Equal(5, Strings.GetLeftDeleteOffset(text.Length, text));
    }

    [Fact]
    public void GetLeftDeleteOffset_PlainText_SkipsOneCharacter()
    {
        Assert.Equal(1, Strings.GetLeftDeleteOffset(2, "ab"));
        Assert.Equal(0, Strings.GetLeftDeleteOffset(0, "ab"));
    }

    [Fact]
    public void SplitLines_MixedLineBreaks_SplitsLikeRegex()
    {
        Assert.Equal(["a", "b", "c", "d"], Strings.SplitLines("a\r\nb\rc\nd"));
    }
}
