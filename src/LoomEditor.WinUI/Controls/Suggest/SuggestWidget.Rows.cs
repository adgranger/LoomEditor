using LoomEditor.Core.Filters;
using LoomEditor.View.Suggest;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Suggest;

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
                row.Root.Visibility = Visibility.Collapsed;
                continue;
            }

            SuggestRowModel model = SuggestRowModel.Create(visible[i], state.FirstVisibleIndex + i == state.SelectedIndex);
            Fill(row, model, font, fontSize);
        }

        string detail = state.SelectedItem is { } selected ? SuggestRowModel.DetailText(selected) : "";
        _details.Text = detail;
        _details.FontFamily = font;
        _details.FontSize = fontSize;
        _details.Visibility = detail.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Fill(SuggestRow row, SuggestRowModel model, FontFamily font, double fontSize)
    {
        Brush text = model.IsSelected ? _selectedForeground : _foreground;
        Brush matchBrush = model.IsSelected ? _focusHighlight : _highlight;

        row.Root.Visibility = Visibility.Visible;
        row.Root.Background = model.IsSelected ? _selectedBackground : null;
        ToolTipService.SetToolTip(row.Root, model.KindLabel);
        row.Glyph.Text = model.BadgeGlyph;
        row.Glyph.Foreground = text;

        TextBlock label = row.Label;
        label.FontFamily = font;
        label.FontSize = fontSize;
        label.Foreground = text;
        label.TextDecorations = model.IsDeprecated ? Windows.UI.Text.TextDecorations.Strikethrough : Windows.UI.Text.TextDecorations.None;
        label.Inlines.Clear();
        int position = 0;
        foreach (FilterMatch match in model.Matches)
        {
            int start = Math.Clamp(match.Start, position, model.Label.Length);
            int end = Math.Clamp(match.End, start, model.Label.Length);
            AddRun(label, model.Label[position..start], null, false);
            AddRun(label, model.Label[start..end], matchBrush, true);
            position = end;
        }

        AddRun(label, model.Label[position..], null, false);
        if (!string.IsNullOrEmpty(model.LabelDetail))
        {
            AddRun(label, model.LabelDetail, _dim, false);
        }

        row.Description.Text = model.LabelDescription ?? "";
        row.Description.FontFamily = font;
        row.Description.FontSize = fontSize;
        row.Description.Foreground = _dim;
    }

    private static void AddRun(TextBlock block, string text, Brush? foreground, bool bold)
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
            run.FontWeight = FontWeights.SemiBold;
        }

        block.Inlines.Add(run);
    }
}
