using System.Globalization;

namespace LoomEditor.Gallery;

/// <summary>Statistics of the intervals between rendered frames.</summary>
/// <param name="Count">Number of intervals.</param>
/// <param name="AverageFps">Average frames per second.</param>
/// <param name="P50">Median interval, in milliseconds.</param>
/// <param name="P95">95th percentile interval, in milliseconds.</param>
/// <param name="Max">Longest interval, in milliseconds.</param>
/// <param name="OverBudget">Number of intervals longer than <see cref="BudgetMilliseconds"/>.</param>
/// <remarks>Threading: immutable; pure computation.</remarks>
internal readonly record struct FrameStatistics(int Count, double AverageFps, double P50, double P95, double Max, int OverBudget)
{
    /// <summary>Two frames at 60 Hz: an interval above it is a visible stutter.</summary>
    public const double BudgetMilliseconds = 33.3;

    /// <summary>Computes the statistics of <paramref name="intervalsMilliseconds"/>.</summary>
    public static FrameStatistics Compute(IReadOnlyList<double> intervalsMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(intervalsMilliseconds);

        if (intervalsMilliseconds.Count == 0)
        {
            return default;
        }

        double[] sorted = [.. intervalsMilliseconds];
        Array.Sort(sorted);
        double total = sorted.Sum();
        return new FrameStatistics(
            sorted.Length,
            total > 0 ? sorted.Length * 1000.0 / total : 0,
            Percentile(sorted, 0.50),
            Percentile(sorted, 0.95),
            sorted[^1],
            sorted.Count(interval => interval > BudgetMilliseconds));
    }

    /// <summary>Formats the statistics for the status line.</summary>
    public string Format() => string.Create(
        CultureInfo.InvariantCulture,
        $"Frames: {Count} · avg {AverageFps:0.0} fps · p95 {P95:0.0} ms · max {Max:0.0} ms · >33 ms: {OverBudget}");

    // Nearest rank.
    private static double Percentile(double[] sorted, double fraction) =>
        sorted[Math.Max(0, (int)Math.Ceiling(fraction * sorted.Length) - 1)];
}
