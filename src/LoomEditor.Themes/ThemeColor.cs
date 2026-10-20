using System.Globalization;

namespace LoomEditor.Themes;

/// <summary>
/// A color with alpha, red, green and blue channels (0–255), independent of any UI framework.
/// </summary>
/// <remarks>Thread safety: immutable value, safe to share between threads.</remarks>
/// <param name="A">Alpha (opacity). 255 is opaque.</param>
/// <param name="R">Red.</param>
/// <param name="G">Green.</param>
/// <param name="B">Blue.</param>
public readonly record struct ThemeColor(byte A, byte R, byte G, byte B)
{
    /// <summary>Creates an opaque color.</summary>
    public static ThemeColor FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>
    /// Parses a color written like in VS Code themes: <c>#RGB</c>, <c>#RGBA</c>, <c>#RRGGBB</c> or <c>#RRGGBBAA</c> (VS Code: <c>Color.fromHex</c>).
    /// </summary>
    /// <exception cref="FormatException">The text is not a valid color.</exception>
    public static ThemeColor Parse(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        if (hex.Length < 2 || hex[0] != '#')
        {
            throw new FormatException($"'{hex}' is not a color: expected #RGB, #RGBA, #RRGGBB or #RRGGBBAA.");
        }

        ReadOnlySpan<char> digits = hex.AsSpan(1);
        return digits.Length switch
        {
            3 => FromRgb(ParseShortChannel(digits[0]), ParseShortChannel(digits[1]), ParseShortChannel(digits[2])),
            4 => new ThemeColor(ParseShortChannel(digits[3]), ParseShortChannel(digits[0]), ParseShortChannel(digits[1]), ParseShortChannel(digits[2])),
            6 => FromRgb(ParseChannel(digits[0..2]), ParseChannel(digits[2..4]), ParseChannel(digits[4..6])),
            8 => new ThemeColor(ParseChannel(digits[6..8]), ParseChannel(digits[0..2]), ParseChannel(digits[2..4]), ParseChannel(digits[4..6])),
            _ => throw new FormatException($"'{hex}' is not a color: expected #RGB, #RGBA, #RRGGBB or #RRGGBBAA."),
        };
    }

    /// <summary>Formats the color as <c>#RRGGBBAA</c>.</summary>
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    private static byte ParseChannel(ReadOnlySpan<char> twoDigits) =>
        byte.Parse(twoDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    // "#abc" means "#aabbcc": each digit is repeated.
    private static byte ParseShortChannel(char digit) => (byte)(ParseChannel([digit]) * 17);
}
