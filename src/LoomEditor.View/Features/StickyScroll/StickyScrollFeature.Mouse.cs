// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollController.ts (click listener, _toggleFoldingRegionForLine)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>The part of a sticky line under the pointer (Monaco: the DOM node tests of <c>StickyScrollWidget</c>).</summary>
internal enum StickyScrollHitPart
{
    /// <summary>The line number area.</summary>
    LineNumberGutter,

    /// <summary>The folding chevron.</summary>
    FoldingIcon,

    /// <summary>The text of the line.</summary>
    Content,
}

// Behavior differences: the click-link gesture (ctrl-click, hover underline) is not ported (phase 8) and a click with a
// modifier other than shift does nothing, as in Monaco; the focus is given back to the editor by the control.
internal sealed partial class StickyScrollFeature
{
    /// <summary>
    /// A left click on a sticky line (Monaco: the <c>CLICK</c> listener of the widget).
    /// <paramref name="column"/> is the model column hit in the drawn line, or <see langword="null"/> for column 1.
    /// </summary>
    internal void HandleClick(int lineIndex, StickyScrollHitPart part, int? column, bool shift, bool otherModifier = false)
    {
        if (otherModifier || _viewModel is null || lineIndex < 0 || lineIndex >= _widgetState.StartLineNumbers.Count)
        {
            return;
        }

        if (shift)
        {
            RevealInCenterIfOutsideViewport(new Position(_widgetState.EndLineNumbers[lineIndex], 1));
            return;
        }

        int lineNumber = _widgetState.StartLineNumbers[lineIndex];
        switch (part)
        {
            case StickyScrollHitPart.FoldingIcon:
                ToggleFoldingRegionForLine(lineNumber, lineIndex);
                break;
            case StickyScrollHitPart.Content:
                RevealPosition(new Position(lineNumber, column ?? 1));
                break;
            default:
                RevealPosition(new Position(lineNumber, 1));
                break;
        }
    }

    // Monaco: `_revealPosition`, `_revealLineInCenterIfOutsideViewport` and `_reveaInEditor`.
    private void RevealPosition(Position position)
    {
        Reveal(position, VerticalRevealType.Simple);
        _viewModel!.SetSelections("api", [new Selection(position, position)]);
    }

    private void RevealInCenterIfOutsideViewport(Position position)
    {
        Reveal(position, VerticalRevealType.CenterIfOutsideViewport);
        _viewModel!.SetSelections("api", [new Selection(position, position)]);
    }

    private void Reveal(Position position, VerticalRevealType type) =>
        _viewModel!.RevealRange(
            "api",
            true,
            _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(Range.Collapsed(position)),
            type,
            ScrollType.Smooth);

    // Monaco: `_toggleFoldingRegionForLine`. The scroll top keeps the toggled line at the same place under the sticky lines.
    private void ToggleFoldingRegionForLine(int lineNumber, int lineIndex)
    {
        if (FoldingModel is not { } foldingModel
            || foldingModel.GetRegionAtLine(lineNumber) is not { } region
            || region.StartLineNumber != lineNumber)
        {
            return;
        }

        FoldingModelFunctions.ToggleCollapseState(foldingModel, 1, [lineNumber]);
        int top = region.IsCollapsed ? GetTopForLineNumber(region.EndLineNumber) : GetTopForLineNumber(region.StartLineNumber);
        double scrollTop = top - _viewModel!.FontInfo.LineHeight * lineIndex + 1;
        _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, scrollTop), ScrollType.Immediate);
        RenderStickyScroll();
    }
}
