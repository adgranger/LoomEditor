using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LoomEditor.Avalonia.Controls;

/// <summary>The surface the editor draws on: the Avalonia twin of the Win2D <c>CanvasControl</c> (<c>PART_Canvas</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class EditorCanvas : Control
{
    /// <summary>Creates the canvas; it clips what is drawn to its bounds.</summary>
    public EditorCanvas()
    {
        ClipToBounds = true;
    }

    /// <summary>Draws a frame: gets the drawing context and the size of the canvas.</summary>
    public Action<DrawingContext, Size>? DrawFrame { get; set; }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DrawFrame?.Invoke(context, Bounds.Size);
    }
}
