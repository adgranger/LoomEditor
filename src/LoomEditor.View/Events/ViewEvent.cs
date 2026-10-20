// Ported from microsoft/vscode: src/vs/editor/common/viewEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Events;

/// <summary>An event sent to the view (Monaco: the <c>ViewEvent</c> union; one class per event here).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal abstract class ViewEvent
{
}

/// <summary>The cursor moved or changed its selections (Monaco: <c>ViewCursorStateChangedEvent</c>).</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class ViewCursorStateChangedEvent(
    IReadOnlyList<Selection> selections,
    IReadOnlyList<Selection> modelSelections,
    CursorChangeReason reason) : ViewEvent
{
    /// <summary>The selections in view coordinates.</summary>
    public IReadOnlyList<Selection> Selections { get; } = selections;

    /// <summary>The selections in model coordinates.</summary>
    public IReadOnlyList<Selection> ModelSelections { get; } = modelSelections;

    public CursorChangeReason Reason { get; } = reason;
}

/// <summary>How to scroll vertically to reveal a range (Monaco: <c>VerticalRevealType</c>).</summary>
internal enum VerticalRevealType
{
    Simple = 0,
    Center = 1,
    CenterIfOutsideViewport = 2,
    Top = 3,
    Bottom = 4,
    NearTop = 5,
    NearTopIfOutsideViewport = 6,
}

/// <summary>Asks the view to scroll so that a range or some selections are visible (Monaco: <c>ViewRevealRangeRequestEvent</c>).</summary>
/// <param name="Source">The source of the call that caused the event.</param>
/// <param name="MinimalReveal">Reduce the revealing to a minimum (e.g. avoid scrolling if the bounding box is visible and near the viewport edge).</param>
/// <param name="Range">The range to be revealed.</param>
/// <param name="Selections">The selections to be revealed.</param>
/// <param name="VerticalType">The vertical reveal strategy.</param>
/// <param name="RevealHorizontal">True for a horizontal and vertical revealing; false for just a vertical one.</param>
/// <param name="ScrollType">The scroll type.</param>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class ViewRevealRangeRequestEvent(
    string? Source,
    bool MinimalReveal,
    Range? Range,
    IReadOnlyList<Selection>? Selections,
    VerticalRevealType VerticalType,
    bool RevealHorizontal,
    ScrollType ScrollType) : ViewEvent
{
    public string? Source { get; } = Source;

    public bool MinimalReveal { get; } = MinimalReveal;

    public Range? Range { get; } = Range;

    public IReadOnlyList<Selection>? Selections { get; } = Selections;

    public VerticalRevealType VerticalType { get; } = VerticalType;

    public bool RevealHorizontal { get; } = RevealHorizontal;

    public ScrollType ScrollType { get; } = ScrollType;
}

/// <summary>A range of view lines (Monaco: the <c>{ fromLineNumber, toLineNumber }</c> of <c>ViewTokensChangedEvent</c>), both inclusive.</summary>
internal readonly record struct ViewLineRange(int FromLineNumber, int ToLineNumber);

/// <summary>The decorations changed (Monaco: <c>ViewDecorationsChangedEvent</c>); without a source, everything is affected.</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class ViewDecorationsChangedEvent(ModelDecorationsChangedEventArgs? source) : ViewEvent
{
    public bool AffectsMinimap { get; } = source?.AffectsMinimap ?? true;

    public bool AffectsOverviewRuler { get; } = source?.AffectsOverviewRuler ?? true;

    public bool AffectsGlyphMargin { get; } = source?.AffectsGlyphMargin ?? true;

    public bool AffectsLineNumber { get; } = source?.AffectsLineNumber ?? true;
}

/// <summary>The tokens of some lines changed (Monaco: <c>ViewTokensChangedEvent</c>).</summary>
/// <remarks>Threading: immutable; UI thread only.</remarks>
internal sealed class ViewTokensChangedEvent(IReadOnlyList<ViewLineRange> ranges) : ViewEvent
{
    /// <summary>The lines whose tokens changed.</summary>
    public IReadOnlyList<ViewLineRange> Ranges { get; } = ranges;
}
