// Ported from microsoft/vscode-textmate: src/tests/grammar.test.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Tests.Grammar;

public class EncodedTokenAttributesTests
{
    private const FontStyle UnderlineBold = FontStyle.Underline | FontStyle.Bold;

    // vscode-textmate: 'StackElementMetadata works'
    [Fact]
    public void StackElementMetadataWorks()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite languageId'
    [Fact]
    public void StackElementMetadataCanOverwriteLanguageId()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 2, OptionalStandardTokenType.NotSet, false, FontStyle.NotSet, 0, 0);
        AssertEquals(value, 2, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite tokenType'
    [Fact]
    public void StackElementMetadataCanOverwriteTokenType()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.Comment, false, FontStyle.NotSet, 0, 0);
        AssertEquals(value, 1, StandardTokenType.Comment, false, UnderlineBold, 101, 102);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite font style'
    [Fact]
    public void StackElementMetadataCanOverwriteFontStyle()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, false, FontStyle.None, 0, 0);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, FontStyle.None, 101, 102);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite font style with strikethrough'
    [Fact]
    public void StackElementMetadataCanOverwriteFontStyleWithStrikethrough()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, FontStyle.Strikethrough, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, FontStyle.Strikethrough, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, false, FontStyle.None, 0, 0);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, FontStyle.None, 101, 102);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite foreground'
    [Fact]
    public void StackElementMetadataCanOverwriteForeground()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, false, FontStyle.NotSet, 5, 0);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 5, 102);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite background'
    [Fact]
    public void StackElementMetadataCanOverwriteBackground()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, false, FontStyle.NotSet, 0, 7);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 7);
    }

    // vscode-textmate: 'StackElementMetadata can overwrite balanced backet bit'
    [Fact]
    public void StackElementMetadataCanOverwriteBalancedBacketBit()
    {
        uint value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, false, UnderlineBold, 101, 102);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, true, FontStyle.NotSet, 0, 0);
        AssertEquals(value, 1, StandardTokenType.RegEx, true, UnderlineBold, 101, 102);

        value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, false, FontStyle.NotSet, 0, 0);
        AssertEquals(value, 1, StandardTokenType.RegEx, false, UnderlineBold, 101, 102);
    }

    // vscode-textmate: 'StackElementMetadata can work at max values'
    [Fact]
    public void StackElementMetadataCanWorkAtMaxValues()
    {
        const int MaxLangId = 255;
        const StandardTokenType MaxTokenType = StandardTokenType.Comment | StandardTokenType.Other | StandardTokenType.RegEx | StandardTokenType.String;
        const FontStyle MaxFontStyle = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline;
        const int MaxForeground = 511;
        const int MaxBackground = 254;

        uint value = EncodedTokenAttributes.Set(0, MaxLangId, (OptionalStandardTokenType)MaxTokenType, true, MaxFontStyle, MaxForeground, MaxBackground);
        AssertEquals(value, MaxLangId, MaxTokenType, true, MaxFontStyle, MaxForeground, MaxBackground);
    }

    [Fact]
    public void Set_BackgroundInBit31_GivesAnUnsignedValue()
    {
        uint value = EncodedTokenAttributes.Set(0, 0, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 0, 255);

        Assert.Equal(0xFF000000u, value);
        Assert.Equal("11111111000000000000000000000000", EncodedTokenAttributes.ToBinaryStr(value));
        Assert.Equal("00000000000000000000000000000101", EncodedTokenAttributes.ToBinaryStr(5));
    }

    [Fact]
    public void OptionalTokenType_RoundTrips()
    {
        Assert.Equal(OptionalStandardTokenType.String, EncodedTokenAttributes.ToOptionalTokenType(StandardTokenType.String));
        Assert.Equal(StandardTokenType.RegEx, EncodedTokenAttributes.FromOptionalTokenType(OptionalStandardTokenType.RegEx));
    }

    private static void AssertEquals(uint metadata, int languageId, StandardTokenType tokenType, bool containsBalancedBrackets, FontStyle fontStyle, int foreground, int background)
    {
        var actual = (
            EncodedTokenAttributes.GetLanguageId(metadata),
            EncodedTokenAttributes.GetTokenType(metadata),
            EncodedTokenAttributes.ContainsBalancedBrackets(metadata),
            EncodedTokenAttributes.GetFontStyle(metadata),
            EncodedTokenAttributes.GetForeground(metadata),
            EncodedTokenAttributes.GetBackground(metadata));
        var expected = (languageId, tokenType, containsBalancedBrackets, (int)fontStyle, foreground, background);

        Assert.True(expected == actual, $"equals for {EncodedTokenAttributes.ToBinaryStr(metadata)}: {actual}");
    }
}
