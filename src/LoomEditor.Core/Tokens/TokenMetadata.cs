// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// Reads the fields of a token metadata (Monaco: <c>TokenMetadata</c>). Not ported: <c>getClassNameFromMetadata</c> and
/// <c>getInlineStyleFromMetadata</c> (CSS).
/// </summary>
/// <remarks>Threading: stateless.</remarks>
internal static class TokenMetadata
{
    /// <summary><c>getLanguageId</c>.</summary>
    public static int GetLanguageId(uint metadata) =>
        (int)((metadata & MetadataConsts.LanguageIdMask) >> MetadataConsts.LanguageIdOffset);

    /// <summary><c>getTokenType</c>.</summary>
    public static StandardTokenType GetTokenType(uint metadata) =>
        (StandardTokenType)((metadata & MetadataConsts.TokenTypeMask) >> MetadataConsts.TokenTypeOffset);

    /// <summary><c>containsBalancedBrackets</c>.</summary>
    public static bool ContainsBalancedBrackets(uint metadata) => (metadata & MetadataConsts.BalancedBracketsMask) != 0;

    /// <summary><c>getFontStyle</c>.</summary>
    public static FontStyle GetFontStyle(uint metadata) =>
        (FontStyle)((metadata & MetadataConsts.FontStyleMask) >> MetadataConsts.FontStyleOffset);

    /// <summary><c>getForeground</c>: a color id.</summary>
    public static int GetForeground(uint metadata) =>
        (int)((metadata & MetadataConsts.ForegroundMask) >> MetadataConsts.ForegroundOffset);

    /// <summary><c>getBackground</c>: a color id.</summary>
    public static int GetBackground(uint metadata) =>
        (int)((metadata & MetadataConsts.BackgroundMask) >> MetadataConsts.BackgroundOffset);

    /// <summary><c>getPresentationFromMetadata</c>.</summary>
    public static TokenPresentation GetPresentationFromMetadata(uint metadata)
    {
        int foreground = GetForeground(metadata);
        FontStyle fontStyle = GetFontStyle(metadata);

        return new TokenPresentation(
            foreground,
            (fontStyle & FontStyle.Italic) != 0,
            (fontStyle & FontStyle.Bold) != 0,
            (fontStyle & FontStyle.Underline) != 0,
            (fontStyle & FontStyle.Strikethrough) != 0);
    }
}
