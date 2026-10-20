// Ported from microsoft/vscode: src/vs/workbench/services/themes/common/colorThemeData.ts (normalizeColor), src/vs/base/common/color.ts (Color.fromHex)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;

namespace LoomEditor.Themes;

/// <summary>Color text helpers of the theme loader.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class ColorNormalization
{
    /// <summary>
    /// <c>normalizeColor</c>: <c>#RGB</c>, <c>#RGBA</c>, <c>#RRGGBB</c> or <c>#RRGGBBAA</c> as upper case <c>#RRGGBB</c>
    /// or <c>#RRGGBBAA</c> (an <c>FF</c> alpha is dropped), or <see langword="null"/> when the text is not such a color.
    /// </summary>
    public static string? Normalize(string? color)
    {
        if (string.IsNullOrEmpty(color))
        {
            return null;
        }

        int len = color.Length;
        if (color[0] != '#' || (len != 4 && len != 5 && len != 7 && len != 9))
        {
            return null;
        }

        var result = new StringBuilder("#");
        for (int i = 1; i < len; i++)
        {
            char c = color[i];
            if (!char.IsAsciiHexDigit(c))
            {
                return null;
            }

            char upper = char.ToUpperInvariant(c);
            result.Append(upper);
            if (len is 4 or 5)
            {
                result.Append(upper);
            }
        }

        if (result.Length == 9 && result[7] == 'F' && result[8] == 'F')
        {
            result.Length = 7;
        }

        return result.ToString();
    }

    /// <summary><c>Color.Format.CSS.formatHexA(color, compact: true)</c> then <see cref="Normalize(string?)"/>: <c>#RRGGBB</c> when opaque, else <c>#RRGGBBAA</c>.</summary>
    public static string Normalize(ThemeColor color) =>
        color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    /// <summary>
    /// <c>Color.fromHex</c>: like VS Code, a text that is not a color (no <c>#</c>, a wrong length) is red and an
    /// invalid digit counts as 0 (<c>_parseHexDigit</c>).
    /// </summary>
    public static ThemeColor FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        if (hex.Length == 0 || hex[0] != '#')
        {
            return Red;
        }

        return hex.Length switch
        {
            7 => new ThemeColor(255, Pair(hex, 1), Pair(hex, 3), Pair(hex, 5)),
            9 => new ThemeColor(Pair(hex, 7), Pair(hex, 1), Pair(hex, 3), Pair(hex, 5)),
            4 => new ThemeColor(255, Doubled(hex[1]), Doubled(hex[2]), Doubled(hex[3])),
            5 => new ThemeColor(Doubled(hex[4]), Doubled(hex[1]), Doubled(hex[2]), Doubled(hex[3])),
            _ => Red,
        };
    }

    private static ThemeColor Red => new(255, 255, 0, 0);

    private static byte Pair(string hex, int index) => (byte)((16 * Digit(hex[index])) + Digit(hex[index + 1]));

    private static byte Doubled(char c) => (byte)(17 * Digit(c));

    private static int Digit(char c) => char.IsAsciiHexDigit(c) ? Convert.ToInt32(c.ToString(), 16) : 0;
}
