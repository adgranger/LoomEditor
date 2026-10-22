// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Decorations;

/// <summary>
/// The raw options of a decoration (Monaco: the <c>IModelDecorationOptions</c> object literal); pass it to
/// <see cref="ModelDecorationOptions.Register"/> or <see cref="ModelDecorationOptions.CreateDynamic"/>.
/// </summary>
/// <remarks>
/// Not ported: <c>blockClassName</c>, <c>blockIsAfterEnd</c>, <c>blockDoesNotCollapse</c>, <c>blockPadding</c>;
/// <c>hoverMessage</c>, <c>glyphMarginHoverMessage</c>, <c>lineNumberHoverMessage</c>; <c>lineHeight</c>,
/// <c>fontFamily</c>, <c>fontSize</c>, <c>fontWeight</c>, <c>fontStyle</c>;
/// <c>glyphMargin</c> lanes; <c>lineNumberClassName</c>; <c>linesDecorationsTooltip</c>;
/// <c>textDirection</c>. Threading: immutable; thread-safe.
/// </remarks>
internal sealed record ModelDecorationOptionsData
{
    public required string Description { get; init; }

    public TrackedRangeStickiness? Stickiness { get; init; }

    public string? ClassName { get; init; }

    public bool? ShouldFillLineOnLineBreak { get; init; }

    public bool? IsWholeLine { get; init; }

    public bool? ShowIfCollapsed { get; init; }

    public bool? CollapseOnReplaceEdit { get; init; }

    public int? ZIndex { get; init; }

    public string? GlyphMarginClassName { get; init; }

    public string? LinesDecorationsClassName { get; init; }

    public string? FirstLineDecorationClassName { get; init; }

    public string? MarginClassName { get; init; }

    public string? InlineClassName { get; init; }

    public bool? InlineClassNameAffectsLetterSpacing { get; init; }

    public string? BeforeContentClassName { get; init; }

    public string? AfterContentClassName { get; init; }

    public bool? HideInCommentTokens { get; init; }

    public bool? HideInStringTokens { get; init; }

    public InjectedTextOptions? Before { get; init; }

    public InjectedTextOptions? After { get; init; }

    /// <summary>If set, the decoration is marked in the overview ruler.</summary>
    public ModelDecorationOverviewRulerOptions? OverviewRuler { get; init; }

    /// <summary>If set, the decoration is marked in the minimap.</summary>
    public ModelDecorationMinimapOptions? Minimap { get; init; }
}
