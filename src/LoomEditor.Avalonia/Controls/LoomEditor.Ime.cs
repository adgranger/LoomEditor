using Avalonia;
using Avalonia.Input.TextInput;
using LoomEditor.Avalonia.Input;

namespace LoomEditor.Avalonia.Controls;

// IME, dead keys and the emoji panel through Avalonia's `TextInputMethodClient` (ADR 0006, decision 11). Plain typing stays
// on `TextInput`; while a composition is active, keys are ignored and `TextInput` commits the composition.
public sealed partial class LoomEditor
{
    private ImeInput _ime = null!;

    private bool IsComposing => _ime.IsComposing;

    // The client, for the tests (they call it directly: there is no IME in the headless platform).
    internal TextInputMethodClient ImeClient => _ime;

    private void InitializeIme()
    {
        _ime = new ImeInput(this, _viewModel, _context.Model, () => IsReadOnly, GetImeCursorRectangle);

        // The read-only editor still gives its client, so that an IME does not type into another control (commits are ignored).
        // A widget of the editor (a find box) has the focus itself and gives its own client.
        TextInputMethodClientRequested += (_, e) =>
        {
            if (HasFocus)
            {
                e.Client = _ime;
            }
        };
        _context.SelectionChanged += (_, _) => _ime.RequestSync();
        _context.Model.ContentChanged += (_, _) => _ime.RequestSync();
        _viewModel.CompositionFormatsChanged += (_, _) => Invalidate();
        _viewModel.ViewLayout.Scroll += (_, _) => _ime.RequestLayoutChanged();
        _viewModel.LayoutInfoChanged += (_, _) => _ime.RequestLayoutChanged();
    }

    // WinUI: `GetImeLayoutBounds`. The caret of the primary selection, one line high, in editor coordinates (DIPs): Avalonia
    // converts it to screen pixels itself.
    private Rect GetImeCursorRectangle()
    {
        var position = _viewModel.GetViewSelections()[0].Position;
        if (_renderer is null
            || !_renderer.TryGetHorizontalRange(_viewModel, position.LineNumber, position.Column, position.Column, out double left, out _))
        {
            return default;
        }

        var layoutInfo = _viewModel.LayoutInfo;
        double x = layoutInfo.ContentLeft + left - _viewModel.ViewLayout.GetCurrentScrollLeft();
        double y = _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(position.LineNumber) - _viewModel.ViewLayout.GetCurrentScrollTop();
        var rectangle = new Rect(x, y, 1, _viewModel.FontInfo.LineHeight);
        return _canvas?.TranslatePoint(default, this) is { } origin ? rectangle.Translate(origin) : rectangle;
    }
}
