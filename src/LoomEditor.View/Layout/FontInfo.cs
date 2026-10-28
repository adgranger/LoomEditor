// Ported from microsoft/vscode: src/vs/editor/common/config/fontInfo.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>A <see cref="BareFontInfo"/> plus the measured character widths. Monaco: <c>FontInfo</c>.</summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Not ported: <c>version</c>, <c>isTrusted</c>.
/// <see cref="IsMonospace"/> is only true when <see cref="BareFontInfo.FontFeatureSettings"/> is exactly
/// <see cref="EditorFontOptions.LigaturesOff"/> (as in Monaco), so <c>"liga" 0, "calt" 0</c> counts as ligatures on.
/// </remarks>
internal sealed record FontInfo(
    double PixelRatio,
    string FontFamily,
    string FontWeight,
    double FontSize,
    int LineHeight,
    double LetterSpacing,
    string FontFeatureSettings,
    string FontVariationSettings,
    bool IsMonospace,
    double TypicalHalfwidthCharacterWidth,
    double TypicalFullwidthCharacterWidth,
    bool CanUseHalfwidthRightwardsArrow,
    double SpaceWidth,
    double MiddotWidth,
    double WsmiddotWidth,
    double MaxDigitWidth)
    : BareFontInfo(PixelRatio, FontFamily, FontWeight, FontSize, LineHeight, LetterSpacing, FontFeatureSettings, FontVariationSettings)
{
    /// <summary>Placeholder used until the control measures the real font (Monaco's test font, in Consolas).</summary>
    public static FontInfo Default { get; } = new(1, "Consolas", "normal", 14, 19, 1.5, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, 10, 20, true, 10, 10, 10, 10);
}
