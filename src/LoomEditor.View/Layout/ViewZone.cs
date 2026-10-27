// Ported from microsoft/vscode: src/vs/editor/browser/editorBrowser.ts (IViewZone)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>What the renderer paints in a <see cref="ViewZone"/>.</summary>
internal enum ViewZoneKind
{
    /// <summary>Nothing: a plain gap.</summary>
    Empty,

    /// <summary>Deleted lines of a diff (drawn by the renderer from <see cref="ViewZone.Data"/>).</summary>
    DeletedLines,

    /// <summary>The collapsed bar of hidden unchanged regions.</summary>
    Collapsed,
}

/// <summary>
/// A vertical gap anchored after a model line (Monaco: <c>IViewZone</c>). Mutable: the owner changes the fields and
/// calls <see cref="IViewZoneChangeAccessor.LayoutZone"/>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class ViewZone
{
    /// <summary>Assigned by <see cref="IViewZoneChangeAccessor.AddZone"/>; empty before.</summary>
    public string Id { get; internal set; } = "";

    /// <summary>The model line this zone comes after (0 = before the first line).</summary>
    public int AfterLineNumber { get; set; }

    /// <summary>The model column the zone comes after; the end of the line when <see langword="null"/>.</summary>
    public int? AfterColumn { get; set; }

    /// <summary>The height in pixels; wins over <see cref="HeightInLines"/>.</summary>
    public int? HeightInPx { get; set; }

    /// <summary>The height in lines; one line when neither height is set.</summary>
    public double? HeightInLines { get; set; }

    /// <summary>The minimum content width in pixels.</summary>
    public int? MinWidthInPx { get; set; }

    /// <summary>Orders the zones that come after the same line; defaults to <see cref="AfterColumn"/>, then 10000.</summary>
    public int? Ordinal { get; set; }

    /// <summary>Keep the height when the zone's position is in a hidden area.</summary>
    public bool ShowInHiddenAreas { get; set; }

    /// <summary>Whether a mouse down on the zone must not move the cursor.</summary>
    public bool SuppressMouseDown { get; set; }

    /// <summary>What the renderer paints.</summary>
    public ViewZoneKind Kind { get; set; }

    /// <summary>What the renderer needs for the <see cref="Kind"/>.</summary>
    public object? Data { get; set; }

    /// <summary>Called with the computed height in pixels when it is (re)computed.</summary>
    public Action<int>? OnComputedHeight { get; set; }
}
