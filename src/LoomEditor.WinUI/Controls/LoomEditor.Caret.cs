using LoomEditor.Rendering;
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
    private DispatcherQueueTimer? _blinkTimer;

    private void InitializeCaret()
    {
        _cursorBlinking.Changed += (_, _) => _canvas?.Invalidate();
        GotFocus += OnCaretFocusChanged;
        LostFocus += OnCaretFocusChanged;
        _context.SelectionChanged += OnCaretSelectionChanged;
    }

    private bool HasFocus => FocusState != FocusState.Unfocused;

    private void OnCaretFocusChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.SetHasFocus(HasFocus);
        _cursorBlinking.Update(HasFocus, readOnly: false);
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
