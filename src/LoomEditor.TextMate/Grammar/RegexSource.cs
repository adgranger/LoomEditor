// Ported from microsoft/vscode-textmate: src/utils.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>RegexSource</c>: back-references to the captures of another match (<c>$1</c>, <c>${1:/downcase}</c>).</summary>
/// <remarks>Threading: stateless; safe on any thread.</remarks>
internal static partial class RegexSource
{
    /// <summary><c>hasCaptures</c>: whether <paramref name="regexSource"/> contains a capture reference.</summary>
    public static bool HasCaptures(string? regexSource)
    {
        if (regexSource is null)
        {
            return false;
        }

        return CapturingRegexSource().IsMatch(regexSource);
    }

    /// <summary>
    /// <c>replaceCaptures</c>: replaces each capture reference with the text of that capture in
    /// <paramref name="captureSource"/> (leading dots removed). A reference to a missing capture is kept as is.
    /// </summary>
    public static string ReplaceCaptures(string regexSource, string captureSource, IReadOnlyList<OnigCaptureIndex> captureIndices)
    {
        ArgumentNullException.ThrowIfNull(regexSource);
        ArgumentNullException.ThrowIfNull(captureSource);
        ArgumentNullException.ThrowIfNull(captureIndices);

        return CapturingRegexSource().Replace(regexSource, match =>
        {
            string index = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (!int.TryParse(index, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int captureIndex)
                || captureIndex >= captureIndices.Count)
            {
                return match.Value;
            }

            OnigCaptureIndex capture = captureIndices[captureIndex];
            string result = JsSubstring(captureSource, capture.Start, capture.End);
            // Remove leading dots that would make the selector invalid
            while (result.Length > 0 && result[0] == '.')
            {
                result = result[1..];
            }

            return match.Groups[3].Value switch
            {
                "downcase" => result.ToLowerInvariant(),
                "upcase" => result.ToUpperInvariant(),
                _ => result,
            };
        });
    }

    // JavaScript's String.prototype.substring: both ends clamped to [0, length], swapped when start > end. An unmatched
    // group (-1, -1) gives "".
    private static string JsSubstring(string value, int start, int end)
    {
        start = Math.Clamp(start, 0, value.Length);
        end = Math.Clamp(end, 0, value.Length);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return value[start..end];
    }

    // CAPTURING_REGEX_SOURCE: /\$(\d+)|\${(\d+):\/(downcase|upcase)}/g, with ASCII digits.
    [GeneratedRegex(@"\$([0-9]+)|\$\{([0-9]+):/(downcase|upcase)\}", RegexOptions.CultureInvariant)]
    private static partial Regex CapturingRegexSource();
}
