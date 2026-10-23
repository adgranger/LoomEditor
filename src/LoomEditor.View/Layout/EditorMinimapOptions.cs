// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>The side of the editor where the minimap is drawn. Monaco: <c>'right' | 'left'</c>.</summary>
internal enum MinimapSide
{
    /// <summary>Monaco: <c>'right'</c>.</summary>
    Right = 0,

    /// <summary>Monaco: <c>'left'</c>.</summary>
    Left = 1,
}

/// <summary>How the minimap is sized. Monaco: <c>'proportional' | 'fill' | 'fit'</c>.</summary>
internal enum MinimapSize
{
    /// <summary>Same size as the editor contents (and might scroll).</summary>
    Proportional = 0,

    /// <summary>Stretches or shrinks to fill the height of the editor (no scrolling).</summary>
    Fill = 1,

    /// <summary>Shrinks to never be larger than the editor (no scrolling).</summary>
    Fit = 2,
}

/// <summary>When the minimap slider is shown. Monaco: <c>'always' | 'mouseover'</c>.</summary>
internal enum MinimapShowSlider
{
    /// <summary>Monaco: <c>'mouseover'</c>.</summary>
    MouseOver = 0,

    /// <summary>Monaco: <c>'always'</c>.</summary>
    Always = 1,
}

/// <summary>When the minimap is hidden automatically. Monaco: <c>'none' | 'mouseover' | 'scroll'</c>.</summary>
internal enum MinimapAutohide
{
    /// <summary>The minimap is always shown.</summary>
    None = 0,

    /// <summary>Shown only while the mouse is over the minimap.</summary>
    MouseOver = 1,

    /// <summary>Shown only while the editor is scrolled.</summary>
    Scroll = 2,
}

/// <summary>The minimap options (Monaco: <c>editor.minimap.*</c>, <c>EditorMinimap</c>).</summary>
/// <remarks>
/// Threading: immutable, safe from any thread. Not ported: the section header options
/// (<c>showRegionSectionHeaders</c>, <c>showMarkSectionHeaders</c>, <c>markSectionHeaderRegex</c>,
/// <c>sectionHeaderFontSize</c>, <c>sectionHeaderLetterSpacing</c>), which belong to the <c>sectionHeaders</c> contribution.
/// The enums cannot hold other values, so <see cref="Validated"/> only clamps the numbers.
/// </remarks>
internal sealed record EditorMinimapOptions
{
    /// <summary>Render the minimap. Monaco default: <c>true</c>.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Hide the minimap automatically. Monaco default: <c>'none'</c>.</summary>
    public MinimapAutohide Autohide { get; init; } = MinimapAutohide.None;

    /// <summary>The side of the minimap. Monaco default: <c>'right'</c>.</summary>
    public MinimapSide Side { get; init; } = MinimapSide.Right;

    /// <summary>The minimap sizing mode. Monaco default: <c>'proportional'</c>.</summary>
    public MinimapSize Size { get; init; } = MinimapSize.Proportional;

    /// <summary>When the slider is shown. Monaco default: <c>'mouseover'</c>.</summary>
    public MinimapShowSlider ShowSlider { get; init; } = MinimapShowSlider.MouseOver;

    /// <summary>Render the actual characters instead of color blocks. Monaco default: <c>true</c>.</summary>
    public bool RenderCharacters { get; init; } = true;

    /// <summary>Render at most this many columns. Monaco default: <c>120</c>.</summary>
    public int MaxColumn { get; init; } = 120;

    /// <summary>Relative size of the font in the minimap (1, 2 or 3). Monaco default: <c>1</c>.</summary>
    public int Scale { get; init; } = 1;

    /// <summary>Monaco's <c>validate</c>: <see cref="Scale"/> clamped to 1–3 and <see cref="MaxColumn"/> to 1–10000.</summary>
    public EditorMinimapOptions Validated() => this with
    {
        Scale = Math.Clamp(Scale, 1, 3),
        MaxColumn = Math.Clamp(MaxColumn, 1, 10000),
    };
}
