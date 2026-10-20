// Inspired by microsoft/vscode: src/vs/editor/common/viewLayout/viewLineRenderer.ts (transformAndRemoveOverflowing)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;
using LoomEditor.Themes;

namespace LoomEditor.Rendering;

/// <summary>A stretch of a line drawn with one color and font style.</summary>
/// <param name="Start">The UTF-16 offset of the first character.</param>
/// <param name="Length">The number of characters, at least 1.</param>
/// <param name="ForegroundArgb">The text color, <c>0xAARRGGBB</c>.</param>
/// <param name="FontStyle">Italic, bold, underline and strikethrough flags.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct StyleRun(int Start, int Length, uint ForegroundArgb, FontStyle FontStyle);

/// <summary>A foreground color given to a stretch of a line by an inline decoration.</summary>
/// <param name="Start">The UTF-16 offset of the first character.</param>
/// <param name="End">The offset after the last character (exclusive).</param>
/// <param name="ForegroundArgb">The text color, <c>0xAARRGGBB</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct StyleOverride(int Start, int End, uint ForegroundArgb);

/// <summary>
/// The colors and font styles of one line: the runs that its tokens give, with neighbours of the same style merged
/// (fewer DirectWrite ranges). A line drawn with the default foreground and no font style has no runs
/// (<see cref="Plain"/>), so plain text keeps one layout whatever the theme.
/// </summary>
/// <remarks>Threading: immutable; safe to share.</remarks>
internal sealed class LineStyle : IEquatable<LineStyle>
{
    /// <summary>The style of a line without colors or font styles.</summary>
    public static readonly LineStyle Plain = new([]);

    private readonly int _hash;

    private LineStyle(StyleRun[] runs)
    {
        Runs = runs;
        var hash = new HashCode();
        foreach (StyleRun run in runs)
        {
            hash.Add(run);
        }

        _hash = hash.ToHashCode();
    }

    /// <summary>The runs, in order; they never overlap and each one is at least one character long.</summary>
    public IReadOnlyList<StyleRun> Runs { get; }

    /// <summary><c>0xAARRGGBB</c> of a theme color.</summary>
    public static uint ToArgb(ThemeColor color) => ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

    /// <summary>
    /// Builds the style of a line from its tokens. Runs past <paramref name="length"/> are dropped and the last one is
    /// cut (<c>transformAndRemoveOverflowing</c>): a very long line is only laid out up to its rendered length.
    /// </summary>
    /// <param name="tokens">The tokens of the line.</param>
    /// <param name="length">The number of characters that are laid out.</param>
    /// <param name="colorMap">The ARGB colors by color id, or <see langword="null"/> when no theme is set.</param>
    /// <param name="defaultForeground">The ARGB of the default foreground (color id 1, or unknown ids).</param>
    /// <param name="overrides">
    /// Foreground colors of inline decorations, applied in list order: a later one wins on the characters it covers
    /// (behavior difference: Monaco splits the line with <c>LineDecorationsNormalizer</c> and lets the CSS cascade
    /// pick the color of overlapping classes). The font style of the tokens is kept.
    /// </param>
    public static LineStyle Create(
        IViewLineTokens tokens, int length, IReadOnlyList<uint>? colorMap, uint defaultForeground,
        IReadOnlyList<StyleOverride>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var runs = new List<StyleRun>();
        int start = 0;
        for (int i = 0, count = tokens.GetCount(); i < count && start < length; i++)
        {
            int end = Math.Min(tokens.GetEndOffset(i), length);
            if (end <= start)
            {
                continue;
            }

            TokenPresentation presentation = tokens.GetPresentation(i);
            uint foreground = presentation.Foreground > ColorId.DefaultForeground && colorMap is not null && presentation.Foreground < colorMap.Count
                ? colorMap[presentation.Foreground]
                : defaultForeground;
            FontStyle fontStyle = (presentation.Italic ? FontStyle.Italic : 0)
                | (presentation.Bold ? FontStyle.Bold : 0)
                | (presentation.Underline ? FontStyle.Underline : 0)
                | (presentation.Strikethrough ? FontStyle.Strikethrough : 0);
            runs.Add(new StyleRun(start, end - start, foreground, fontStyle));
            start = end;
        }

        foreach (StyleOverride styleOverride in overrides ?? [])
        {
            runs = ApplyOverride(runs, Math.Max(0, styleOverride.Start), Math.Min(styleOverride.End, length), styleOverride.ForegroundArgb);
        }

        var merged = new List<StyleRun>(runs.Count);
        foreach (StyleRun run in runs)
        {
            if (merged.Count > 0 && merged[^1] is { } last && last.ForegroundArgb == run.ForegroundArgb && last.FontStyle == run.FontStyle)
            {
                merged[^1] = last with { Length = last.Length + run.Length };
            }
            else
            {
                merged.Add(run);
            }
        }

        return merged.Count == 0 || (merged.Count == 1 && merged[0].ForegroundArgb == defaultForeground && merged[0].FontStyle == FontStyle.None)
            ? Plain
            : new LineStyle([.. merged]);
    }

    // Splits the runs at the bounds of [start, end) and gives the part inside the foreground of the override.
    private static List<StyleRun> ApplyOverride(List<StyleRun> runs, int start, int end, uint foreground)
    {
        if (end <= start)
        {
            return runs;
        }

        var result = new List<StyleRun>(runs.Count + 2);
        foreach (StyleRun run in runs)
        {
            int runEnd = run.Start + run.Length;
            int from = Math.Max(run.Start, start);
            int to = Math.Min(runEnd, end);
            if (from >= to)
            {
                result.Add(run);
                continue;
            }

            if (from > run.Start)
            {
                result.Add(run with { Length = from - run.Start });
            }

            result.Add(new StyleRun(from, to - from, foreground, run.FontStyle));
            if (runEnd > to)
            {
                result.Add(run with { Start = to, Length = runEnd - to });
            }
        }

        return result;
    }

    /// <inheritdoc />
    public bool Equals(LineStyle? other) => other is not null && (ReferenceEquals(this, other) || (_hash == other._hash && Runs.SequenceEqual(other.Runs)));

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as LineStyle);

    /// <inheritdoc />
    public override int GetHashCode() => _hash;
}
