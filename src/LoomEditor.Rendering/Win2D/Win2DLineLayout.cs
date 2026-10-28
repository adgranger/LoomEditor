using System.Globalization;
using Microsoft.Graphics.Canvas.Text;

namespace LoomEditor.Rendering.Win2D;

/// <summary>An <see cref="ILineLayout"/> backed by one DirectWrite text layout.</summary>
/// <remarks>Threading: UI thread only. Disposing it disposes the underlying <see cref="CanvasTextLayout"/>.</remarks>
internal sealed class Win2DLineLayout : ILineLayout
{
    /// <summary>Wraps <paramref name="layout"/> and takes ownership of it.</summary>
    public Win2DLineLayout(string text, CanvasTextLayout layout)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(layout);

        Text = text;
        TextLayout = layout;
        // LayoutBounds.Width ignores trailing whitespace; the caret at the end of the text does not.
        Width = text.Length == 0 ? 0 : layout.GetCaretPosition(text.Length, false).X;
    }

    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public double Width { get; }

    /// <summary>The DirectWrite layout, for drawing.</summary>
    public CanvasTextLayout TextLayout { get; }

    /// <inheritdoc />
    public double GetCharacterX(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Text.Length);

        return index == Text.Length ? Width : TextLayout.GetCaretPosition(index, false).X;
    }

    /// <inheritdoc />
    public int HitTest(double x)
    {
        if (Text.Length == 0)
        {
            return 0;
        }

        // DirectWrite: the cluster under the point, and whether the point is on its trailing half (or beyond the end).
        TextLayout.HitTest((float)x, 0, out CanvasTextLayoutRegion region, out bool isTrailingHit);
        if (region.CharacterCount > 1 && IsSplittableCluster(region.CharacterIndex, region.CharacterCount))
        {
            // A ligature: DirectWrite returns the whole cluster, so pick the closest caret position inside it.
            return NearestOffset(region.CharacterIndex, region.CharacterCount, x);
        }

        int index = isTrailingHit ? region.CharacterIndex + region.CharacterCount : region.CharacterIndex;
        index = Math.Clamp(index, 0, Text.Length);
        System.Diagnostics.Debug.Assert(index == 0 || index == Text.Length || !char.IsLowSurrogate(Text[index]), "HitTest split a surrogate pair");
        return index;
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
