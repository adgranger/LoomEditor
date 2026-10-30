using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Themes;

namespace LoomEditor.View.Decorations;

/// <summary>How a host decoration is drawn: class names for its text area, gutter and strips, and an overview ruler mark.</summary>
/// <remarks>
/// A class name is a built-in class or one registered by the host with <see cref="CustomDecorationStyles"/>; unknown names draw nothing.
/// Reuse one instance for many decorations. Threading: immutable; thread-safe.
/// </remarks>
public sealed class EditorDecorationOptions
{
    private ModelDecorationOptions? _modelOptions;

    /// <summary>The class of the decorated range: its background and border (Monaco: <c>className</c>).</summary>
    public string? ClassName { get; init; }

    /// <summary>The decoration covers whole lines, from the left of the text to the right edge (Monaco: <c>isWholeLine</c>).</summary>
    public bool IsWholeLine { get; init; }

    /// <summary>The class that colors the decorated text (Monaco: <c>inlineClassName</c>).</summary>
    public string? InlineClassName { get; init; }

    /// <summary>The class drawn across the whole gutter of the decorated lines (Monaco: <c>marginClassName</c>).</summary>
    public string? MarginClassName { get; init; }

    /// <summary>The class drawn in the narrow strip between the line numbers and the text (Monaco: <c>linesDecorationsClassName</c>).</summary>
    public string? LinesDecorationsClassName { get; init; }

    /// <summary>The mark in the overview ruler in light themes; no mark when it is <see langword="null"/>.</summary>
    public ThemeColor? OverviewRulerColor { get; init; }

    /// <summary>The mark in the overview ruler in dark themes; the light color is used when it is <see langword="null"/>.</summary>
    public ThemeColor? OverviewRulerDarkColor { get; init; }

    internal ModelDecorationOptions ToModelOptions()
    {
        // A race creates two equal objects: harmless.
        return _modelOptions ??= ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            Description = "host-decoration",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = ClassName,
            IsWholeLine = IsWholeLine,
            InlineClassName = InlineClassName,
            MarginClassName = MarginClassName,
            LinesDecorationsClassName = LinesDecorationsClassName,
            OverviewRuler = OverviewRulerColor is { } light
                ? new ModelDecorationOverviewRulerOptions(
                    DecorationColor.FromValue(light.ToString()),
                    OverviewRulerDarkColor is { } dark ? DecorationColor.FromValue(dark.ToString()) : null,
                    OverviewRulerLane.Center)
                : null,
        });
    }
}
