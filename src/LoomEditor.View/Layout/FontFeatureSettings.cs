using System.Globalization;

namespace LoomEditor.View.Layout;

/// <summary>One OpenType feature of a CSS <c>font-feature-settings</c> list: a four-character tag and its value.</summary>
internal readonly record struct FontFeature(string Tag, int Value);

/// <summary>Parses the CSS <c>font-feature-settings</c> syntax (LoomEditor-specific; Monaco hands the string to the browser).</summary>
/// <remarks>Threading: stateless, thread-safe.</remarks>
internal static class FontFeatureSettings
{
    /// <summary>
    /// A comma-separated list of <c>'tag'</c> or <c>"tag"</c> (exactly four ASCII characters), each optionally followed by
    /// <c>on</c>, <c>off</c> or a non-negative integer (default 1). A malformed item is skipped; <c>normal</c> or an
    /// empty string gives an empty list. The order is kept.
    /// </summary>
    public static IReadOnlyList<FontFeature> Parse(string? settings)
    {
        var result = new List<FontFeature>();
        if (string.IsNullOrWhiteSpace(settings))
        {
            return result;
        }

        foreach (string raw in settings.Split(','))
        {
            string item = raw.Trim();
            if (item.Length < 6 || (item[0] != '\'' && item[0] != '"') || item[5] != item[0])
            {
                continue;
            }

            string tag = item.Substring(1, 4);
            if (!IsAscii(tag))
            {
                continue;
            }

            string rest = item.Substring(6).Trim();
            int value;
            if (rest.Length == 0 || rest == "on")
            {
                value = 1;
            }
            else if (rest == "off")
            {
                value = 0;
            }
            else if (!IsDigits(rest) || !int.TryParse(rest, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                continue;
            }

            result.Add(new FontFeature(tag, value));
        }

        return result;
    }

    private static bool IsAscii(string s)
    {
        foreach (char c in s)
        {
            if (c is < ' ' or > '~' || c is '\'' or '"')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDigits(string s)
    {
        foreach (char c in s)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
