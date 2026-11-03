using Avalonia.Threading;

namespace LoomEditor.Avalonia.Controls;

public sealed partial class LoomEditor
{
    private bool _diffGutterArrangePending;

    // Called after each draw: Avalonia refuses a layout change during the render pass, so the buttons are placed right after it.
    private void ArrangeDiffGutterAfterDraw()
    {
        if (_diffGutterArrangePending || _diffGutter is null)
        {
            return;
        }

        _diffGutterArrangePending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _diffGutterArrangePending = false;
            _diffGutter?.Arrange();
        });
    }
}
