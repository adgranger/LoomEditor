using System.Globalization;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering;

/// <summary>The font weight rules that the text measurers of both hosts share.</summary>
/// <remarks>Threading: stateless; safe from any thread.</remarks>
internal static class FontVariationSettings
{
    /// <summary>Reads the <c>wght</c> axis from a CSS <c>font-variation-settings</c> value (<c>"wght" 600, "wdth" 90</c>).</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="value">The axis value (truncated), or 0.</param>
    /// <returns><see langword="true"/> when the settings set the axis.</returns>
    public static bool TryGetWght(string settings, out int value)
    {
        value = 0;
        foreach (string item in settings.Split(','))
        {
            string trimmed = item.Trim();
            if (trimmed.Length > 6 && trimmed[0] is '\x27' or '"' && trimmed[5] == trimmed[0] && trimmed.AsSpan(1, 4).SequenceEqual("wght")
                && double.TryParse(trimmed.AsSpan(6), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                value = (int)d;
                return true;
            }
        }

        return false;
    }

    /// <summary>The base weight (1 to 999) of the editor font: the <c>wght</c> axis when set, else the weight name or number, else 400.</summary>
    /// <param name="bare">The font settings.</param>
    /// <returns>The weight.</returns>
    public static int ResolveBaseWeight(BareFontInfo bare)
    {
        if (TryGetWght(bare.FontVariationSettings, out int wght))
        {
            return Math.Clamp(wght, 1, 999);
        }

        return bare.FontWeight switch
        {
            "normal" => 400,
            "bold" => 700,
            _ => int.TryParse(bare.FontWeight, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? Math.Clamp(n, 1, 999) : 400,
        };
    }
}
