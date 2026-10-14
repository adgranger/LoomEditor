// Ported from microsoft/vscode: src/vs/editor/browser/config/fontMeasurements.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Builds a <see cref="FontInfo"/> by measuring a fixed set of characters. Monaco: <c>FontMeasurements.readFontInfo</c>
/// and <c>_actualReadFontInfo</c>.
/// </summary>
/// <remarks>
/// Threading: stateless; thread-safe if the reader is. Behavior difference: when a key width is <c>&lt;= 2</c> Monaco
/// clears its cache and measures again before clamping to 5; LoomEditor has no cache, so it only clamps.
/// </remarks>
internal static class FontMeasurements
{
    private const string MonospaceTestChars = "|/-_ilm%";

    /// <summary>Measures <paramref name="bare"/> with <paramref name="reader"/>.</summary>
    public static FontInfo Read(BareFontInfo bare, ICharacterWidthReader reader, double pixelRatio)
    {
        // Same characters, same order as Monaco. `monospace` collects the widths used by the monospace test.
        var monospace = new List<double>();

        double Measure(string chr, CharacterStyle style, bool inMonospaceTest)
        {
            double width = reader.GetWidth(chr, style);
            if (inMonospaceTest)
            {
                monospace.Add(width);
            }

            return width;
        }

        double typicalHalfwidth = Measure("n", CharacterStyle.Regular, true);
        double typicalFullwidth = Measure("ｍ", CharacterStyle.Regular, false);
        double space = Measure(" ", CharacterStyle.Regular, true);
        double maxDigitWidth = 0;
        for (char digit = '0'; digit <= '9'; digit++)
        {
            maxDigitWidth = Math.Max(maxDigitWidth, Measure(digit.ToString(), CharacterStyle.Regular, true));
        }

        double rightwardsArrow = Measure("→", CharacterStyle.Regular, true);
        double halfwidthRightwardsArrow = Measure("￫", CharacterStyle.Regular, false);
        double middot = Measure("·", CharacterStyle.Regular, true);
        double wsmiddot = Measure("⸱", CharacterStyle.Regular, false);

        foreach (char c in MonospaceTestChars)
        {
            string s = c.ToString();
            Measure(s, CharacterStyle.Regular, true);
            Measure(s, CharacterStyle.Italic, true);
            Measure(s, CharacterStyle.Bold, true);
        }

        // LoomEditor has no ligature option yet, so the monospace test starts at `true`.
        bool isMonospace = true;
        double referenceWidth = monospace[0];
        for (int i = 1; i < monospace.Count; i++)
        {
            double diff = referenceWidth - monospace[i];
            if (diff is < -0.001 or > 0.001)
            {
                isMonospace = false;
                break;
            }
        }

        bool canUseHalfwidthRightwardsArrow = true;
        if (isMonospace && halfwidthRightwardsArrow != referenceWidth)
        {
            // Using a halfwidth rightwards arrow would break monospace.
            canUseHalfwidthRightwardsArrow = false;
        }

        if (halfwidthRightwardsArrow > rightwardsArrow)
        {
            // Using a halfwidth rightwards arrow would paint a larger arrow than a regular rightwards arrow.
            canUseHalfwidthRightwardsArrow = false;
        }

        if (typicalHalfwidth <= 2 || typicalFullwidth <= 2 || space <= 2 || maxDigitWidth <= 2)
        {
            // Monaco bug 14341: the font could not be read.
            typicalHalfwidth = Math.Max(typicalHalfwidth, 5);
            typicalFullwidth = Math.Max(typicalFullwidth, 5);
            space = Math.Max(space, 5);
            middot = Math.Max(middot, 5);
            wsmiddot = Math.Max(wsmiddot, 5);
            maxDigitWidth = Math.Max(maxDigitWidth, 5);
        }

        return new FontInfo(
            pixelRatio, bare.FontFamily, bare.FontWeight, bare.FontSize, bare.LineHeight, bare.LetterSpacing,
            isMonospace, typicalHalfwidth, typicalFullwidth, canUseHalfwidthRightwardsArrow,
            space, middot, wsmiddot, maxDigitWidth);
    }
}
