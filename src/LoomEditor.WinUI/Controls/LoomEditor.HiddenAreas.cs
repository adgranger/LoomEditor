using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    private readonly object _hostHiddenAreasSource = new();

    /// <summary>
    /// Hides whole lines: each range hides its lines <c>StartLineNumber</c> … <c>EndLineNumber</c> (Monaco: <c>setHiddenAreas</c>). Replaces the ranges of the
    /// previous call; an empty list shows them again. Folding keeps its own hidden ranges. On a side of a <see cref="LoomDiffEditor"/>, "hide unchanged regions"
    /// also hides lines: combining the two is not supported.
    /// </summary>
    /// <param name="ranges">The ranges to hide; each one is clamped to the text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ranges"/> is <see langword="null"/>.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public void SetHiddenAreas(IReadOnlyList<Range> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        var validated = new Range[ranges.Count];
        for (int i = 0; i < validated.Length; i++)
        {
            validated[i] = _context.Model.ValidateRange(ranges[i]);
        }

        _viewModel.SetHiddenAreas(validated, _hostHiddenAreasSource);
    }
}
