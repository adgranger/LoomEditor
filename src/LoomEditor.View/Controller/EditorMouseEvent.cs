// Ported from microsoft/vscode: src/vs/editor/browser/editorDom.ts (EditorMouseEvent, the fields the mouse handler reads)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Controller;

/// <summary>What the control gives for each pointer event.</summary>
/// <param name="X">X in DIPs, relative to the left of the control.</param>
/// <param name="Y">Y in DIPs, relative to the top of the control.</param>
/// <param name="LeftButton">The left button is down.</param>
/// <param name="MiddleButton">The middle button is down.</param>
/// <param name="AltKey">Alt is down.</param>
/// <param name="CtrlKey">Ctrl is down.</param>
/// <param name="ShiftKey">Shift is down.</param>
/// <param name="MetaKey">The Windows key is down.</param>
/// <param name="Detail">The click count that the platform reports. WinUI reports none: the control passes 4 (the highest
/// count Monaco tells apart) and <see cref="MouseDownState.TrySetCount"/> does the counting from the time and the position
/// (it never lets the count grow by more than one per click, and never past the number of clicks at the same position).</param>
/// <param name="TimestampMs">The time of the event, in milliseconds.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record EditorMouseEvent(
    double X,
    double Y,
    bool LeftButton,
    bool MiddleButton,
    bool AltKey,
    bool CtrlKey,
    bool ShiftKey,
    bool MetaKey,
    int Detail,
    long TimestampMs);
