// Ported from microsoft/vscode: src/vs/editor/contrib/toggleTabFocusMode/browser/toggleTabFocusMode.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.TabFocus;

/// <summary>
/// Registers <see cref="TabFocusCommands.ToggleTabFocusMode"/> (Ctrl+M), the keyboard trap exit: it flips
/// <see cref="EditorContext.TabMovesFocus"/> and announces the new mode to screen readers.
/// </summary>
/// <remarks>
/// The state is per editor (Monaco's <c>TabFocus</c> is a process-wide singleton).
/// Threading: UI thread only.
/// </remarks>
internal sealed class TabFocusFeature : IEditorFeature
{
    private IDisposable? _registration;

    /// <inheritdoc />
    public string Id => "loom.tabFocus";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not EditorContext editorContext)
        {
            return;
        }

        _registration = context.Commands.Register(new EditorCommand(TabFocusCommands.ToggleTabFocusMode, () =>
        {
            bool value = !editorContext.TabMovesFocus;
            editorContext.TabMovesFocus = value;
            context.GetAnnouncer().Announce(value ? AccessibilityMessages.TabMovesFocusOn : AccessibilityMessages.TabMovesFocusOff);
        }));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration?.Dispose();
        _registration = null;
    }
}
