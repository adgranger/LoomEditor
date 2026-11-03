using Avalonia.Input;

namespace LoomEditor.Avalonia.Controls;

public sealed partial class LoomEditor
{
    // The pointer of the press being handled, for a zone that asks for the capture.
    private IPointer? _zonePressPointer;

    private void CapturePointerForZone() => Capture(_zonePressPointer!);
}
