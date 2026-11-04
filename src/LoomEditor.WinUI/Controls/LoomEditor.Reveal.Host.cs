namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    private void InvalidateView() => _canvas?.Invalidate();
}
