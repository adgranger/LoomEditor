using System.Globalization;

namespace LoomEditor.Rendering;

/// <summary>
/// The cluster hit-test rules that the line layouts of both hosts share: which clusters may be split at a character, and which offset of a cluster is nearest to a point.
/// </summary>
/// <remarks>Threading: stateless; safe from any thread.</remarks>
internal static class LineLayoutClusters
{
    /// <summary>Graphemes, emoji and combining sequences are not split: only plain BMP characters are.</summary>
    /// <param name="text">The text of the line.</param>
    /// <param name="start">The first character of the cluster.</param>
    /// <param name="count">The number of characters of the cluster.</param>
    /// <returns><see langword="true"/> when the cluster has no surrogate and no combining mark.</returns>
    public static bool IsSplittableCluster(string text, int start, int count)
    {
        for (int i = start; i < start + count; i++)
        {
            char c = text[i];
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

    /// <summary>The offset of the cluster whose x is nearest to <paramref name="x"/>; ties go to the lower offset.</summary>
    /// <param name="getCharacterX">The x of the left edge of a character offset.</param>
    /// <param name="start">The first character of the cluster.</param>
    /// <param name="count">The number of characters of the cluster.</param>
    /// <param name="x">The point.</param>
    /// <returns>An offset from <paramref name="start"/> to <c>start + count</c>.</returns>
    public static int NearestOffset(Func<int, double> getCharacterX, int start, int count, double x)
    {
        int best = start;
        double bestDistance = double.MaxValue;
        for (int offset = start; offset <= start + count; offset++)
        {
            double distance = Math.Abs(getCharacterX(offset) - x);
            if (distance < bestDistance)
            {
                best = offset;
                bestDistance = distance;
            }
        }

        return best;
    }
}
