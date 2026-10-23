namespace LoomEditor.Themes.HighContrast;

/// <summary>
/// The WCAG 2.1 contrast ratio between two theme colors. No Monaco counterpart; the luminance comes from
/// <see cref="Color.GetRelativeLuminance"/>, which rounds to 4 decimals, so the result is approximate.
/// </summary>
/// <remarks>Threading: stateless, safe to call from any thread.</remarks>
internal static class ContrastRatio
{
    /// <summary>WCAG AA minimum for normal text.</summary>
    internal const double AaNormalText = 4.5;

    /// <summary>WCAG AAA minimum for normal text.</summary>
    internal const double AaaNormalText = 7.0;

    /// <summary>WCAG minimum for non-text elements (cursor, borders).</summary>
    internal const double NonText = 3.0;

    /// <summary>
    /// The contrast ratio, from 1 to 21. A translucent foreground is flattened over the background; the alpha of
    /// the background is ignored (it is taken as opaque).
    /// </summary>
    internal static double Between(ThemeColor foreground, ThemeColor background)
    {
        double a = foreground.A / 255.0;
        ThemeColor flattened = ThemeColor.FromRgb(
            Blend(foreground.R, background.R, a),
            Blend(foreground.G, background.G, a),
            Blend(foreground.B, background.B, a));

        double l1 = Color.FromThemeColor(flattened).GetRelativeLuminance();
        double l2 = Color.FromThemeColor(ThemeColor.FromRgb(background.R, background.G, background.B)).GetRelativeLuminance();
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    private static byte Blend(byte fg, byte bg, double alpha) => (byte)Math.Round((fg * alpha) + (bg * (1 - alpha)));
}
