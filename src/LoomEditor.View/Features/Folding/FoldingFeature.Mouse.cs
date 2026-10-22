// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/folding.ts (onEditorMouseDown, onEditorMouseUp)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Controller;

namespace LoomEditor.View.Features.Folding;

// Not ported: the mouse-down also carries `e.target.element` in Monaco; here `gutterOffsetX` is measured from `DecorationsLeft`.
internal sealed partial class FoldingFeature
{
    // Monaco: the whitespace between the border and the real folding icon border is 4px.
    private const int IconLeftMargin = 4;

    private readonly record struct MouseDownInfo(int LineNumber, bool IconClicked);

    private ViewUserInputEvents? _userInputEvents;
    private MouseDownInfo? _mouseDownInfo;

    private void AttachMouse(IEditorContext context)
    {
        _userInputEvents = context.GetUserInputEvents();
        if (_userInputEvents is { } events)
        {
            events.MouseDown += OnEditorMouseDown;
            events.MouseUp += OnEditorMouseUp;
        }
    }

    private void DetachMouse()
    {
        if (_userInputEvents is { } events)
        {
            events.MouseDown -= OnEditorMouseDown;
            events.MouseUp -= OnEditorMouseUp;
        }

        _userInputEvents = null;
        _mouseDownInfo = null;
    }

    private void OnEditorMouseDown(object? sender, EditorMouseEventArgs e)
    {
        _mouseDownInfo = null;

        if (_hiddenRangeModel is not { } hiddenRangeModel || e.Target.Range is not { } range)
        {
            return;
        }

        if (!e.Event.LeftButton && !e.Event.MiddleButton)
        {
            return;
        }

        bool iconClicked = false;
        switch (e.Target.Type)
        {
            case MouseTargetType.GutterLineDecorations:
                double gutterOffsetX = e.Event.X - _viewModel!.LayoutInfo.DecorationsLeft;

                // this is such that we don't collide with dirty diff
                if (gutterOffsetX < IconLeftMargin)
                {
                    return;
                }

                iconClicked = true;
                break;
            case MouseTargetType.ContentEmpty:
                if (_options.UnfoldOnClickAfterEndOfLine && hiddenRangeModel.HasRanges() && e.Target.IsAfterLines)
                {
                    break;
                }

                return;
            case MouseTargetType.ContentText:
                if (hiddenRangeModel.HasRanges() && range.StartColumn == _model!.GetLineMaxColumn(range.StartLineNumber))
                {
                    break;
                }

                return;
            default:
                return;
        }

        _mouseDownInfo = new MouseDownInfo(range.StartLineNumber, iconClicked);
    }

    private void OnEditorMouseUp(object? sender, EditorMouseEventArgs e)
    {
        if (GetFoldingModel() is not { } foldingModel || _mouseDownInfo is not { } info)
        {
            return;
        }

        int lineNumber = info.LineNumber;
        if (e.Target.Range is not { } range || range.StartLineNumber != lineNumber)
        {
            return;
        }

        if (info.IconClicked)
        {
            if (e.Target.Type != MouseTargetType.GutterLineDecorations)
            {
                return;
            }
        }
        else if (range.StartColumn != _model!.GetLineMaxColumn(lineNumber))
        {
            return;
        }

        FoldingRegion? region = foldingModel.GetRegionAtLine(lineNumber);
        if (region is null || region.StartLineNumber != lineNumber)
        {
            return;
        }

        bool isCollapsed = region.IsCollapsed;
        if (!info.IconClicked && !isCollapsed)
        {
            return;
        }

        var toToggle = new List<FoldingRegion>();
        if (e.Event.AltKey)
        {
            // the surrounding regions: those that neither contain the region nor are contained by it
            List<FoldingRegion> toMaybeToggle = foldingModel.GetRegionsInside(
                null, other => !other.ContainedBy(region) && !region.ContainedBy(other));
            toToggle.AddRange(toMaybeToggle.Where(r => r.IsCollapsed));

            // if any surrounding regions are folded, unfold those. Otherwise, fold all surrounding
            if (toToggle.Count == 0)
            {
                toToggle = toMaybeToggle;
            }
        }
        else
        {
            bool recursive = e.Event.MiddleButton || e.Event.ShiftKey;
            if (recursive)
            {
                toToggle.AddRange(foldingModel.GetRegionsInside(region).Where(r => r.IsCollapsed == isCollapsed));
            }

            // when recursive, first only collapse all children. If all are already folded or there are no children, also fold parent.
            if (isCollapsed || !recursive || toToggle.Count == 0)
            {
                toToggle.Add(region);
            }
        }

        foldingModel.ToggleCollapseState(toToggle);
        Reveal(new Position(lineNumber, 1));
    }
}
