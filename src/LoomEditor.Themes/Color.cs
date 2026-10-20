// Ported from microsoft/vscode: src/vs/base/common/color.ts (RGBA, HSLA, Color: the parts that the theme transforms need)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>Monaco's <c>RGBA</c>: integer channels in 0–255 and an alpha in 0–1 rounded to 3 decimals.</summary>
/// <remarks>Threading: immutable value; thread-safe.</remarks>
internal readonly record struct Rgba
{
    public Rgba(double r, double g, double b, double a = 1)
    {
        // Monaco: `Math.min(255, Math.max(0, r)) | 0` (truncation)
        R = (int)Math.Min(255, Math.Max(0, r));
        G = (int)Math.Min(255, Math.Max(0, g));
        B = (int)Math.Min(255, Math.Max(0, b));
        A = Color.RoundFloat(Math.Max(Math.Min(1, a), 0), 3);
    }

    public int R { get; }

    public int G { get; }

    public int B { get; }

    public double A { get; }
}

/// <summary>Monaco's <c>HSLA</c>: hue 0–360 (integer), saturation, lightness and alpha in 0–1 rounded to 3 decimals.</summary>
/// <remarks>Threading: immutable value; thread-safe.</remarks>
internal readonly record struct Hsla
{
    public Hsla(double h, double s, double l, double a)
    {
        H = (int)Math.Max(Math.Min(360, h), 0);
        S = Color.RoundFloat(Math.Max(Math.Min(1, s), 0), 3);
        L = Color.RoundFloat(Math.Max(Math.Min(1, l), 0), 3);
        A = Color.RoundFloat(Math.Max(Math.Min(1, a), 0), 3);
    }

    public int H { get; }

    public double S { get; }

    public double L { get; }

    public double A { get; }

    /// <summary>Converts an RGB color to HSL (Monaco: <c>HSLA.fromRGBA</c>).</summary>
    public static Hsla FromRgba(Rgba rgba)
    {
        double r = rgba.R / 255.0;
        double g = rgba.G / 255.0;
        double b = rgba.B / 255.0;
        double a = rgba.A;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double h = 0;
        double s = 0;
        double l = (min + max) / 2;
        double chroma = max - min;

        if (chroma > 0)
        {
            s = Math.Min(l <= 0.5 ? chroma / (2 * l) : chroma / (2 - (2 * l)), 1);

            // JavaScript's switch compares with ===: the first case that matches wins
            if (max == r)
            {
                h = ((g - b) / chroma) + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                h = ((b - r) / chroma) + 2;
            }
            else
            {
                h = ((r - g) / chroma) + 4;
            }

            h *= 60;
            h = Color.Round(h);
        }

        return new Hsla(h, s, l, a);
    }

    /// <summary>Converts an HSL color to RGB (Monaco: <c>HSLA.toRGBA</c>).</summary>
    public static Rgba ToRgba(Hsla hsla)
    {
        double h = hsla.H / 360.0;
        double s = hsla.S;
        double l = hsla.L;
        double a = hsla.A;
        double r;
        double g;
        double b;

        if (s == 0)
        {
            r = g = b = l; // achromatic
        }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
            double p = (2 * l) - q;
            r = Hue2Rgb(p, q, h + (1.0 / 3));
            g = Hue2Rgb(p, q, h);
            b = Hue2Rgb(p, q, h - (1.0 / 3));
        }

        return new Rgba(Color.Round(r * 255), Color.Round(g * 255), Color.Round(b * 255), a);
    }

    private static double Hue2Rgb(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6)
        {
            return p + ((q - p) * 6 * t);
        }

        if (t < 1.0 / 2)
        {
            return q;
        }

        if (t < 2.0 / 3)
        {
            return p + ((q - p) * ((2.0 / 3) - t) * 6);
        }

        return p;
    }
}

