using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    // The pointer of the press being handled, for a zone that asks for the capture.
    private Pointer? _zonePressPointer;

    private void CapturePointerForZone() => CapturePointer(_zonePressPointer!);
}
