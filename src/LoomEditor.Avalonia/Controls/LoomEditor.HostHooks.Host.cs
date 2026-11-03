using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// What a host of this control needs: the public members for applications (phase 13).
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class LoomEditor
{
    /// <summary>Raised when the scroll position or the scroll height changes: scrolling, edits, folding, view zones, wrapping.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public event EventHandler<EditorScrollChangedEventArgs>? ScrollChanged;

    /// <summary>Raised when the layout of the editor changes: size, gutter widths, minimap, line numbers, font and line height.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    public event EventHandler? LayoutChanged;

    /// <summary>The view zones of the editor.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal global::LoomEditor.View.Layout.ViewZones ViewZones => _viewModel.ViewZones;

    /// <summary>Converts between model and view positions (needed to map whitespaces and wrapped lines to model lines).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal ICoordinatesConverter CoordinatesConverter => _viewModel.CoordinatesConverter;

    /// <summary>Whether lines wrap.</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal bool IsWrapping => WordWrap != EditorWordWrap.Off;

    /// <summary>The layout of the editor (read-only).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal EditorLayoutInfo LayoutInfo => _viewModel.LayoutInfo;

    /// <summary>The whitespaces of the layout, the view zones included (Monaco: <c>getWhitespaces</c>).</summary>
    /// <remarks>Threading: UI thread only.</remarks>
    internal IReadOnlyList<IEditorWhitespace> GetWhitespaces() => _viewModel.ViewLayout.GetWhitespaces();

    /// <summary>
    /// The vertical offset of a model line in scroll coordinates (Monaco: <c>getTopForLineNumber</c>); subtract <see cref="ScrollTop"/> for a y in the editor.
    /// <paramref name="includeViewZones"/> adds the zones above the line, so the result is the top of the line's text. A wrapped line gives the top of its first
    /// row. A line inside a hidden area gives the top of the last row of the nearest visible line above it (the top of the first line when none is visible).
    /// </summary>
    /// <param name="lineNumber">The model line number; it is clamped to <c>1</c> to the line count.</param>
    /// <param name="includeViewZones">Whether to add the height of the view zones above the line.</param>
    /// <returns>The offset, in DIPs.</returns>
    /// <remarks>Threading: UI thread only.</remarks>
    public double GetTopForLineNumber(int lineNumber, bool includeViewZones = false)
    {
        int clamped = Math.Clamp(lineNumber, 1, _context.Model.LineCount);
        int viewLine = _viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(clamped, 1)).LineNumber;
        return _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(viewLine, includeViewZones);
    }

    /// <summary>
    /// Replaces the whole text and resets the editor as setting <see cref="Text"/> does, even when <paramref name="text"/> equals the last value of
    /// <see cref="Text"/> (that property is not updated by edits, so setting it to the same string does nothing). The undo stack is cleared, the
    /// selection goes back to the start of the text and every decoration is removed (Monaco: <c>setValue</c>): the host sets its decorations again.
    /// <see cref="Text"/> is not updated by this method.
    /// </summary>
    /// <param name="text">The new text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public void ResetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _context.SetText(text);
        _viewModel.SetIsDominatedByLongLines(_context.Model.IsDominatedByLongLines());
    }

    private void RaiseScrollChanged(ScrollEvent e) =>
        ScrollChanged?.Invoke(
            this, new EditorScrollChangedEventArgs(e.ScrollTopChanged, e.ScrollLeftChanged, e.ScrollHeightChanged, e.ScrollTop, e.ScrollLeft));

    private void RaiseContentSizeChanged() =>
        ScrollChanged?.Invoke(
            this,
            new EditorScrollChangedEventArgs(
                false, false, true, _viewModel.ViewLayout.GetCurrentScrollTop(), _viewModel.ViewLayout.GetCurrentScrollLeft()));
}
