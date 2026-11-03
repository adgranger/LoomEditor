using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Rendering;
using LoomEditor.View.Layout;
using LoomEditor.View.Cursors;

namespace LoomEditor.Avalonia.Controls;

// Caret blinking and focus. Monaco: `ViewCursors.onFocusChanged`, `onCursorStateChanged` and `_updateBlinking`;
// the state machine is `CursorBlinking`, this part only drives it with a timer.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    private readonly CursorBlinking _cursorBlinking = new();

    /// <summary>Identifies the <see cref="CursorStyle"/> styled property.</summary>
    public static readonly StyledProperty<TextEditorCursorStyle> CursorStyleProperty =
        AvaloniaProperty.Register<LoomEditor, TextEditorCursorStyle>(nameof(CursorStyle), TextEditorCursorStyle.Line);

    /// <summary>Identifies the <see cref="OvertypeCursorStyle"/> styled property.</summary>
    public static readonly StyledProperty<TextEditorCursorStyle> OvertypeCursorStyleProperty =
        AvaloniaProperty.Register<LoomEditor, TextEditorCursorStyle>(nameof(OvertypeCursorStyle), TextEditorCursorStyle.Block);

    /// <summary>Identifies the <see cref="InputMode"/> property.</summary>
    public static readonly DirectProperty<LoomEditor, EditorInputMode> InputModeProperty =
        AvaloniaProperty.RegisterDirect<LoomEditor, EditorInputMode>(nameof(InputMode), editor => editor.InputMode);

    private EditorInputMode _inputMode = EditorInputMode.Insert;
    private DispatcherTimer? _blinkTimer;

    /// <summary>How the caret is drawn in insert mode. Defaults to <see cref="TextEditorCursorStyle.Line"/>.</summary>
    public TextEditorCursorStyle CursorStyle
    {
        get => GetValue(CursorStyleProperty);
        set => SetValue(CursorStyleProperty, value);
    }

    /// <summary>How the caret is drawn in overtype mode. Defaults to <see cref="TextEditorCursorStyle.Block"/>.</summary>
    public TextEditorCursorStyle OvertypeCursorStyle
    {
        get => GetValue(OvertypeCursorStyleProperty);
        set => SetValue(OvertypeCursorStyleProperty, value);
    }

    /// <summary>Whether typing inserts or overwrites. Read-only: the <c>Insert</c> key or the toggle-overtype command changes it.</summary>
    public EditorInputMode InputMode
    {
        get => _inputMode;
        private set => SetAndRaise(InputModeProperty, ref _inputMode, value);
    }

    private bool HasFocus => IsFocused;

    private static void InitializeCaretProperties()
    {
        CursorStyleProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.SyncCaretStyle());
        OvertypeCursorStyleProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.SyncCaretStyle());
        InputModeProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.SyncCaretStyle());
    }

    private void SyncCaretStyle()
    {
        if (_renderer is { } renderer)
        {
            renderer.CursorStyle = CursorStyle;
            renderer.OvertypeCursorStyle = OvertypeCursorStyle;
            renderer.InputMode = InputMode;
        }

        Invalidate();
    }

    private void InitializeCaret()
    {
        _cursorBlinking.Changed += (_, _) => Invalidate();
        _context.SelectionChanged += OnCaretSelectionChanged;
        _context.InputModeChanged += (_, _) => InputMode = _context.InputMode;
    }

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        OnCaretFocusChanged();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        OnCaretFocusChanged();
    }

    private void OnCaretFocusChanged()
    {
        _viewModel.SetHasFocus(HasFocus);
        _cursorBlinking.Update(HasFocus, IsReadOnly);
        SyncBlinkTimer(restart: true);
        Invalidate();
    }

    private void OnCaretSelectionChanged(object? sender, EventArgs e)
    {
        _cursorBlinking.Reset();
        SyncBlinkTimer(restart: true);
        Invalidate();
    }

    // Starts the timer while the caret blinks and stops it otherwise, so an unfocused editor uses no timer.
    // `restart` gives the caret a full interval of visibility, like Monaco's `cancelAndSet`.
    private void SyncBlinkTimer(bool restart)
    {
        if (!_cursorBlinking.NeedsTimer)
        {
            _blinkTimer?.Stop();
            return;
        }

        if (_blinkTimer is null)
        {
            _blinkTimer = new DispatcherTimer { Interval = CursorBlinking.BlinkInterval };
            _blinkTimer.Tick += OnBlinkTimerTick;
        }

        if (restart || !_blinkTimer.IsEnabled)
        {
            _blinkTimer.Stop();
            _blinkTimer.Start();
        }
    }

    private void OnBlinkTimerTick(object? sender, EventArgs e) => _cursorBlinking.Tick();

    private void StopBlinkTimer()
    {
        if (_blinkTimer is null)
        {
            return;
        }

        _blinkTimer.Stop();
        _blinkTimer.Tick -= OnBlinkTimerTick;
        _blinkTimer = null;
    }
}
