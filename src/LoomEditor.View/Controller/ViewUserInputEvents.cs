// Ported from microsoft/vscode: src/vs/editor/browser/view/viewUserInputEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>A mouse event and what is under it (Monaco: <c>IEditorMouseEvent</c>).</summary>
/// <param name="Event">The mouse event.</param>
/// <param name="Target">The target, in view coordinates when raised by the view, in model coordinates for features.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record EditorMouseEventArgs(EditorMouseEvent Event, MouseTarget Target);

/// <summary>
/// The mouse events that features listen to: the view emits them with view coordinates and they reach features with model
/// coordinates (Monaco: <c>ViewUserInputEvents</c>, without view zones).
/// </summary>
/// <remarks>
/// Not ported: the other events (leave, drag, drop, wheel, key, context menu).
/// Threading: UI thread only.
/// </remarks>
internal sealed class ViewUserInputEvents(ICoordinatesConverter coordinatesConverter)
{
    /// <summary>Monaco: <c>onMouseDown</c>.</summary>
    public event EventHandler<EditorMouseEventArgs>? MouseDown;

    /// <summary>Monaco: <c>onMouseUp</c>.</summary>
    public event EventHandler<EditorMouseEventArgs>? MouseUp;

    /// <summary>Monaco: <c>emitMouseDown</c>.</summary>
    public void EmitMouseDown(EditorMouseEventArgs e) => MouseDown?.Invoke(this, Convert(e));

    /// <summary>Monaco: <c>emitMouseUp</c>.</summary>
    public void EmitMouseUp(EditorMouseEventArgs e) => MouseUp?.Invoke(this, Convert(e));

    private EditorMouseEventArgs Convert(EditorMouseEventArgs e) =>
        e with { Target = ConvertViewToModelMouseTarget(e.Target, coordinatesConverter) };

    /// <summary>Monaco: <c>convertViewToModelMouseTarget</c>.</summary>
    public static MouseTarget ConvertViewToModelMouseTarget(MouseTarget target, ICoordinatesConverter coordinatesConverter)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(coordinatesConverter);

        return target with
        {
            Position = target.Position is { } position ? coordinatesConverter.ConvertViewPositionToModelPosition(position) : null,
            Range = target.Range is { } range ? coordinatesConverter.ConvertViewRangeToModelRange(range) : null,
        };
    }
}
