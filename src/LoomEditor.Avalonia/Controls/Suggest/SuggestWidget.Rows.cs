using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using LoomEditor.View.Suggest;

namespace LoomEditor.Avalonia.Controls.Suggest;

// Monaco: `ItemRenderer.renderElement` (what a row shows) and `showSuggestions` (the detail strip).
internal sealed partial class SuggestWidget
{
    /// <summary>Fills the visible rows from <paramref name="state"/> and shows the detail of the selected item.</summary>
    internal void Refresh(SuggestListState state, FontFamily font, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;

        IReadOnlyList<SuggestItem> visible = state.VisibleItems;
        for (int i = 0; i < _rows.Length; i++)
        {
            SuggestRow row = _rows[i];
            if (i >= state.VisibleRowCount)
            {
                row.Root.IsVisible = false;
                continue;
            }

            SuggestRowModel model = SuggestRowModel.Create(visible[i], state.FirstVisibleIndex + i == state.SelectedIndex);
            Fill(row, model, font, fontSize);
        }

        string detail = state.SelectedItem is { } selected ? SuggestRowModel.DetailText(selected) : "";
        _details.Text = detail;
        _details.FontFamily = font;
        _details.FontSize = fontSize;
        _details.IsVisible = detail.Length != 0;
    }

    private void Fill(SuggestRow row, SuggestRowModel model, FontFamily font, double fontSize)
    {
        IBrush text = model.IsSelected ? _selectedForeground : _foreground;
        IBrush matchBrush = model.IsSelected ? _focusHighlight : _highlight;

        row.Root.IsVisible = true;
        // Transparent, not null: a row without a background does not receive the pointer.
        row.Root.Background = model.IsSelected ? _selectedBackground : Brushes.Transparent;
        ToolTip.SetTip(row.Root, model.KindLabel);
        row.Glyph.Text = model.BadgeGlyph;
        row.Glyph.Foreground = text;

        TextBlock label = row.Label;
        label.FontFamily = font;
        label.FontSize = fontSize;
        label.Foreground = text;
        label.TextDecorations = model.IsDeprecated ? TextDecorations.Strikethrough : null;
        label.Inlines!.Clear();
        foreach ((string segment, bool isMatch) in model.LabelSegments())
        {
            AddRun(label, segment, isMatch ? matchBrush : null, isMatch);
        }

        if (!string.IsNullOrEmpty(model.LabelDetail))
        {
            AddRun(label, model.LabelDetail, _dim, false);
        }

        row.Description.Text = model.LabelDescription ?? "";
        row.Description.Foreground = _dim;
        row.Description.FontFamily = font;
        row.Description.FontSize = fontSize;
    }

    private static void AddRun(TextBlock block, string text, IBrush? foreground, bool bold)
    {
        if (text.Length == 0)
        {
            return;
        }

        var run = new Run { Text = text };
        if (foreground is not null)
        {
            run.Foreground = foreground;
        }

        if (bold)
        {
            run.FontWeight = FontWeight.SemiBold;
        }

        block.Inlines!.Add(run);
    }
}
