using LoomEditor.View.Layout;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

// The scroll hooks of the host API (phase 13) and of the diff editor.
public sealed partial class LoomEditor
{
    /// <summary>The top of a view line in scroll coordinates (Monaco: <c>viewLayout.getVerticalOffsetForLineNumber</c>).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal int GetVerticalOffsetForViewLine(int viewLineNumber) => _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(viewLineNumber);

    /// <summary>The height of the scrollable content, in DIPs.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal double GetScrollHeight() => _viewModel.ViewLayout.GetScrollable().GetScrollDimensions().ScrollHeight;

    /// <summary>
    /// Scrolls without animation (Monaco: <c>ScrollType.Immediate</c>). The caller guards against feedback loops through
    /// <see cref="ScrollChanged"/>.
    /// </summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal void SetScrollPositionImmediate(double? scrollLeft, double? scrollTop)
    {
        if (scrollLeft is { } left)
        {
            _pendingScrollLeft = left;
        }

        if (scrollTop is { } top)
        {
            _pendingScrollTop = top;
        }

        if (_measurer is not null)
        {
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(scrollLeft, scrollTop), ScrollType.Immediate);
        }
    }
}
