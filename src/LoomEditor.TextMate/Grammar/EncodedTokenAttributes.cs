// Ported from microsoft/vscode-textmate: src/encodedTokenAttributes.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>EncodedTokenAttributes</c>: the "collapsed" metadata of a whole state stack, in 32 bits (a <see cref="uint"/>:
/// bit 31 belongs to the background color).
/// </summary>
/// <remarks>
/// <para>
/// The binary format (<c>EncodedTokenDataConsts</c>), bit 31 first:
/// <c>bbbb bbbb ffff ffff fFFF FBTT LLLL LLLL</c> — L = language id (8 bits), T = <see cref="StandardTokenType"/>
/// (2 bits), B = balanced bracket (1 bit), F = <see cref="FontStyle"/> (4 bits), f = foreground color (9 bits),
/// b = background color (8 bits). Same layout as Monaco's <c>encodedTokenAttributes.ts</c>. <c>FontAttribute</c> is not
/// ported (ADR 0008, decision 3).
/// </para>
/// <para>Threading: stateless.</para>
/// </remarks>
internal static class EncodedTokenAttributes
{
    // EncodedTokenDataConsts
    private const uint LanguageIdMask = 0b00000000000000000000000011111111;
    private const uint TokenTypeMask = 0b00000000000000000000001100000000;
    private const uint BalancedBracketsMask = 0b00000000000000000000010000000000;
    private const uint FontStyleMask = 0b00000000000000000111100000000000;
    private const uint ForegroundMask = 0b00000000111111111000000000000000;
    private const uint BackgroundMask = 0b11111111000000000000000000000000;

    private const int LanguageIdOffset = 0;
    private const int TokenTypeOffset = 8;
    private const int BalancedBracketsOffset = 10;
    private const int FontStyleOffset = 11;
    private const int ForegroundOffset = 15;
    private const int BackgroundOffset = 24;

    /// <summary><c>toBinaryStr</c>: 32 binary digits.</summary>
    public static string ToBinaryStr(uint encodedTokenAttributes) => Convert.ToString(encodedTokenAttributes, 2).PadLeft(32, '0');

    /// <summary><c>print</c>: writes the fields to <see cref="Debug"/>.</summary>
    public static void Print(uint encodedTokenAttributes)
    {
        Debug.WriteLine(
            $"{{ languageId: {GetLanguageId(encodedTokenAttributes)}, tokenType: {GetTokenType(encodedTokenAttributes)}, " +
            $"fontStyle: {GetFontStyle(encodedTokenAttributes)}, foreground: {GetForeground(encodedTokenAttributes)}, " +
            $"background: {GetBackground(encodedTokenAttributes)} }}");
    }

    /// <summary><c>getLanguageId</c>.</summary>
    public static int GetLanguageId(uint encodedTokenAttributes) =>
        (int)((encodedTokenAttributes & LanguageIdMask) >> LanguageIdOffset);

    /// <summary><c>getTokenType</c>.</summary>
    public static StandardTokenType GetTokenType(uint encodedTokenAttributes) =>
        (StandardTokenType)((encodedTokenAttributes & TokenTypeMask) >> TokenTypeOffset);

    /// <summary><c>containsBalancedBrackets</c>.</summary>
    public static bool ContainsBalancedBrackets(uint encodedTokenAttributes) =>
        (encodedTokenAttributes & BalancedBracketsMask) != 0;

    /// <summary><c>getFontStyle</c>: the <see cref="FontStyle"/> bits.</summary>
    public static int GetFontStyle(uint encodedTokenAttributes) =>
        (int)((encodedTokenAttributes & FontStyleMask) >> FontStyleOffset);

    /// <summary><c>getForeground</c>: a color id.</summary>
    public static int GetForeground(uint encodedTokenAttributes) =>
        (int)((encodedTokenAttributes & ForegroundMask) >> ForegroundOffset);

    /// <summary><c>getBackground</c>: a color id.</summary>
    public static int GetBackground(uint encodedTokenAttributes) =>
        (int)((encodedTokenAttributes & BackgroundMask) >> BackgroundOffset);

    /// <summary>
    /// <c>set</c>: updates the fields in <paramref name="encodedTokenAttributes"/>. A value of <c>0</c>,
    /// <c>NotSet</c> or <see langword="null"/> indicates that the corresponding field should be left as is.
    /// </summary>
    public static uint Set(uint encodedTokenAttributes, int languageId, OptionalStandardTokenType tokenType,
        bool? containsBalancedBrackets, FontStyle fontStyle, int foreground, int background)
    {
        uint languageIdBits = (uint)GetLanguageId(encodedTokenAttributes);
        uint tokenTypeBits = (uint)GetTokenType(encodedTokenAttributes);
        uint containsBalancedBracketsBit = ContainsBalancedBrackets(encodedTokenAttributes) ? 1u : 0u;
        uint fontStyleBits = (uint)GetFontStyle(encodedTokenAttributes);
        uint foregroundBits = (uint)GetForeground(encodedTokenAttributes);
        uint backgroundBits = (uint)GetBackground(encodedTokenAttributes);

        if (languageId != 0)
        {
            languageIdBits = (uint)languageId;
        }

        if (tokenType != OptionalStandardTokenType.NotSet)
        {
            tokenTypeBits = (uint)FromOptionalTokenType(tokenType);
        }

        if (containsBalancedBrackets is bool brackets)
        {
            containsBalancedBracketsBit = brackets ? 1u : 0u;
        }

        if (fontStyle != FontStyle.NotSet)
        {
            fontStyleBits = (uint)fontStyle;
        }

        if (foreground != 0)
        {
            foregroundBits = (uint)foreground;
        }

        if (background != 0)
        {
            backgroundBits = (uint)background;
        }

        return (languageIdBits << LanguageIdOffset)
            | (tokenTypeBits << TokenTypeOffset)
            | (containsBalancedBracketsBit << BalancedBracketsOffset)
            | (fontStyleBits << FontStyleOffset)
            | (foregroundBits << ForegroundOffset)
            | (backgroundBits << BackgroundOffset);
    }

    /// <summary><c>toOptionalTokenType</c>.</summary>
    public static OptionalStandardTokenType ToOptionalTokenType(StandardTokenType standardType) => (OptionalStandardTokenType)standardType;

    /// <summary><c>fromOptionalTokenType</c> (not <see cref="OptionalStandardTokenType.NotSet"/>).</summary>
    public static StandardTokenType FromOptionalTokenType(OptionalStandardTokenType standardType) => (StandardTokenType)standardType;
}
