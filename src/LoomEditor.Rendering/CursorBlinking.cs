// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/viewCursors/viewCursors.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
namespace LoomEditor.Rendering;

/// <summary>
/// The caret blinking state machine. Monaco: <c>_getCursorBlinking</c>, <c>_updateBlinking</c>, <c>_show</c> and
/// <c>_hide</c> of <c>ViewCursors</c>.
/// </summary>
/// <remarks>
/// <para>
/// There is no timer in here: the host calls <see cref="Tick"/> every <see cref="BlinkInterval"/> while
/// <see cref="NeedsTimer"/> is <see langword="true"/>, <see cref="Update"/> when the focus or the read-only state
/// changes, and <see cref="Reset"/> when the selection changes.
/// </para>
/// <para>
/// Behavior difference: <see cref="TextEditorCursorBlinkingStyle.Smooth"/>, <see cref="TextEditorCursorBlinkingStyle.Phase"/>
/// and <see cref="TextEditorCursorBlinkingStyle.Expand"/> are CSS animations in Monaco and behave like
/// <see cref="TextEditorCursorBlinkingStyle.Blink"/> here.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class CursorBlinking
{
    /// <summary>Monaco: <c>ViewCursors.BLINK_INTERVAL</c>.</summary>
    public static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(500);

    private TextEditorCursorBlinkingStyle _style = TextEditorCursorBlinkingStyle.Blink;
    private bool _hasFocus;
    private bool _readOnly;

    /// <summary>Raised when <see cref="IsVisible"/> changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The configured style. Monaco: the <c>cursorBlinking</c> option (default <c>blink</c>).</summary>
    public TextEditorCursorBlinkingStyle Style
    {
        get => _style;
        set
        {
            _style = value;
            Apply();
        }
    }

    /// <summary>Whether the caret is drawn right now.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>Whether the host must call <see cref="Tick"/> every <see cref="BlinkInterval"/>.</summary>
    public bool NeedsTimer => IsBlinking(GetEffectiveStyle());

    /// <summary>Recomputes the effective style: hidden when unfocused, solid when read-only, else the configured one.</summary>
    public void Update(bool hasFocus, bool readOnly)
    {
        _hasFocus = hasFocus;
        _readOnly = readOnly;
        Apply();
    }

    /// <summary>Shows the caret again, unless it is hidden. Monaco: <c>onCursorStateChanged</c>.</summary>
    public void Reset() => Apply();

    /// <summary>Toggles the caret; does nothing unless the effective style blinks.</summary>
    public void Tick()
    {
        if (NeedsTimer)
        {
            SetVisible(!IsVisible);
        }
    }

    private static bool IsBlinking(TextEditorCursorBlinkingStyle style) =>
        style is not (TextEditorCursorBlinkingStyle.Hidden or TextEditorCursorBlinkingStyle.Solid);

    // Monaco: `_getCursorBlinking`.
    private TextEditorCursorBlinkingStyle GetEffectiveStyle()
    {
        if (!_hasFocus)
        {
            return TextEditorCursorBlinkingStyle.Hidden;
        }

        return _readOnly ? TextEditorCursorBlinkingStyle.Solid : _style;
    }

    // Monaco: `_updateBlinking` (hidden hides, everything else shows).
    private void Apply() => SetVisible(GetEffectiveStyle() != TextEditorCursorBlinkingStyle.Hidden);

    private void SetVisible(bool visible)
    {
        if (IsVisible == visible)
        {
            return;
        }

        IsVisible = visible;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
