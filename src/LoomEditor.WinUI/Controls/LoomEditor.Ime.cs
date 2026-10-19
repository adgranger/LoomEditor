using LoomEditor.WinUI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace LoomEditor.WinUI.Controls;

// IME, emoji panel and other text services through `CoreTextEditContext` (ADR 0006, decision 11). Plain typing stays on
// `CharacterReceived`; while a composition is active, keys and characters are ignored.
#pragma warning disable CA1001 // Reported on the first partial declaration; see the suppression in LoomEditor.cs.
public sealed partial class LoomEditor
#pragma warning restore CA1001
{
    private ImeInput? _ime;
    private AppWindow? _observedAppWindow;

    private bool IsComposing => _ime is { IsComposing: true };

    private void InitializeIme()
    {
        GotFocus += OnImeGotFocus;
        LostFocus += OnImeLostFocus;
        _context.SelectionChanged += (_, _) => _ime?.RequestSync();
        _viewModel.CompositionFormatsChanged += (_, _) => _canvas?.Invalidate();
        _context.Model.ContentChanged += (_, _) => _ime?.RequestSync();
    }

    private void StartIme()
    {
        _ime ??= ImeInput.TryCreate(_viewModel, _context.Model, DispatcherQueue, () => IsReadOnly, GetImeLayoutBounds);
        if (HasFocus)
        {
            _ime?.NotifyFocusEnter();
        }

        ObserveAppWindow();
    }

    private void StopIme()
    {
        StopObservingAppWindow();

        // `Dispose` unsubscribes every handler and notifies the focus leave.
        _ime?.Dispose();
        _ime = null;
    }

    private void OnImeGotFocus(object sender, RoutedEventArgs e) => _ime?.NotifyFocusEnter();

    private void OnImeLostFocus(object sender, RoutedEventArgs e) => _ime?.NotifyFocusLeave();

    // Monaco: `_updateSelectionAndControlBoundsData`. TextBounds is the start of the primary selection to the end of its
    // first line (one line high); ControlBounds is the text area. Both in screen pixels.
    private (Rect Text, Rect Control)? GetImeLayoutBounds()
    {
        if (_renderer is null)
        {
            return null;
        }

        var selection = _viewModel.GetSelections()[0].Range;
        int lineNumber = selection.StartLineNumber;
        int endColumn = selection.EndLineNumber == lineNumber ? selection.EndColumn : _context.Model.GetLineMaxColumn(lineNumber);
        if (!_renderer.TryGetHorizontalRange(_viewModel, lineNumber, selection.StartColumn, endColumn, out double left, out double width))
        {
            return null;
        }

        var layoutInfo = _viewModel.LayoutInfo;
        double scrollLeft = _viewModel.ViewLayout.GetCurrentScrollLeft();
        double scrollTop = _viewModel.ViewLayout.GetCurrentScrollTop();
        double top = _viewModel.ViewLayout.GetVerticalOffsetForLineNumber(lineNumber) - scrollTop;

        // The canvas is the control's content: its origin is the control's.
        var text = new Rect(layoutInfo.ContentLeft + left - scrollLeft, top, Math.Max(width, 1), _viewModel.FontInfo.LineHeight);
        var control = new Rect(layoutInfo.ContentLeft, 0, Math.Max(layoutInfo.Width - layoutInfo.ContentLeft, 0), layoutInfo.Height);
        if (ScreenCoordinates.ToScreenPixels(this, text) is not { } screenText
            || ScreenCoordinates.ToScreenPixels(this, control) is not { } screenControl)
        {
            return null;
        }

        return (screenText, screenControl);
    }

    // A window move changes the screen position of the candidate window without any XAML event.
    private void ObserveAppWindow()
    {
        if (XamlRoot?.ContentIslandEnvironment is not { } environment || _observedAppWindow is not null)
        {
            return;
        }

        _observedAppWindow = AppWindow.GetFromWindowId(environment.AppWindowId);
        _observedAppWindow.Changed += OnAppWindowChanged;
    }

    private void StopObservingAppWindow()
    {
        if (_observedAppWindow is not null)
        {
            _observedAppWindow.Changed -= OnAppWindowChanged;
            _observedAppWindow = null;
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            _ime?.RequestLayoutChanged();
        }
    }
}
