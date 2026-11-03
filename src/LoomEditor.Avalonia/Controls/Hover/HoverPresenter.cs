using Avalonia;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.Themes;
using LoomEditor.View.Features.Hover;
using LoomEditor.View.Hover;
using LoomEditor.View.Layout;
using LoomEditorControl = LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Controls.Hover;

/// <summary>Shows the hover content in a <see cref="HoverWidget"/> on the overlay canvas, under or over the anchor.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HoverPresenter : IHoverPresenter
{
    private readonly OverlayHost _overlay;
    private readonly LoomEditorControl _owner;

    public HoverPresenter(OverlayHost overlay, LoomEditorControl owner)
    {
        _overlay = overlay;
        _owner = owner;
        Widget.PointerOverChanged += OnPointerOverChanged;
    }

    /// <inheritdoc />
    public bool IsVisible { get; private set; }

    /// <inheritdoc />
    public bool IsMouseOver { get; private set; }

    /// <summary>The widget on the overlay, for the tests.</summary>
    internal HoverWidget Widget { get; } = new();

    internal void ApplyTheme(EditorTheme theme) => Widget.ApplyTheme(theme);

    /// <inheritdoc />
    public void Show(HoverContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Range range = content.Anchor.Range;
        if (range.IsEmpty && _owner.Model.GetLineMaxColumn(range.StartLineNumber) > 1)
        {
            range = new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn + 1);
        }

        if (!_owner.TryGetRangeStartBounds(range, out Rect rect))
        {
            Hide();
            return;
        }

        Widget.Update(content.Parts, _owner.FontFamily, _owner.FontSize);
        Size desired = _overlay.Measure(Widget);
        OverlayRect placed = OverlayPlacement.Place(
            rect.X, rect.Y, rect.Bottom, desired.Width, desired.Height, _overlay.Width, _overlay.Height);
        _overlay.Show(Widget, placed);
        IsVisible = true;
    }

    /// <inheritdoc />
    public void Hide()
    {
        OverlayHost.Hide(Widget);
        IsVisible = false;
        IsMouseOver = false;
    }

    private void OnPointerOverChanged(bool isOver)
    {
        IsMouseOver = isOver;
        if (!isOver)
        {
            // Keeps the controller in step: it hides the content and resets its own state.
            _owner.Commands.Execute(HoverCommands.HideHover);
        }
    }
}