/// <summary>
/// Monaco's <c>Color</c>, the subset the theme transforms use: <c>lighten</c>, <c>darken</c>, <c>transparent</c>,
/// <c>getLighterColor</c> and <c>getDarkerColor</c>. Not ported: <c>HSVA</c>, <c>blend</c>, <c>mix</c>,
/// <c>makeOpaque</c>, contrast ratios and the formatters.
/// </summary>
/// <remarks>
/// Every <c>Math.round</c> of Monaco is <c>Math.Floor(x + 0.5)</c> here (JavaScript rounds .5 up).
/// Threading: immutable; thread-safe.
/// </remarks>
internal sealed class Color
{
    private readonly Hsla? _hsla;

    public Color(Rgba rgba)
    {
        Rgba = rgba;
    }

    public Color(Hsla hsla)
    {
        _hsla = hsla;
        Rgba = Hsla.ToRgba(hsla);
    }

    public Rgba Rgba { get; }

    /// <summary>The HSLA the color was built from, or the one computed from its RGBA.</summary>
    public Hsla Hsla => _hsla ?? Hsla.FromRgba(Rgba);

    /// <summary>Monaco: <c>Math.round</c>.</summary>
    internal static double Round(double value) => Math.Floor(value + 0.5);

    /// <summary>Monaco: <c>roundFloat</c>.</summary>
    internal static double RoundFloat(double number, int decimalPoints)
    {
        double decimalFactor = Math.Pow(10, decimalPoints);
        return Round(number * decimalFactor) / decimalFactor;
    }

    /// <summary>Converts a theme color (alpha 0–255) to a <see cref="Color"/> (alpha 0–1).</summary>
    public static Color FromThemeColor(ThemeColor color) => new(new Rgba(color.R, color.G, color.B, color.A / 255.0));

    /// <summary>Converts back to a theme color (alpha × 255, rounded).</summary>
    public ThemeColor ToThemeColor() =>
        new((byte)Round(Rgba.A * 255), (byte)Rgba.R, (byte)Rgba.G, (byte)Rgba.B);

    /// <summary>The relative luminance in 0–1 (http://www.w3.org/TR/WCAG20/#relativeluminancedef), rounded to 4 decimals.</summary>
    public double GetRelativeLuminance()
    {
        double r = RelativeLuminanceForComponent(Rgba.R);
        double g = RelativeLuminanceForComponent(Rgba.G);
        double b = RelativeLuminanceForComponent(Rgba.B);
        double luminance = (0.2126 * r) + (0.7152 * g) + (0.0722 * b);

        return RoundFloat(luminance, 4);
    }

    public bool IsLighterThan(Color another) => GetRelativeLuminance() > another.GetRelativeLuminance();

    public bool IsDarkerThan(Color another) => GetRelativeLuminance() < another.GetRelativeLuminance();

    public Color Lighten(double factor) => new(new Hsla(Hsla.H, Hsla.S, Hsla.L + (Hsla.L * factor), Hsla.A));

    public Color Darken(double factor) => new(new Hsla(Hsla.H, Hsla.S, Hsla.L - (Hsla.L * factor), Hsla.A));

    public Color Transparent(double factor) => new(new Rgba(Rgba.R, Rgba.G, Rgba.B, Rgba.A * factor));

    /// <summary>Monaco: <c>Color.getLighterColor</c> (a factor of 0 or <see langword="null"/> means 0.5, like JavaScript's <c>factor ? factor : 0.5</c>).</summary>
    public static Color GetLighterColor(Color of, Color relative, double? factor = null)
    {
        if (of.IsLighterThan(relative))
        {
            return of;
        }

        double f = factor is { } given && given != 0 ? given : 0.5;
        double lum1 = of.GetRelativeLuminance();
        double lum2 = relative.GetRelativeLuminance();
        f = f * (lum2 - lum1) / lum2;
        return of.Lighten(f);
    }

    /// <summary>Monaco: <c>Color.getDarkerColor</c>.</summary>
    public static Color GetDarkerColor(Color of, Color relative, double? factor = null)
    {
        if (of.IsDarkerThan(relative))
        {
            return of;
        }

        double f = factor is { } given && given != 0 ? given : 0.5;
        double lum1 = of.GetRelativeLuminance();
        double lum2 = relative.GetRelativeLuminance();
        f = f * (lum1 - lum2) / lum1;
        return of.Darken(f);
    }

    private static double RelativeLuminanceForComponent(int color)
    {
        double c = color / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
