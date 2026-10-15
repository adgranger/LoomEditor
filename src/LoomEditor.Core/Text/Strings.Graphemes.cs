// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

internal static partial class Strings
{
    /// <summary>Length (UTF-16 units) of the grapheme cluster starting at <paramref name="initialOffset"/> (Monaco: <c>nextCharLength</c>).</summary>
    public static int NextCharLength(string str, int initialOffset) => new GraphemeIterator(str, initialOffset).NextGraphemeLength();

    /// <summary>Length (UTF-16 units) of the grapheme cluster ending at <paramref name="initialOffset"/> (Monaco: <c>prevCharLength</c>).</summary>
    public static int PrevCharLength(string str, int initialOffset) => new GraphemeIterator(str, initialOffset).PrevGraphemeLength();

    /// <summary>Start and end offsets of the grapheme cluster containing <paramref name="offset"/> (Monaco: <c>getCharContainingOffset</c>).</summary>
    public static (int Start, int End) GetCharContainingOffset(string str, int offset)
    {
        // JS charCodeAt(str.length) is NaN (not a low surrogate); C# would throw, so guard the index.
        if (offset > 0 && offset < str.Length && IsLowSurrogate(str[offset]))
        {
            offset--;
        }

        var endOffset = offset + NextCharLength(str, offset);
        var startOffset = endOffset - PrevCharLength(str, endOffset);
        return (startOffset, endOffset);
    }

    /// <summary>Number of grapheme clusters in <paramref name="str"/> (Monaco: <c>charCount</c>).</summary>
    public static int CharCount(string str)
    {
        var iterator = new GraphemeIterator(str);
        var length = 0;
        while (!iterator.Eol())
        {
            length++;
            iterator.NextGraphemeLength();
        }

        return length;
    }

    /// <summary>Grapheme break type of a code point (Monaco: <c>getGraphemeBreakType</c>).</summary>
    public static GraphemeBreakType GetGraphemeBreakType(int codePoint) => GraphemeBreakTree.Instance.GetGraphemeBreakType(codePoint);

    /// <summary>
    /// True when there is a grapheme boundary between the two break types (Monaco: <c>breakBetweenGraphemeBreakType</c>).
    /// GB11 and GB12/13 are applied without state, exactly like Monaco.
    /// </summary>
    internal static bool BreakBetweenGraphemeBreakType(GraphemeBreakType breakTypeA, GraphemeBreakType breakTypeB)
    {
        // http://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules

        // Let's make the common case a bit faster
        if (breakTypeA == GraphemeBreakType.Other)
        {
            return breakTypeB != GraphemeBreakType.Extend && breakTypeB != GraphemeBreakType.SpacingMark;
        }

        // GB3: CR × LF. GB4: (Control | CR | LF) ÷. GB5: ÷ (Control | CR | LF)
        if (breakTypeA == GraphemeBreakType.CR && breakTypeB == GraphemeBreakType.LF)
        {
            return false; // GB3
        }

        if (breakTypeA is GraphemeBreakType.Control or GraphemeBreakType.CR or GraphemeBreakType.LF)
        {
            return true; // GB4
        }

        if (breakTypeB is GraphemeBreakType.Control or GraphemeBreakType.CR or GraphemeBreakType.LF)
        {
            return true; // GB5
        }

        // GB6: L × (L | V | LV | LVT). GB7: (LV | V) × (V | T). GB8: (LVT | T) × T
        if (breakTypeA == GraphemeBreakType.L
            && breakTypeB is GraphemeBreakType.L or GraphemeBreakType.V or GraphemeBreakType.LV or GraphemeBreakType.LVT)
        {
            return false; // GB6
        }

        if (breakTypeA is GraphemeBreakType.LV or GraphemeBreakType.V
            && breakTypeB is GraphemeBreakType.V or GraphemeBreakType.T)
        {
            return false; // GB7
        }

        if (breakTypeA is GraphemeBreakType.LVT or GraphemeBreakType.T && breakTypeB == GraphemeBreakType.T)
        {
            return false; // GB8
        }

        // GB9: × (Extend | ZWJ)
        if (breakTypeB is GraphemeBreakType.Extend or GraphemeBreakType.ZWJ)
        {
            return false; // GB9
        }

        // GB9a: × SpacingMark. GB9b: Prepend ×
        if (breakTypeB == GraphemeBreakType.SpacingMark)
        {
            return false; // GB9a
        }

        if (breakTypeA == GraphemeBreakType.Prepend)
        {
            return false; // GB9b
        }

        // GB11: \p{Extended_Pictographic} Extend* ZWJ × \p{Extended_Pictographic}
        if (breakTypeA == GraphemeBreakType.ZWJ && breakTypeB == GraphemeBreakType.ExtendedPictographic)
        {
            // Note: we are not implementing the rule entirely here to avoid introducing states
            return false; // GB11
        }

        // GB12: sot (RI RI)* RI × RI. GB13: [^RI] (RI RI)* RI × RI
        if (breakTypeA == GraphemeBreakType.RegionalIndicator && breakTypeB == GraphemeBreakType.RegionalIndicator)
        {
            // Note: we are not implementing the rule entirely here to avoid introducing states
            return false; // GB12 & GB13
        }

        // GB999: Any ÷ Any
        return true;
    }
}
