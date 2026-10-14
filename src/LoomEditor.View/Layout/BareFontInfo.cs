// Ported from microsoft/vscode: src/vs/editor/common/config/fontInfo.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// What the user asked for: font family, weight, size, line height and letter spacing. Monaco: <c>BareFontInfo</c>.
/// </summary>
/// <remarks>
/// Threading: immutable, safe from any thread.
/// Not ported: <c>fontFeatureSettings</c>, <c>fontVariationSettings</c>, editor zoom, <c>getId</c>,
/// <c>getMassagedFontFamily</c>. The Windows values of Monaco's constants are used.
/// </remarks>
internal record BareFontInfo(
    double PixelRatio,
    string FontFamily,
    string FontWeight,
    double FontSize,
    int LineHeight,
    double LetterSpacing)
{
    /// <summary>Monaco: <c>GOLDEN_LINE_HEIGHT_RATIO</c> (Windows value).</summary>
    public const double GoldenLineHeightRatio = 1.35;

    /// <summary>Monaco: <c>MINIMUM_LINE_HEIGHT</c>.</summary>
    public const int MinimumLineHeight = 8;

    /// <summary>Monaco: <c>DEFAULT_WINDOWS_FONT_FAMILY</c> (<c>EDITOR_FONT_DEFAULTS.fontFamily</c> on Windows).</summary>
    public const string DefaultWindowsFontFamily = "Consolas, 'Courier New', monospace";

    /// <summary>
    /// Monaco: <c>BareFontInfo._create</c>. <paramref name="lineHeight"/> is <c>0</c> (golden ratio), a multiplier of
    /// the font size when below 8, or a size in pixels; the result is rounded and at least 8.
    /// </summary>
    public static BareFontInfo Create(
        string fontFamily, string fontWeight, double fontSize, double lineHeight, double letterSpacing, double pixelRatio)
    {
        if (lineHeight == 0)
        {
            lineHeight = GoldenLineHeightRatio * fontSize;
        }
        else if (lineHeight < MinimumLineHeight)
        {
            // Values too small to be line heights in pixels are in ems.
            lineHeight *= fontSize;
        }

        int rounded = (int)Math.Round(lineHeight, MidpointRounding.AwayFromZero);
        if (rounded < MinimumLineHeight)
        {
            rounded = MinimumLineHeight;
        }

        return new BareFontInfo(pixelRatio, fontFamily, fontWeight, fontSize, rounded, letterSpacing);
    }
}
