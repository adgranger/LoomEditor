// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// Helpers to manage the "collapsed" metadata of an entire token (Monaco: <c>MetadataConsts</c>).
/// The following assumptions have been made: languageId &lt; 256 =&gt; needs 8 bits; unique color count &lt; 512 =&gt;
/// needs 9 bits.
/// </summary>
/// <remarks>
/// <para>The binary format is:</para>
/// <code>
///  3322 2222 2222 1111 1111 1100 0000 0000
///  1098 7654 3210 9876 5432 1098 7654 3210
/// -------------------------------------------
///  bbbb bbbb ffff ffff fFFF FBTT LLLL LLLL
/// </code>
/// <para>
/// L = language id (8 bits), T = standard token type (2 bits), B = balanced bracket (1 bit), F = font style (4 bits),
/// f = foreground color (9 bits), b = background color (8 bits).
/// </para>
/// <para>Threading: constants.</para>
/// </remarks>
internal static class MetadataConsts
{
    /// <summary>Language id mask.</summary>
    public const uint LanguageIdMask = 0b00000000_00000000_00000000_11111111;

    /// <summary>Token type mask.</summary>
    public const uint TokenTypeMask = 0b00000000_00000000_00000011_00000000;

    /// <summary>Balanced brackets mask.</summary>
    public const uint BalancedBracketsMask = 0b00000000_00000000_00000100_00000000;

    /// <summary>Font style mask.</summary>
    public const uint FontStyleMask = 0b00000000_00000000_01111000_00000000;

    /// <summary>Foreground mask.</summary>
    public const uint ForegroundMask = 0b00000000_11111111_10000000_00000000;

    /// <summary>Background mask.</summary>
    public const uint BackgroundMask = 0b11111111_00000000_00000000_00000000;

    /// <summary>Italic mask.</summary>
    public const uint ItalicMask = 0b00000000_00000000_00001000_00000000;

    /// <summary>Bold mask.</summary>
    public const uint BoldMask = 0b00000000_00000000_00010000_00000000;

    /// <summary>Underline mask.</summary>
    public const uint UnderlineMask = 0b00000000_00000000_00100000_00000000;

    /// <summary>Strikethrough mask.</summary>
    public const uint StrikethroughMask = 0b00000000_00000000_01000000_00000000;

    // Semantic tokens cannot set the language id, so we can use the first 8 bits for control purposes

    /// <summary>Semantic tokens: use the italic of the token.</summary>
    public const uint SemanticUseItalic = 0b00000000_00000000_00000000_00000001;

    /// <summary>Semantic tokens: use the bold of the token.</summary>
    public const uint SemanticUseBold = 0b00000000_00000000_00000000_00000010;

    /// <summary>Semantic tokens: use the underline of the token.</summary>
    public const uint SemanticUseUnderline = 0b00000000_00000000_00000000_00000100;

    /// <summary>Semantic tokens: use the strikethrough of the token.</summary>
    public const uint SemanticUseStrikethrough = 0b00000000_00000000_00000000_00001000;

    /// <summary>Semantic tokens: use the foreground of the token.</summary>
    public const uint SemanticUseForeground = 0b00000000_00000000_00000000_00010000;

    /// <summary>Semantic tokens: use the background of the token.</summary>
    public const uint SemanticUseBackground = 0b00000000_00000000_00000000_00100000;

    /// <summary>Language id offset.</summary>
    public const int LanguageIdOffset = 0;

    /// <summary>Token type offset.</summary>
    public const int TokenTypeOffset = 8;

    /// <summary>Balanced brackets offset.</summary>
    public const int BalancedBracketsOffset = 10;

    /// <summary>Font style offset.</summary>
    public const int FontStyleOffset = 11;

    /// <summary>Foreground offset.</summary>
    public const int ForegroundOffset = 15;

    /// <summary>Background offset.</summary>
    public const int BackgroundOffset = 24;
}
