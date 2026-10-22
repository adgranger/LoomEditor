// Ported from microsoft/vscode: src/vs/editor/browser/view/viewController.ts (IMouseDispatchData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;

namespace LoomEditor.View.Controller;

/// <summary>What a mouse press or drag step tells <see cref="ViewController.DispatchMouse"/>.</summary>
/// <param name="Position">The position under the mouse, in view coordinates.</param>
/// <param name="MouseColumn">The desired mouse column (when the position is clamped to the text length: a click after the text).</param>
/// <param name="RevealType">How to reveal the cursor after the move.</param>
/// <param name="StartedOnLineNumbers">The press started on the line numbers.</param>
/// <param name="InSelectionMode">A selection is being extended (Shift, or a drag).</param>
/// <param name="MouseDownCount">The number of consecutive clicks (1 to 4).</param>
/// <param name="AltKey">Alt is down.</param>
/// <param name="CtrlKey">Ctrl is down.</param>
/// <param name="MetaKey">The Windows key is down.</param>
/// <param name="ShiftKey">Shift is down.</param>
/// <param name="LeftButton">The left button is down.</param>
/// <param name="MiddleButton">The middle button is down.</param>
/// <param name="OnInjectedText">The mouse is over injected text (a double click then selects nothing).</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record MouseDispatchData(
    Position Position,
    int MouseColumn,
    NavigationCommandRevealType RevealType,
    bool StartedOnLineNumbers,
    bool InSelectionMode,
    int MouseDownCount,
    bool AltKey,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    bool LeftButton,
    bool MiddleButton,
    bool OnInjectedText);
