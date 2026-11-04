using LoomEditor.Core.Comments;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.Comments;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.Comments;
#endif

// The header: the title, and for a resolved thread a badge, a ▸ / ▾ glyph and (collapsed) the first line of the first comment and the count.
internal sealed partial class CommentThreadWidget
{
    private void AddHeader()
    {
        bool resolved = _viewModel.Thread.State == CommentThreadState.Resolved;
        var row = NewHeaderRow();
        if (resolved)
        {
            row.Children.Add(Text(_viewModel.IsCollapsed ? "▸" : "▾", _foreground));
        }

        row.Children.Add(BoldText(_viewModel.Title, _foreground));
        if (!resolved)
        {
            _rows.Children.Add(row);
            return;
        }

        row.Children.Add(ResolvedBadge(Math.Max(9, _fontSize - 2)));
        if (_viewModel.IsCollapsed)
        {
            row.Children.Add(SummaryText(CollapsedSummary()));
        }

        // Only a resolved thread's header is interactive: it expands and collapses the thread.
        _rows.Children.Add(HeaderToggle(row));
    }
}
