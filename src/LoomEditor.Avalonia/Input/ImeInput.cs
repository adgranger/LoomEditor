using Avalonia;
using Avalonia.Input.TextInput;
using LoomEditor.Core.Model;
using LoomEditor.View.Controller;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Input;

/// <summary>
/// Types with an IME, dead keys and the emoji panel through Avalonia's <see cref="TextInputMethodClient"/> (TSF on Windows,
/// <c>NSTextInputClient</c> on macOS, IBus / Fcitx on Linux). The twin of the WinUI <c>ImeInput</c>; the logic is in
/// <see cref="EditContextModel"/> (Monaco: <c>NativeEditContext</c>).
/// </summary>
/// <remarks>
/// Avalonia gives the composition as preedit text and the result through the <c>TextInput</c> event. As in WinUI, the preedit
/// text is typed into the model with <c>CompositionType</c> and underlined; the commit replaces it, then the composition ends.
/// The IME sees the lines of the primary selection (<see cref="SurroundingText"/>), as WinUI's <c>TextRequested</c>.
/// Threading: UI thread only.
/// </remarks>
internal sealed class ImeInput : TextInputMethodClient
{
    private readonly EditContextModel _editContextModel = new();
    private readonly Visual _textView;
    private readonly ViewModel _viewModel;
    private readonly TextModel _model;
    private readonly Func<bool> _isReadOnly;
    private readonly Func<Rect> _getCursorRectangle;
    private int _compositionStart;
    private int _compositionLength;
    private bool _applying;

    public ImeInput(Visual textView, ViewModel viewModel, TextModel model, Func<bool> isReadOnly, Func<Rect> getCursorRectangle)
    {
        _textView = textView;
        _viewModel = viewModel;
        _model = model;
        _isReadOnly = isReadOnly;
        _getCursorRectangle = getCursorRectangle;
        _editContextModel.Update(_model, _viewModel.GetSelections()[0]);
    }

    /// <summary>Whether preedit text is in the model (between the first preedit and the commit or cancel).</summary>
    public bool IsComposing { get; private set; }

    /// <inheritdoc />
    public override Visual TextViewVisual => _textView;

    /// <inheritdoc />
    public override bool SupportsPreedit => true;

    /// <inheritdoc />
    public override bool SupportsSurroundingText => true;

    /// <inheritdoc />
    public override string SurroundingText => _editContextModel.Text;

    /// <inheritdoc />
    public override Rect CursorRectangle => _getCursorRectangle();

    /// <inheritdoc />
    /// <remarks>Set by the IME for a reconversion: WinUI's <c>SelectionUpdating</c>.</remarks>
    public override TextSelection Selection
    {
        get => new(_editContextModel.Selection.Start, _editContextModel.Selection.End);
        set
        {
            int length = _editContextModel.Text.Length;
            Apply(_editContextModel.OnSelectionUpdate(Math.Clamp(value.Start, 0, length), Math.Clamp(value.End, 0, length)));
        }
    }

    /// <inheritdoc />
    public override void SetPreeditText(string? preeditText) => SetPreeditText(preeditText, null);

    /// <inheritdoc />
    /// <remarks>WinUI's <c>TextUpdating</c> during a composition. An empty text cancels the composition and removes what it typed.</remarks>
    public override void SetPreeditText(string? preeditText, int? cursorPos)
    {
        if (_isReadOnly())
        {
            return;
        }

        string text = preeditText ?? string.Empty;
        if (text.Length == 0)
        {
            if (IsComposing)
            {
                Replace(string.Empty, 0);
                EndComposition();
            }

            return;
        }

        if (!IsComposing)
        {
            IsComposing = true;
            _compositionStart = _editContextModel.Selection.Start;
            _compositionLength = _editContextModel.Selection.End - _compositionStart;
            _viewModel.StartComposition();
        }

        Replace(text, ClampCursor(text, cursorPos ?? text.Length));
        _viewModel.SetCompositionFormats(
            _editContextModel.ToCompositionFormats(_model, [(_compositionStart, _compositionStart + _compositionLength, "thin")]));
    }

    /// <summary>
    /// Commits <paramref name="text"/> (from the <c>TextInput</c> event) in place of the preedit text and ends the composition
    /// (WinUI: the last <c>TextUpdating</c>, then <c>CompositionCompleted</c>). Returns <see langword="false"/> when no
    /// composition is active: the text is plain typing.
    /// </summary>
    public bool TryCommit(string text)
    {
        if (!IsComposing)
        {
            return false;
        }

        Replace(text, text.Length);
        EndComposition();
        return true;
    }

    /// <summary>Ends the composition where it is and asks the IME to drop its own (the editor became read-only).</summary>
    public void Reset()
    {
        if (IsComposing)
        {
            EndComposition();
            RequestReset();
        }
    }

    /// <summary>Re-reads the lines of the primary selection and tells the IME what changed (WinUI: <c>RequestSync</c>).</summary>
    public void RequestSync()
    {
        if (!_applying)
        {
            Sync(notify: true);
        }
    }

    /// <summary>Tells the IME that the caret moved on screen (scroll, size, font): WinUI's <c>NotifyLayoutChanged</c>.</summary>
    public void RequestLayoutChanged() => RaiseCursorRectangleChanged();

    // Replaces the composed range with `text` and puts the caret `cursor` characters into it.
    private void Replace(string text, int cursor)
    {
        int end = _compositionStart + _compositionLength;
        int caret = _compositionStart + cursor;
        TypeData? data = _editContextModel.OnTextUpdate(new TextUpdate(_compositionStart, end, text, caret, caret));
        _compositionLength = text.Length;
        Apply(data);
    }

    private void EndComposition()
    {
        IsComposing = false;
        _viewModel.SetCompositionFormats(Array.Empty<CompositionFormat>());
        _applying = true;
        try
        {
            _viewModel.EndComposition("keyboard");
        }
        finally
        {
            _applying = false;
        }

        Sync(notify: true);
    }

    private void Apply(TypeData? data)
    {
        if (data is null)
        {
            return;
        }

        _applying = true;
        try
        {
            EditContextModel.Apply(_viewModel, data.Value);
        }
        finally
        {
            _applying = false;
        }

        // The IME made the change itself and knows the text: only our copy is refreshed (WinUI: `Sync(notify: false)`).
        Sync(notify: false);
    }

    // Monaco: `_updateEditContext`.
    private void Sync(bool notify)
    {
        (bool textChanged, bool selectionChanged) = _editContextModel.Update(_model, _viewModel.GetSelections()[0]);
        if (!notify)
        {
            return;
        }

        if (textChanged)
        {
            RaiseSurroundingTextChanged();
        }

        if (textChanged || selectionChanged)
        {
            RaiseSelectionChanged();
            RaiseCursorRectangleChanged();
        }
    }

    // The cursor never falls between the two halves of a surrogate pair: it goes after the pair.
    private static int ClampCursor(string text, int cursor)
    {
        cursor = Math.Clamp(cursor, 0, text.Length);
        if (cursor > 0 && cursor < text.Length && char.IsSurrogatePair(text[cursor - 1], text[cursor]))
        {
            cursor++;
        }

        return cursor;
    }
}
