// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
namespace LoomEditor.Rendering;

/// <summary>
/// How the caret blinks. Monaco: <c>TextEditorCursorBlinkingStyle</c>.
/// </summary>
/// <remarks>
/// <para>
/// Behavior difference: <see cref="Smooth"/>, <see cref="Phase"/> and <see cref="Expand"/> are CSS animations in Monaco;
/// here they behave like <see cref="Blink"/> (see <c>docs/PORTING.md</c>).
/// </para>
/// <para>Threading: an immutable value, safe to use from any thread.</para>
/// </remarks>
internal enum TextEditorCursorBlinkingStyle
{
    /// <summary>Hidden.</summary>
    Hidden = 0,

    /// <summary>Blinking.</summary>
    Blink = 1,

    /// <summary>Blinking with smooth fading (behaves like <see cref="Blink"/>).</summary>
    Smooth = 2,

    /// <summary>Blinking with prolonged filled state and smooth fading (behaves like <see cref="Blink"/>).</summary>
    Phase = 3,

    /// <summary>Expand collapse animation on the y axis (behaves like <see cref="Blink"/>).</summary>
    Expand = 4,

    /// <summary>No blinking.</summary>
    Solid = 5,
}
