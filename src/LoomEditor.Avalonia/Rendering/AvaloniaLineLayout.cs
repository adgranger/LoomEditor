using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using LoomEditor.Rendering;

namespace LoomEditor.Avalonia.Rendering;

/// <summary>An <see cref="ILineLayout"/> backed by one Avalonia <see cref="global::Avalonia.Media.TextFormatting.TextLayout"/>.</summary>
/// <remarks>Threading: UI thread only. Disposing it disposes the underlying <see cref="global::Avalonia.Media.TextFormatting.TextLayout"/>.</remarks>
internal sealed class AvaloniaLineLayout : ILineLayout
{
    /// <summary>Wraps <paramref name="layout"/> and takes ownership of it.</summary>
    public AvaloniaLineLayout(string text, TextLayout layout)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(layout);

        Text = text;
        TextLayout = layout;
        // The caret at the end of the text counts trailing whitespace.
        Width = text.Length == 0 ? 0 : layout.HitTestTextPosition(text.Length).X;
    }

    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public double Width { get; }

    /// <summary>The Avalonia layout, for drawing.</summary>
    public TextLayout TextLayout { get; }

    /// <inheritdoc />
    public double GetCharacterX(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Text.Length);

        return index == Text.Length ? Width : TextLayout.HitTestTextPosition(index).X;
    }

    /// <inheritdoc />
    public int HitTest(double x)
    {
        if (Text.Length == 0)
        {
            return 0;
        }

        // Avalonia: the cluster under the point; TextPosition is its start, plus its length when the point is on its
        // trailing half (so IsTrailing, only set beyond the end of the text, adds nothing).
        TextHitTestResult hit = TextLayout.HitTestPoint(new Point(x, 0));
        int start = Math.Clamp(hit.CharacterHit.FirstCharacterIndex, 0, Text.Length);
        int count = ClusterLength(start);
        if (count > 1 && IsSplittableCluster(start, count))
        {
            // A ligature: the hit is on the whole cluster, so pick the closest caret position inside it.
            return NearestOffset(start, count, x);
        }

        int index = Math.Clamp(hit.TextPosition, 0, Text.Length);
        System.Diagnostics.Debug.Assert(index == 0 || index == Text.Length || !char.IsLowSurrogate(Text[index]), "HitTest split a surrogate pair");
        return index;
    }

    // The number of UTF-16 units from start to the next caret stop.
    private int ClusterLength(int start)
    {
        if (start >= Text.Length)
        {
            return 0;
        }

        CharacterHit next = TextLayout.TextLines[0].GetNextCaretCharacterHit(new CharacterHit(start));
        return Math.Clamp(next.FirstCharacterIndex + next.TrailingLength - start, 1, Text.Length - start);
    }

    // Graphemes, emoji and combining sequences are not split: only plain BMP characters are.
    private bool IsSplittableCluster(int start, int count)
    {
        for (int i = start; i < start + count; i++)
        {
            char c = Text[i];
            if (char.IsSurrogate(c))
            {
                return false;
            }

            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
            {
                return false;
            }
        }

        return true;
    }

    // Ties go to the lower offset.
    private int NearestOffset(int start, int count, double x)
    {
        int best = start;
        double bestDistance = double.MaxValue;
        for (int offset = start; offset <= start + count; offset++)
        {
            double distance = Math.Abs(GetCharacterX(offset) - x);
            if (distance < bestDistance)
            {
                best = offset;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <inheritdoc />
    public void Dispose() => TextLayout.Dispose();
}
