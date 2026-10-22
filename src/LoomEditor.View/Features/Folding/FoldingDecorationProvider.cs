// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Folding;

/// <summary>
/// The decoration options of the folding regions, according to their state, <see cref="ShowFoldingControls"/> and
/// <see cref="ShowFoldingHighlights"/> (Monaco: <c>FoldingDecorationProvider</c>).
/// </summary>
/// <remarks>
/// Not ported: <c>linesDecorationsTooltip</c> (no hover) and <c>minimap</c> (no minimap decorations yet).
/// Threading: UI thread only.
/// </remarks>
internal sealed class FoldingDecorationProvider(ViewModel viewModel) : IDecorationProvider
{
    private const string CollapsedIcon = "codicon codicon-folding-collapsed";
    private const string ManualCollapsedIcon = "codicon codicon-folding-manual-collapsed";
    private const string ExpandedIcon = "codicon codicon-folding-expanded";
    private const string ManualExpandedIcon = "codicon codicon-folding-manual-expanded";

    private static readonly ModelDecorationOptions CollapsedVisualDecoration = Collapsed(
        "folding-collapsed-visual-decoration", highlighted: false, CollapsedIcon);

    private static readonly ModelDecorationOptions CollapsedHighlightedVisualDecoration = Collapsed(
        "folding-collapsed-highlighted-visual-decoration", highlighted: true, CollapsedIcon);

    private static readonly ModelDecorationOptions ManuallyCollapsedVisualDecoration = Collapsed(
        "folding-manually-collapsed-visual-decoration", highlighted: false, ManualCollapsedIcon);

    private static readonly ModelDecorationOptions ManuallyCollapsedHighlightedVisualDecoration = Collapsed(
        "folding-manually-collapsed-highlighted-visual-decoration", highlighted: true, ManualCollapsedIcon);

    private static readonly ModelDecorationOptions NoControlsCollapsedRangeDecoration = Collapsed(
        "folding-no-controls-range-decoration", highlighted: false, null);

    private static readonly ModelDecorationOptions NoControlsCollapsedHighlightedRangeDecoration = Collapsed(
        "folding-no-controls-range-decoration", highlighted: true, null);

    private static readonly ModelDecorationOptions ExpandedVisualDecoration = Expanded(
        "folding-expanded-visual-decoration", TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, "alwaysShowFoldIcons " + ExpandedIcon);

    private static readonly ModelDecorationOptions ExpandedAutoHideVisualDecoration = Expanded(
        "folding-expanded-auto-hide-visual-decoration", TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, ExpandedIcon);

    private static readonly ModelDecorationOptions ManuallyExpandedVisualDecoration = Expanded(
        "folding-manually-expanded-visual-decoration", TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges, "alwaysShowFoldIcons " + ManualExpandedIcon);

    private static readonly ModelDecorationOptions ManuallyExpandedAutoHideVisualDecoration = Expanded(
        "folding-manually-expanded-auto-hide-visual-decoration", TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges, ManualExpandedIcon);

    private static readonly ModelDecorationOptions NoControlsExpandedRangeDecoration = Expanded(
        "folding-no-controls-range-decoration", TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges, null);

    private static readonly ModelDecorationOptions HiddenRangeDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "folding-hidden-range-decoration",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
    });

    public ShowFoldingControls ShowFoldingControls { get; set; } = ShowFoldingControls.Mouseover;

    public bool ShowFoldingHighlights { get; set; } = true;

    public ModelDecorationOptions GetDecorationOption(bool isCollapsed, bool isHidden, bool isManual)
    {
        if (isHidden)
        {
            // is inside another collapsed region
            return HiddenRangeDecoration;
        }

        if (ShowFoldingControls == ShowFoldingControls.Never)
        {
            if (isCollapsed)
            {
                return ShowFoldingHighlights ? NoControlsCollapsedHighlightedRangeDecoration : NoControlsCollapsedRangeDecoration;
            }

            return NoControlsExpandedRangeDecoration;
        }

        if (isCollapsed)
        {
            return isManual
                ? (ShowFoldingHighlights ? ManuallyCollapsedHighlightedVisualDecoration : ManuallyCollapsedVisualDecoration)
                : (ShowFoldingHighlights ? CollapsedHighlightedVisualDecoration : CollapsedVisualDecoration);
        }

        if (ShowFoldingControls == ShowFoldingControls.Mouseover)
        {
            return isManual ? ManuallyExpandedAutoHideVisualDecoration : ExpandedAutoHideVisualDecoration;
        }

        return isManual ? ManuallyExpandedVisualDecoration : ExpandedVisualDecoration;
    }

    public T ChangeDecorations<T>(Func<IModelDecorationsChangeAccessor, T> callback) =>
        viewModel.Model.ChangeDecorations(viewModel.EditorId, callback);

    public void RemoveDecorations(IReadOnlyList<string> decorationIds) =>
        viewModel.Model.ChangeDecorations(viewModel.EditorId, accessor => accessor.DeltaDecorations(decorationIds, []));

    private static ModelDecorationOptions Collapsed(string description, bool highlighted, string? icon) =>
        ModelDecorationOptions.Register(new ModelDecorationOptionsData
        {
            Description = description,
            Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges,
            AfterContentClassName = "inline-folded",
            ClassName = highlighted ? "folded-background" : null,
            IsWholeLine = true,
            FirstLineDecorationClassName = icon,
        });

    private static ModelDecorationOptions Expanded(string description, TrackedRangeStickiness stickiness, string? icon) =>
        ModelDecorationOptions.Register(new ModelDecorationOptionsData
        {
            Description = description,
            Stickiness = stickiness,
            IsWholeLine = true,
            FirstLineDecorationClassName = icon,
        });
}
