using LoomEditor.WinUI.Input;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// IME, emoji panel and other text services through `CoreTextEditContext` (ADR 0006, decision 11). Plain typing stays on
// `CharacterReceived`; while a composition is active, keys and characters are ignored.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    private ImeInput? _ime;

    private bool IsComposing => _ime is { IsComposing: true };

    private void InitializeIme()
    {
        GotFocus += OnImeGotFocus;
        LostFocus += OnImeLostFocus;
        _context.SelectionChanged += (_, _) => _ime?.RequestSync();
        _context.Model.ContentChanged += (_, _) => _ime?.RequestSync();
    }

    private void StartIme()
    {
        _ime ??= ImeInput.TryCreate(_viewModel, _context.Model, DispatcherQueue, () => IsReadOnly);
        if (HasFocus)
        {
            _ime?.NotifyFocusEnter();
        }
    }

    private void StopIme()
    {
        // `Dispose` unsubscribes every handler and notifies the focus leave.
        _ime?.Dispose();
        _ime = null;
    }

    private void OnImeGotFocus(object sender, RoutedEventArgs e) => _ime?.NotifyFocusEnter();

    private void OnImeLostFocus(object sender, RoutedEventArgs e) => _ime?.NotifyFocusLeave();
}
