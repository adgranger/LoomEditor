using System.Globalization;

namespace LoomEditor.TextMate.Parsing;

/// <summary>
/// JavaScript's <c>parseInt(s, 10)</c> and <c>parseFloat(s)</c>: leading whitespace is skipped, the longest valid
/// prefix is read, and <see cref="double.NaN"/> is returned when there is none.
/// </summary>
/// <remarks>Threading: stateless; safe on any thread.</remarks>
internal static class JsNumbers
{
    /// <summary><c>parseInt(s, 10)</c>.</summary>
    public static double ParseInt(string s)
    {
        int i = SkipWhitespace(s, 0);
        int start = i;
        if (i < s.Length && (s[i] == '+' || s[i] == '-'))
        {
            i++;
        }

        int digits = i;
        while (i < s.Length && char.IsAsciiDigit(s[i]))
        {
            i++;
        }

        if (i == digits)
        {
            return double.NaN;
        }

        return double.Parse(s.AsSpan(start, i - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    /// <summary><c>parseFloat(s)</c>.</summary>
    public static double ParseFloat(string s)
    {
        int i = SkipWhitespace(s, 0);
        int start = i;
        if (i < s.Length && (s[i] == '+' || s[i] == '-'))
        {
            i++;
        }

        if (string.CompareOrdinal(s, i, "Infinity", 0, 8) == 0)
        {
            return s[start] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
        }

        int integerDigits = CountDigits(s, i);
        i += integerDigits;
        int fractionDigits = 0;
        if (i < s.Length && s[i] == '.')
        {
            fractionDigits = CountDigits(s, i + 1);
            if (integerDigits > 0 || fractionDigits > 0)
            {
                i += 1 + fractionDigits;
            }
        }

        if (integerDigits == 0 && fractionDigits == 0)
        {
            return double.NaN;
        }

        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            int j = i + 1;
            if (j < s.Length && (s[j] == '+' || s[j] == '-'))
            {
                j++;
            }

            int exponentDigits = CountDigits(s, j);
            if (exponentDigits > 0)
            {
                i = j + exponentDigits;
            }
        }

        return double.Parse(s.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static int CountDigits(string s, int i)
    {
        int count = 0;
        while (i + count < s.Length && char.IsAsciiDigit(s[i + count]))
        {
            count++;
        }

        return count;
    }

    // JavaScript's whitespace (StrWhiteSpaceChar): white space and line terminators.
    private static int SkipWhitespace(string s, int i)
    {
        while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == '﻿'))
        {
            i++;
        }

        return i;
    }
}
