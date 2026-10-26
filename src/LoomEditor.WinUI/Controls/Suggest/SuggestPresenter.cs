using Range = LoomEditor.Core.Text.Range;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Features.Suggest;
using LoomEditor.View.Layout;
using LoomEditor.View.Suggest;
using LoomEditor.WinUI.Controls.Overlay;
using Windows.Foundation;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Controls.Suggest;

/// <summary>Shows the completion list in a <see cref="SuggestWidget"/> on the overlay canvas, under or over the start of the typed word.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class SuggestPresenter : ISuggestPresenter
{
    private readonly OverlayHost _overlay;
    private readonly LoomEditorControl _owner;
    private readonly SuggestWidget _widget = new();
    private SuggestListState? _state;
    private Position _anchor;

    public SuggestPresenter(OverlayHost overlay, LoomEditorControl owner)
    {
        _overlay = overlay;
        _owner = owner;
        _widget.RowPressed += OnRowPressed;
    }

    /// <inheritdoc />
    public bool IsVisible { get; private set; }

    internal void ApplyTheme(EditorTheme theme) => _widget.ApplyTheme(theme);

    /// <inheritdoc />
    public void Show(SuggestListState state, Position anchor)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!ReferenceEquals(_state, state))
        {
            Unsubscribe();
            _state = state;
            state.Changed += OnChanged;
        }

        // The list does not move while the user types more letters of the word: it is only placed again when the anchor changed.
        bool moved = !IsVisible || anchor != _anchor;
        _anchor = anchor;
        _widget.Refresh(state, _owner.FontFamily, _owner.FontSize);
        if (moved)
        {
            Place();
        }
    }

    /// <inheritdoc />
    public void Hide()
    {
        Unsubscribe();
        OverlayHost.Hide(_widget);
        IsVisible = false;
    }

    private void Unsubscribe()
    {
        if (_state is not null)
        {
            _state.Changed -= OnChanged;
            _state = null;
        }
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        if (_state is null)
        {
            return;
        }

        _widget.Refresh(_state, _owner.FontFamily, _owner.FontSize);
        Place();
    }

    private void Place()
    {
        int endColumn = Math.Min(_anchor.Column + 1, _owner.Model.GetLineMaxColumn(_anchor.LineNumber));
        var rects = _owner.AutomationGeometry.GetBoundingRectangles(new Range(_anchor.LineNumber, _anchor.Column, _anchor.LineNumber, endColumn));
        if (rects.Count == 0)
        {
            Hide();
            return;
        }

        var rect = rects[0];
        _widget.Measure(new Size(_overlay.Width, _overlay.Height));
        Size desired = _widget.DesiredSize;
        OverlayRect placed = OverlayPlacement.Place(
            rect.X, rect.Y, rect.Y + rect.Height, SuggestWidget.WidgetWidth, desired.Height, _overlay.Width, _overlay.Height);
        _overlay.Show(_widget, placed);
        IsVisible = true;
    }

    private void OnRowPressed(int slot)
    {
        if (_state is null)
        {
            return;
        }

        _state.Select(_state.FirstVisibleIndex + slot);
        _owner.Commands.Execute(SuggestCommands.AcceptSelectedSuggestion);
    }
}
