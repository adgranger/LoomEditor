using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.WinUI.Controls;

// Scrolls so that the cursor stays visible (Monaco: the reveal part of `ViewLines`; the computations are in
// `RevealCalculator`). The vertical scroll is applied at once, the horizontal one after the next draw, once the lines
// it needs are laid out.
public sealed partial class LoomEditor
{
    private RevealCalculator? _reveal;

    private void InitializeReveal()
    {
        _reveal = new RevealCalculator(_viewModel);
        _viewModel.EventDispatcher.AddViewEventHandler(new RevealEventHandler(this));
    }

    private void OnRevealRangeRequest(ViewRevealRangeRequestEvent e)
    {
        if (_reveal!.OnRevealRangeRequest(e) && _reveal.PendingHorizontalRequest is not null)
        {
            // The vertical scroll may not move anything: still draw, to compute the horizontal one.
            _canvas?.Invalidate();
        }
    }

    // Monaco: `revealRangeInCenter`. Used by the diff editor.
    internal void RevealRangeInCenter(Core.Text.Range modelRange) =>
        _viewModel.RevealRange(
            "api", true, _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(modelRange), VerticalRevealType.Center, ScrollType.Smooth);

    // Monaco: `revealRange` (the vertical position is kept when the range is already visible). Used by the diff editor.
    internal void RevealRange(Core.Text.Range modelRange) =>
        _viewModel.RevealRange(
            "api", true, _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(modelRange), VerticalRevealType.Simple, ScrollType.Smooth);

    // Monaco: `renderText`, step 2. Called after each draw.
    private void ApplyPendingHorizontalReveal()
    {
        if (_renderer is not { } renderer || _reveal is not { PendingHorizontalRequest: not null } reveal)
        {
            return;
        }

        reveal.TryApplyPendingHorizontalReveal(
            renderer.RenderedStartLineNumber,
            renderer.RenderedEndLineNumber,
            (lineNumber, startColumn, endColumn) =>
                renderer.TryGetHorizontalRange(_viewModel, lineNumber, startColumn, endColumn, out double left, out double width)
                    ? (left, width)
                    : null,
            maxHorizontalOffset => renderer.EnsureMaxLineWidth(_viewModel, maxHorizontalOffset));
    }

    private sealed class RevealEventHandler(LoomEditor owner) : IViewEventHandler
    {
        public void HandleEvents(IReadOnlyList<ViewEvent> events)
        {
            foreach (ViewEvent viewEvent in events)
            {
                if (viewEvent is ViewRevealRangeRequestEvent reveal)
                {
                    owner.OnRevealRangeRequest(reveal);
                }
            }
        }
    }
}
