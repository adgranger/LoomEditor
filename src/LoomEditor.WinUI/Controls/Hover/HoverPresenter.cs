using Range = LoomEditor.Core.Text.Range;
using LoomEditor.Themes;
using LoomEditor.View.Features.Hover;
using LoomEditor.View.Hover;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.Overlay;
using Windows.Foundation;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Controls.Hover;

/// <summary>Shows the hover content in a <see cref="HoverWidget"/> on the overlay canvas, under or over the anchor.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class HoverPresenter : IHoverPresenter
{
    private readonly OverlayHost _overlay;
    private readonly LoomEditorControl _owner;
    private readonly HoverWidget _widget = new();

    public HoverPresenter(OverlayHost overlay, LoomEditorControl owner)
    {
        _overlay = overlay;
        _owner = owner;
        _widget.PointerOverChanged += OnPointerOverChanged;
    }

    /// <inheritdoc />
    public bool IsVisible { get; private set; }

    /// <inheritdoc />
    public bool IsMouseOver { get; private set; }

    internal void ApplyTheme(EditorTheme theme) => _widget.ApplyTheme(theme);

    /// <inheritdoc />
    public void Show(HoverContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Range range = content.Anchor.Range;
        if (range.IsEmpty && _owner.Model.GetLineMaxColumn(range.StartLineNumber) > 1)
        {
            range = new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn + 1);
        }

        var rects = _owner.AutomationGeometry.GetBoundingRectangles(range);
        if (rects.Count == 0)
        {
            Hide();
            return;
        }

        var rect = rects[0];
        _widget.Update(content.Parts, _owner.FontFamily, _owner.FontSize);
        _widget.Measure(new Size(_overlay.Width, _overlay.Height));
        Size desired = _widget.DesiredSize;
        OverlayRect placed = OverlayPlacement.Place(
            rect.X, rect.Y, rect.Y + rect.Height, desired.Width, desired.Height, _overlay.Width, _overlay.Height);
        _overlay.Show(_widget, placed);
        IsVisible = true;
    }

    /// <inheritdoc />
    public void Hide()
    {
        OverlayHost.Hide(_widget);
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
