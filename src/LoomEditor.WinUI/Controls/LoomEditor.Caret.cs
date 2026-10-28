using LoomEditor.Rendering;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Caret blinking and focus. Monaco: `ViewCursors.onFocusChanged`, `onCursorStateChanged` and `_updateBlinking`;
// the state machine is `CursorBlinking`, this part only drives it with a timer.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    private readonly CursorBlinking _cursorBlinking = new();

    /// <summary>Identifies the <see cref="CursorStyle"/> dependency property.</summary>
    public static readonly DependencyProperty CursorStyleProperty = DependencyProperty.Register(
        nameof(CursorStyle), typeof(TextEditorCursorStyle), typeof(LoomEditor), new PropertyMetadata(TextEditorCursorStyle.Line, OnCursorStyleChanged));

    /// <summary>Identifies the <see cref="OvertypeCursorStyle"/> dependency property.</summary>
    public static readonly DependencyProperty OvertypeCursorStyleProperty = DependencyProperty.Register(
        nameof(OvertypeCursorStyle), typeof(TextEditorCursorStyle), typeof(LoomEditor), new PropertyMetadata(TextEditorCursorStyle.Block, OnCursorStyleChanged));

    /// <summary>Identifies the <see cref="InputMode"/> dependency property.</summary>
    public static readonly DependencyProperty InputModeProperty = DependencyProperty.Register(
        nameof(InputMode), typeof(EditorInputMode), typeof(LoomEditor), new PropertyMetadata(EditorInputMode.Insert, OnCursorStyleChanged));

    /// <summary>How the caret is drawn in insert mode. Defaults to <see cref="TextEditorCursorStyle.Line"/>.</summary>
    public TextEditorCursorStyle CursorStyle
    {
        get => (TextEditorCursorStyle)GetValue(CursorStyleProperty);
        set => SetValue(CursorStyleProperty, value);
    }

    /// <summary>How the caret is drawn in overtype mode. Defaults to <see cref="TextEditorCursorStyle.Block"/>.</summary>
    public TextEditorCursorStyle OvertypeCursorStyle
    {
        get => (TextEditorCursorStyle)GetValue(OvertypeCursorStyleProperty);
        set => SetValue(OvertypeCursorStyleProperty, value);
    }

    /// <summary>Whether typing inserts or overwrites. Read-only: the <c>Insert</c> key or the toggle-overtype command changes it.</summary>
    public EditorInputMode InputMode
    {
        get => (EditorInputMode)GetValue(InputModeProperty);
        private set => SetValue(InputModeProperty, value);
    }

    private static void OnCursorStyleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
        ((LoomEditor)sender).SyncCaretStyle();

    private void SyncCaretStyle()
    {
        if (_renderer is { } renderer)
        {
            renderer.CursorStyle = CursorStyle;
            renderer.OvertypeCursorStyle = OvertypeCursorStyle;
            renderer.InputMode = InputMode;
        }

        _canvas?.Invalidate();
    }
    private DispatcherQueueTimer? _blinkTimer;

    private void InitializeCaret()
    {
        _cursorBlinking.Changed += (_, _) => _canvas?.Invalidate();
        GotFocus += OnCaretFocusChanged;
        LostFocus += OnCaretFocusChanged;
        _context.SelectionChanged += OnCaretSelectionChanged;
        _context.InputModeChanged += (_, _) => InputMode = _context.InputMode;
    }

    private bool HasFocus => FocusState != FocusState.Unfocused;

    private void OnCaretFocusChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.SetHasFocus(HasFocus);
        _cursorBlinking.Update(HasFocus, IsReadOnly);
        SyncBlinkTimer(restart: true);
        _canvas?.Invalidate();
    }

    private void OnCaretSelectionChanged(object? sender, EventArgs e)
    {
        _cursorBlinking.Reset();
        SyncBlinkTimer(restart: true);
        _canvas?.Invalidate();
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
            _blinkTimer = DispatcherQueue.CreateTimer();
            _blinkTimer.Interval = CursorBlinking.BlinkInterval;
            _blinkTimer.IsRepeating = true;
            _blinkTimer.Tick += OnBlinkTimerTick;
        }

        if (restart || !_blinkTimer.IsRunning)
        {
            _blinkTimer.Stop();
            _blinkTimer.Start();
        }
    }

    private void OnBlinkTimerTick(DispatcherQueueTimer sender, object args) => _cursorBlinking.Tick();

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
